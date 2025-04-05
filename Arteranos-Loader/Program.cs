using Ipfs;
using Ipfs.CoreApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Arteranos_Loader
{
    internal static class Program
    {
        private const string COMPANYNAME = "arteranos";


        private const string KUBO_ARCH_WIN64 = "windows-amd64";
        private const string KUBO_ARCH_LINUX64 = "linux-amd64";
        private const string AUTHORNAME = "willneedit";

        public static bool IsOnLinux { get; private set; } = false;
        public static string ProgDataDir { get; private set; } = null;
        public static string IPFSExeName { get; private set; } = null;
        public static string IPFSExePath { get; private set; } = null;
        public static string PersistentDataPath { get; private set; } = null;
        public static string ArteranosFlavor { get; private set; } = null;
        public static string ArteranosRoot { get; private set; } = null;
        public static string ArteranosDir { get; private set; } = null;
        public static string ArteranosExePath { get; private set; } = null;
        public static BootstrapData BootstrapData { get; private set; } = null;
        public static string CacheFileName { get; private set; } = null;

        private static string ipfsArchiveSource = null;
        private static string ipfsExeInArchive = null;
        private static string osArchitecture = null;


        private static Splash splash = null;

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Initialize0();

            splash = new();

            Application.Run(splash);
        }

        public static async Task LoaderWorkerThread()
        {
            try
            {
                await WebDownloadBootstrap();

                Initialize();

                await WebDownloadIPFSExe();

                await WebDownloadArteranos();

                await StartArteranosIPFS();

                await GatherLocalFiles();

                await GatherRemoteFiles();

                CompareFiles();

                await DownloadFromIPFS();

                WriteHashCacheFile();

                StartArteranos();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Loader failed: {ex}");
            }
            finally
            {
                Console.Out.Flush();
            }

            Application.Exit();
        }

        private static readonly HttpClient HttpClient = new();

        // ---------------------------------------------------------------
        #region Initializing
        private static void Initialize0()
        {
            HttpClient.Timeout = TimeSpan.FromSeconds(60);

            IsOnLinux = Environment.OSVersion.Platform == PlatformID.Unix;

            string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (IsOnLinux)
            {
                ProgDataDir = $"{userHomeDir}/{COMPANYNAME}/arteranos";
            }
            else
            {
                string pdroot = Environment.GetEnvironmentVariable("PROGRAMDATA");
                ProgDataDir = $"{pdroot}/{COMPANYNAME}/arteranos";
            }

            if (!Directory.Exists(ProgDataDir)) Directory.CreateDirectory(ProgDataDir);
        }

        private static string persistentDataPathRoot = null;

        private static void Initialize()
        {
            IsOnLinux = Environment.OSVersion.Platform == PlatformID.Unix;

            string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string KUBO_EXECUTABLE_ROOT = $"{BootstrapData.KuboWebDlRoot}/{BootstrapData.KuboVersion}/kubo_{BootstrapData.KuboVersion}";

            if (IsOnLinux)
            {
                ProgDataDir = $"{userHomeDir}/{COMPANYNAME}/arteranos";
                IPFSExeName = "ipfs";

                ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_LINUX64}.tar.gz";
                ipfsExeInArchive = $"/{IPFSExeName}";
                osArchitecture = "Linux-amd64";

                string confighome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(confighome))
                    confighome = $"{Environment.GetEnvironmentVariable("HOME")}/.config";

                persistentDataPathRoot = $"{confighome}/unity3d";
            }
            else
            {
                string pdroot = Environment.GetEnvironmentVariable("PROGRAMDATA");
                ProgDataDir = $"{pdroot}/{COMPANYNAME}/arteranos";
                IPFSExeName = "ipfs.exe";

                ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_WIN64}.zip";
                ipfsExeInArchive = $"/kubo/{IPFSExeName}";
                osArchitecture = "Win-amd64";

                // Unity manual says it prefers USERPROFILE over SHGetKnownFolderPath()
                persistentDataPathRoot = $"{Environment.GetEnvironmentVariable("USERPROFILE")}/Appdata/LocalLow";
            }

            // TODO Flavor selection via commandline
            ArteranosFlavor = "desktop";
            ArteranosRoot = $"{ArteranosFlavor}-{osArchitecture}";
            ArteranosDir = $"{ProgDataDir}/{ArteranosRoot}";
            CacheFileName = $"{ArteranosDir}-Cache.json";


            Console.WriteLine($"Program Dir: {ProgDataDir}");
        }

        #endregion
        // ---------------------------------------------------------------
        #region Web Download File
        private static Action<long, long> ReportProgress(string pattern, int frompercent, int topercent)
        {
            void ReportProgressFunc(long actual, long total)
            {
                float normalized = (float)actual / total;
                string msg = string.Format(pattern, Magnitude(actual), Magnitude(total));
                int progress = (int)(frompercent + ((float)topercent - frompercent) * normalized);

                splash.Progress = progress;
                splash.ProgressTxt = msg;
            }


            return (actual, total) => ReportProgressFunc(actual, total);
        }

        private static string Magnitude(long value, string suffix = "B")
        {
            float val = value;
            string[] prefixes = ["", "k", "M", "G", "T", "E"];
            for (int i = 0; i < prefixes.Length - 1; i++)
            {
                if (val < 900) return string.Format("{0:F1} {1}{2}", val, prefixes[i], suffix);
                // SI numbers prefixes, sorry, no powers of two...
                val /= 1000;
            }
            return string.Format("{0:F1} {1}{2}", val, prefixes[^1], suffix);
        }

        private static async Task WebDownloadFileAsync(string source, string target, string what, int to)
        {
            int from = splash.Progress;

            if (File.Exists(target)) File.Delete(target);

            using Stream file = File.Create(target);

            try
            {
                await HttpClient.DownloadAsync(source, file, ReportProgress($"{what} ({{0}} of {{1}})", from, to));
            }
            catch (Exception)
            {
                File.Delete(target);
                throw;
            }

        }

        #endregion
        // ---------------------------------------------------------------
        #region Web Download components up to 40
        private static async Task WebDownloadBootstrap()
        {
            string bootstrapDataFile = $"{ProgDataDir}/BootstrapData.json";
            if (File.Exists(bootstrapDataFile))
            {
                string json = File.ReadAllText(bootstrapDataFile);
                BootstrapData = JsonConvert.DeserializeObject<BootstrapData>(json);
            }
            else
                BootstrapData = BootstrapData.Defaults();

            try
            {
                await WebDownloadFileAsync(BootstrapData.ArteranosBootstrapData, bootstrapDataFile, "D/l bootstrap data", 5);
                string json = File.ReadAllText(bootstrapDataFile);
                BootstrapData = JsonConvert.DeserializeObject<BootstrapData>(json);
                Utils.SetWorldWritable(bootstrapDataFile);
            }
            catch
            {
                if (File.Exists(bootstrapDataFile)) File.Delete(bootstrapDataFile);
                BootstrapData = BootstrapData.Defaults();
            }
        }

        private static async Task WebDownloadIPFSExe()
        {
            string url = ipfsArchiveSource;
            string fileInArchive = ipfsExeInArchive;

            Console.WriteLine($"IPFS Exe   : {IPFSExeName}");
            Console.WriteLine($"IPFS source: {ipfsArchiveSource}");

            IPFSExePath = $"{ProgDataDir}/{IPFSExeName}";

            Console.WriteLine($"IPFS Exe Path: {IPFSExePath}");

            string target = $"{ProgDataDir}/downloaded-kubo-ipfs";
            string targetDir = $"{target}.dir";

            // Got a version, skip downloading via web.
            if (File.Exists(IPFSExePath))
            {
                Console.WriteLine("Web download of IPFS skipped, already one version there");
                return;
            }

            try
            {
                if(!File.Exists(target))
                    await WebDownloadFileAsync(url, target, "D/l IPFS from web", 10);

                splash.ProgressTxt = "Extracting IPFS from web...";

                if (url.EndsWith(".zip"))
                    ZipFile.ExtractToDirectory(target, targetDir);
                else if (url.EndsWith(".tar.gz"))
                    await Utils.UnTarGzDirectoryAsync(targetDir, target);

                File.Copy($"{targetDir}/{fileInArchive}", IPFSExePath);
                if (IsOnLinux) Utils.Exec($"chmod 755 {IPFSExePath}");
                Utils.SetWorldWritable(IPFSExePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot download IPFS executable: {ex.Message}");
                throw;
            }
            finally
            {
                if(File.Exists(target)) File.Delete(target);
                if(Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                splash.Progress = 20;
            }
        }

        private static async Task WebDownloadArteranos()
        {
            string arteranosURL = $"{BootstrapData.ArteranosWebDlRoot}/{ArteranosRoot}.tar.gz";

            string arteranosArchiveFile = $"{ProgDataDir}/{ArteranosRoot}.tar.gz";

            string ArteranosExeFile = 
                (ArteranosFlavor == "desktop"
                    ? "Arteranos"
                    : "Arteranos-Server")
                + (IsOnLinux
                    ? ""
                    : ".exe");
            ArteranosExePath = $"{ArteranosDir}/{ArteranosExeFile}";

            PersistentDataPath = persistentDataPathRoot + $"/{AUTHORNAME}/Arteranos" +
                (ArteranosFlavor == "desktop"
                    ? string.Empty
                    : "_DedicatedServer");

            Console.WriteLine($"Archive File       : {arteranosArchiveFile}");
            Console.WriteLine($"Arteranos Exe path : {ArteranosExePath}");
            Console.WriteLine($"User data path     : {PersistentDataPath}");

            if (Directory.Exists(ArteranosDir)) return;

            try
            {
                await WebDownloadFileAsync(arteranosURL, arteranosArchiveFile, "D/l Arteranos from web", 30).ConfigureAwait(false);

                splash.ProgressTxt = "Extracting Arteranos from web...";

                await Utils.UnTarGzDirectoryAsync(ArteranosDir, arteranosArchiveFile).ConfigureAwait(false);

                if (IsOnLinux) Utils.Exec($"chmod -R 755 {ArteranosDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot web download {arteranosURL}: {ex.Message}");
                Console.WriteLine("Will resort to using IPFS.");
            }
            finally
            {
                if (File.Exists(arteranosArchiveFile)) File.Delete(arteranosArchiveFile);
                splash.Progress = 40;
            }
        }

        #endregion
        // ---------------------------------------------------------------
        #region Init and start IPFS up to 50

        private static void GetFreePort(string what, ref int port, HashSet<int> occupied)
        {
            Random rnd = new();
            for (int i = 0; i < 5000;  i++)
            {
                if (!occupied.Contains(port))
                {
                    Console.WriteLine($"{what} port {port} is available");
                    occupied.Add(port);
                    return;
                }

                // Mark it off in the list
                occupied.Add(port);

                // Try another one.
                port = rnd.Next(16384, 49152);
            }

            throw new Exception("Port exhaustion");
        }

        private static async Task StartArteranosIPFS()
        {
            splash.ProgressTxt = "Starting Arteranos IPFS";

            IPFSConnection.Status res = IPFSConnection.CheckRepository(true);

            if(res != IPFSConnection.Status.OK)
                throw new InvalidOperationException($"Cannot build/access IPFS repo: {res}");

            res = IPFSConnection.AddBootstraps();

            if (res != IPFSConnection.Status.OK)
                throw new InvalidOperationException($"Cannot add Prime and Deploy bootstrap multiaddr: {res}");

            // Starts the daemon, safe to start on top of another instance, and
            // fails to start if there's a port squatter.
            _ = IPFSConnection.StartDaemon(false);

            res = await IPFSConnection.CheckAPIConnection(20);
            if(res != IPFSConnection.Status.OK)
            {
                Console.WriteLine("Cannot start daemon, maybe on different ports...");

                int ipfsPort = 4001;    // default
                int apiPort = 5001;     // default
                HashSet<int> occupied = [];

                Utils.GetUsedPorts(occupied);

                GetFreePort("API", ref apiPort, occupied);
                GetFreePort("IPFS", ref ipfsPort, occupied);

                IPFSConnection.ReconfigurePorts(apiPort, ipfsPort);

                // Start the daemon anew, and see if we're okay.
                IPFSConnection.StartDaemon(true);
                await Task.Delay(5000);
                _ = await IPFSConnection.CheckAPIConnection(5, true);

            }

            splash.Progress = 50;
        }
        #endregion
        // ---------------------------------------------------------------
        #region Patch via IPFS

        private static List<FileEntry> LocalFileList = null;
        private static List<FileEntry> RemoteFileList = null;

        private static async Task GatherLocalFiles()
        {
            splash.ProgressTxt = "Listing local files...";

            Dictionary<string, FileEntry> CachedFiles = [];

            if (File.Exists(CacheFileName))
            {
                List<FileEntry> CachedLocalFileList;

                string json = File.ReadAllText(CacheFileName);
                CachedLocalFileList = JsonConvert.DeserializeObject<List<FileEntry>>(json);

                foreach (FileEntry entry in CachedLocalFileList)
                    CachedFiles[entry.Path] = entry;
            }

            List<FileEntry> list = [];
            List<string> list1 = Utils.ListDirectory(ArteranosDir, true, false);
            for (int i = 0; i < list1.Count; i++)
            {
                list1[i] = list1[i].Replace('\\', '/');

                string entry = list1[i];
                FileInfo fileInfo = new(entry);
                string path = entry[(ArteranosDir.Length + 1)..];

                string Cid = (CachedFiles.ContainsKey(path) && CachedFiles[path].Size == fileInfo.Length) 
                    ? CachedFiles[path].Cid 
                    : null;

                list.Add(new FileEntry
                {
                    Cid = Cid,
                    Size = fileInfo.Length,
                    Path = path,
                    Status = FileStatus.ToDelete
                });
            }

            LocalFileList = list;

            // Only missing files to rehash.
            for (int i = 0; i < LocalFileList.Count; i++)
            {
                splash.ProgressTxt = $"Listing local files ({i} of {LocalFileList.Count})";
                FileEntry entry = LocalFileList[i];
                if(entry.Cid == null)
                {
                    AddFileOptions ao = new()
                    {
                        OnlyHash = true,
                    };
                    IFileSystemNode fsn = await IPFSConnection.Ipfs.FileSystem.AddFileAsync($"{ArteranosDir}/{entry.Path}", ao);
                    entry.Cid = fsn.Id;
                }
            }

            splash.Progress = 55;
        }

        private static async Task GatherRemoteFiles()
        {
            splash.ProgressTxt = "Listing remote files...";

            string name = $"{BootstrapData.IPFSDeployDir}/{ArteranosRoot}-FileList.json";
            string rootCid = await IPFSConnection.Ipfs.ResolveAsync(name);

            if (rootCid.StartsWith("/ipfs/")) rootCid = rootCid[6..];

            string json = await IPFSConnection.Ipfs.FileSystem.ReadAllTextAsync(rootCid);
            RemoteFileList = JsonConvert.DeserializeObject<List<FileEntry>>(json);

            splash.Progress = 60;
        }

        private static int toDownloadFiles = 0;
        private static long toDownloadSize = 0;

        private static Dictionary<string, FileEntry> CompareTable = null;

        private static void CompareFiles()
        {
            CompareTable = [];

            foreach(FileEntry entry in LocalFileList)
                CompareTable[entry.Path] = entry;

            foreach(FileEntry entry in RemoteFileList)
            {
                entry.Status = FileStatus.ToPatch;
                if(!CompareTable.ContainsKey(entry.Path))
                {
                    // New file
                    CompareTable[entry.Path] = entry;
                    toDownloadFiles++;
                    toDownloadSize += entry.Size;
                }
                else if (CompareTable[entry.Path].Cid != entry.Cid)
                {
                    // Changed file
                    CompareTable[entry.Path] = entry;
                    toDownloadFiles++;
                    toDownloadSize += entry.Size;
                }
                else
                {
                    // As-is
                    entry.Status = FileStatus.Unchanged;
                    CompareTable[entry.Path] = entry;
                }
            }
        }

        private static async Task DownloadFromIPFS()
        {
            splash.ProgressTxt = "D/l from IPFS...";

            string name = $"{BootstrapData.IPFSDeployDir}/{ArteranosRoot}";
            string rootCid = await IPFSConnection.Ipfs.ResolveAsync(name);
            if (rootCid.StartsWith("/ipfs/")) rootCid = rootCid[6..];

            LocalFileList = [];

            foreach (KeyValuePair<string, FileEntry> entry in CompareTable)
            {
                string target = $"{ArteranosDir}/{entry.Key}";
                if (entry.Value.Status == FileStatus.ToDelete)
                {
                    File.Delete(target);
                }
                else if(entry.Value.Status == FileStatus.ToPatch)
                {
                    splash.ProgressTxt = $"D/l from IPFS ({toDownloadFiles} files, {Magnitude(toDownloadSize)})";

                    Stream s = await IPFSConnection.Ipfs.FileSystem.ReadFileAsync($"{rootCid}/{entry.Key}");
                    if(File.Exists(target)) File.Delete(target);
                    Stream ts = File.Create(target);
                    s.CopyTo(ts);
                    toDownloadFiles--;
                    toDownloadSize -= entry.Value.Size;
                }

                // And update local file list.
                if(entry.Value.Status != FileStatus.ToDelete)
                    LocalFileList.Add(entry.Value);

                // Unchanged and new files need to be opened
                Utils.SetWorldWritable(target);
            }

            if (IsOnLinux) Utils.Exec($"chmod -R 755 {ArteranosDir}");

            splash.Progress = 80;
        }

        private static void WriteHashCacheFile()
        {
            string json = JsonConvert.SerializeObject(LocalFileList, Formatting.Indented);
            File.WriteAllText(CacheFileName, json);
            Utils.SetWorldWritable(CacheFileName);
        }
        #endregion
        // ---------------------------------------------------------------
        #region Handoff and startup

        private static void StartArteranos()
        {
            string argLine = string.Empty;

            ProcessStartInfo psi = new()
            {
                FileName = ArteranosExePath,
                Arguments = argLine,
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
            };

            try
            {
                Process process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}
