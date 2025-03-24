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
        public static string ArteranosExePath { get; private set; } = null;
        public static BootstrapData BootstrapData { get; private set; } = null;

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

                for (int i = 4; i < 10; i++)
                {
                    splash.Progress = i * 10;

                    await Task.Delay(1000);
                }
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

            Console.WriteLine($"Program Dir: {ProgDataDir}");
        }

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

            static string Magnitude(long value, string suffix = "B")
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


            return (actual, total) => ReportProgressFunc(actual, total);
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
            {
                BootstrapData = new();
                string json = JsonConvert.SerializeObject(BootstrapData, Formatting.Indented);
                File.WriteAllText(bootstrapDataFile, json);
            }

            try
            {
                await WebDownloadFileAsync(BootstrapData.ArteranosBootstrapData, bootstrapDataFile, "D/l bootstrap data", 5);
                string json = File.ReadAllText(bootstrapDataFile);
                BootstrapData = JsonConvert.DeserializeObject<BootstrapData>(json);
            }
            catch
            {
                if (File.Exists(bootstrapDataFile)) File.Delete(bootstrapDataFile);
                BootstrapData = new();
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
            ArteranosFlavor = "desktop";
            string arteranosRoot = $"{ArteranosFlavor}-{osArchitecture}";
            string arteranosURL = $"{BootstrapData.ArteranosWebDlRoot}/{arteranosRoot}.tar.gz";

            string arteranosDir = $"{ProgDataDir}/{arteranosRoot}";
            string arteranosArchiveFile = $"{ProgDataDir}/{arteranosRoot}.tar.gz";

            string ArteranosExeFile = 
                (ArteranosFlavor == "desktop"
                    ? "Arteranos"
                    : "Arteranos-Server")
                + (IsOnLinux
                    ? ""
                    : ".exe");
            ArteranosExePath = $"{arteranosDir}/{ArteranosExeFile}";

            PersistentDataPath = persistentDataPathRoot + $"/{AUTHORNAME}/Arteranos" +
                (ArteranosFlavor == "desktop"
                    ? string.Empty
                    : "_DedicatedServer");

            Console.WriteLine($"Archive File       : {arteranosArchiveFile}");
            Console.WriteLine($"Arteranos Exe path : {ArteranosExePath}");
            Console.WriteLine($"User data path     : {PersistentDataPath}");

            if (Directory.Exists(arteranosDir)) return;

            try
            {
                await WebDownloadFileAsync(arteranosURL, arteranosArchiveFile, "D/l Arteranos from web", 30).ConfigureAwait(false);

                splash.ProgressTxt = "Extracting Arteranos from web...";

                await Utils.UnTarGzDirectoryAsync(arteranosDir, arteranosArchiveFile).ConfigureAwait(false);

                if (IsOnLinux) Utils.Exec($"chmod -R 755 {arteranosDir}");
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

        #region Init and startup IPFS

        private static void GetFreePort(string what, ref int port, HashSet<int> occupied)
        {
            Random rnd = new Random();
            bool free = true;

            for(int i = 0; i < 5000;  i++)
            {
                if(free)
                {
                    Console.WriteLine($"{what} port {port} is available");
                    occupied.Add(port);
                    return;
                }

                // Mark it off in the list
                occupied.Add(port);

                // Try another one.
                port = rnd.Next(8192, 49152);
            }

            throw new Exception("Port  exhaustion");
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
            res = IPFSConnection.StartDaemon(false);

            res = await IPFSConnection.CheckAPIConnection(20);
            if(res != IPFSConnection.Status.OK)
            {
                Console.WriteLine("Cannot start daemon, maybe on different ports...");

                int ipfsPort = 4001;    // default
                int apiPort = 5001;     // default
                HashSet<int> occupied = new();

                GetFreePort("API", ref apiPort, occupied);
                GetFreePort("IPFS", ref ipfsPort, occupied);

                IPFSConnection.ReconfigurePorts(apiPort, ipfsPort);

                // Start the daemon anew, and see if we're okay.
                IPFSConnection.StartDaemon(false);
                await Task.Delay(5000);
                res = await IPFSConnection.CheckAPIConnection(5, true);

            }

            splash.Progress = 50;
        }
        #endregion
    }
}
