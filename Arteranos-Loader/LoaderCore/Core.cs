using System;
using System.Threading.Tasks;
using System.Net.Http;
using ArteranosLoader.Views;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Collections.Generic;

using Ipfs;
using Ipfs.CoreApi;
using System.Diagnostics;
using System.Net;

namespace ArteranosLoader.LoaderCore;

public class Core
{
    private const string COMPANYNAME = "arteranos";
    private const string KUBO_ARCH_WIN64 = "windows-amd64";
    private const string KUBO_ARCH_LINUX64 = "linux-amd64";
    private const string AUTHORNAME = "willneedit";

    public Func<IProgressReporter, Task> Action => StartupAsync;

    public bool IsOnLinux { get; private set; } = false;
    public string ProgDataDir { get; private set; } = string.Empty;
    public string IPFSExeName { get; private set; } = string.Empty;
    public string IPFSExePath { get; private set; } = string.Empty;

    public string ArteranosFlavor { get; private set; } = string.Empty;
    public string ArteranosRoot { get; private set; } = string.Empty;
    public string ArteranosDir { get; private set; } = string.Empty;
    public string ArteranosExePath { get; private set; } = string.Empty;
    public string CacheFileName { get; private set; } = string.Empty;

    public string PersistentDataPath { get; private set; } = string.Empty;

    public BootstrapData BootstrapData { get; private set; } = BootstrapData.Defaults();


    private string ipfsArchiveSource = string.Empty;
    private string ipfsExeInArchive = string.Empty;
    private string osArchitecture = string.Empty;
    private IPAddress? externalIPAddress = null;
    private IPFSConnection? IPFSConnection = null;

    private WebDownload? WebDownload;

    private IProgressReporter? splash;

    private async Task StartupAsync(IProgressReporter reporter)
    {
        try
        {
            splash = reporter;

            reporter.ProgressTxt = "Initializing..";

            Initialize0();

            externalIPAddress = await Networking.GetExternalIPAddress();

            Console.WriteLine(externalIPAddress == null
                ? "Cannot determine external IPv4 address"
                : $"External IP address is {externalIPAddress}"
            );

            HttpClient httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            WebDownload = new(httpClient, reporter);

            await WebDownloadBootstrap();

            Initialize();

            IPFSConnection = new(this);

            await WebDownloadIPFSExe();

            await WebDownloadArteranos();

            await StartArteranosIPFS();

            await GatherLocalFiles();

            await GatherRemoteFiles();

            CompareFiles();

            await DownloadFromIPFS();

            WriteHashCacheFile();

            StartArteranos();

            // Do some background stuff here.
            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            reporter.ProgressTxt = $"{ex.Message}";
            await Task.Delay(10000);
        }
    }

    // ---------------------------------------------------------------
    #region Initializing

    private void Initialize0()
    {
        IsOnLinux = Environment.OSVersion.Platform == PlatformID.Unix;

        string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (IsOnLinux)
        {
            ProgDataDir = $"{userHomeDir}/{COMPANYNAME}/arteranos";
        }
        else
        {
            string pdroot = Environment.GetEnvironmentVariable("PROGRAMDATA") ?? string.Empty;
            ProgDataDir = $"{pdroot}/{COMPANYNAME}/arteranos";
        }

        if (!Directory.Exists(ProgDataDir)) Directory.CreateDirectory(ProgDataDir);
    }

    private static string persistentDataPathRoot = string.Empty;

    private void Initialize()
    {
        IsOnLinux = Environment.OSVersion.Platform == PlatformID.Unix;

        string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string KUBO_EXECUTABLE_ROOT = $"{BootstrapData.KuboWebDlRoot}/{BootstrapData.KuboVersion}/kubo_{BootstrapData.KuboVersion}";

        if (IsOnLinux)
        {
            ProgDataDir = $"{userHomeDir}/{COMPANYNAME}/arteranos";
            IPFSExeName = "ipfs";

            ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_LINUX64}.tar.gz";
            ipfsExeInArchive = $"/kubo/{IPFSExeName}";
            osArchitecture = "Linux-amd64";

            string confighome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
            if (string.IsNullOrEmpty(confighome))
                confighome = $"{Environment.GetEnvironmentVariable("HOME")}/.config";

            persistentDataPathRoot = $"{confighome}/unity3d";
        }
        else
        {
            string pdroot = Environment.GetEnvironmentVariable("PROGRAMDATA") ?? string.Empty;
            ProgDataDir = $"{pdroot}/{COMPANYNAME}/arteranos";
            IPFSExeName = "ipfs.exe";

            ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_WIN64}.zip";
            ipfsExeInArchive = $"/kubo/{IPFSExeName}";
            osArchitecture = "Win-amd64";

            // Unity manual says it prefers USERPROFILE over SHGetKnownFolderPath()
            persistentDataPathRoot = $"{Environment.GetEnvironmentVariable("USERPROFILE")}/Appdata/LocalLow";
        }

        ArteranosFlavor = Program.Server ? "server" : "desktop";
        ArteranosRoot = $"{ArteranosFlavor}-{osArchitecture}";
        ArteranosDir = $"{ProgDataDir}/{ArteranosRoot}";
        CacheFileName = $"{ArteranosDir}-Cache.json";


        Console.WriteLine($"Program Dir: {ProgDataDir}");
    }


    #endregion
    // ---------------------------------------------------------------
    #region Web Download components up to 40
    private async Task WebDownloadBootstrap()
    {
        string bootstrapDataFile = $"{ProgDataDir}/BootstrapData.json";
        if (File.Exists(bootstrapDataFile))
        {
            string json = File.ReadAllText(bootstrapDataFile);
            BootstrapData = JsonConvert.DeserializeObject<BootstrapData>(json) ?? BootstrapData.Defaults();
        }
        else
            BootstrapData = BootstrapData.Defaults();

        try
        {
            await WebDownload.WebDownloadFileAsync(BootstrapData.ArteranosBootstrapData, bootstrapDataFile, "D/l bootstrap data", 5);
            string json = File.ReadAllText(bootstrapDataFile);
            BootstrapData = JsonConvert.DeserializeObject<BootstrapData>(json) ?? BootstrapData.Defaults();
            Utils.SetWorldWritable(bootstrapDataFile);
        }
        catch
        {
            if (File.Exists(bootstrapDataFile)) File.Delete(bootstrapDataFile);
            BootstrapData = BootstrapData.Defaults();
        }
    }

    private async Task WebDownloadIPFSExe()
    {
        if (WebDownload == null) throw new Exception();
        if (splash == null) throw new Exception();

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
            if (!File.Exists(target))
                await WebDownload.WebDownloadFileAsync(url, target, "D/l IPFS from web", 10);

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
            if (File.Exists(target)) File.Delete(target);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            splash.Progress = 20;
        }
    }

    private async Task WebDownloadArteranos()
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
            await WebDownload.WebDownloadFileAsync(arteranosURL, arteranosArchiveFile, "D/l Arteranos from web", 30).ConfigureAwait(false);

            splash.ProgressTxt = "Extracting Arteranos from web...";

            await Utils.UnTarGzDirectoryAsync(ArteranosDir, arteranosArchiveFile).ConfigureAwait(false);

            if (IsOnLinux) Utils.Exec($"chmod -R 755 {ArteranosDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot web download {arteranosURL}: {ex.Message}");

            splash.ProgressTxt = $"'{ArteranosRoot}' is unavailable";
            throw new Exception(splash.ProgressTxt);
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

    private void GetFreePort(string what, ref int port, HashSet<int> occupied)
    {
        Random rnd = new();
        for (int i = 0; i < 5000; i++)
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

    private async Task StartArteranosIPFS()
    {
        splash.ProgressTxt = "Starting Arteranos IPFS";

        IPFSConnection.Status res = IPFSConnection.CheckRepository(true);

        if (res != IPFSConnection.Status.OK)
            throw new InvalidOperationException($"Cannot build/access IPFS repo: {res}");

        res = IPFSConnection.AddBootstraps();

        if (res != IPFSConnection.Status.OK)
            throw new InvalidOperationException($"Cannot add Prime and Deploy bootstrap multiaddr: {res}");

        // Set (or update) the external IPv4 address as seen
        IPFSConnection.SetExternalIPAddress(externalIPAddress.ToString());

        // Starts the daemon, safe to start on top of another instance, and
        // fails to start if there's a port squatter.
        _ = IPFSConnection.StartDaemon(false);

        res = await IPFSConnection.CheckAPIConnection(20, true);
        if (res != IPFSConnection.Status.OK)
        {
            Console.WriteLine("Cannot start daemon, or there is a different daemon, so try on different ports...");

            int ipfsPort = 4001;    // default
            int apiPort = 5001;     // default
            HashSet<int> occupied = [];

            Utils.GetUsedPorts(occupied);

            GetFreePort("API", ref apiPort, occupied);
            GetFreePort("IPFS", ref ipfsPort, occupied);

            await Task.Delay(5000);

            IPFSConnection.ReconfigurePorts(apiPort, ipfsPort);

            // Multiaddress entries have both the addresses and ports, so we have to update them, too.
            IPFSConnection.SetExternalIPAddress(externalIPAddress.ToString());

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

    private List<FileEntry>? LocalFileList = null;
    private List<FileEntry>? RemoteFileList = null;

    private async Task GatherLocalFiles()
    {
        splash.ProgressTxt = "Listing local files...";

        Dictionary<string, FileEntry> CachedFiles = [];

        if (File.Exists(CacheFileName))
        {
            List<FileEntry>? CachedLocalFileList;

            string json = File.ReadAllText(CacheFileName);
            CachedLocalFileList = JsonConvert.DeserializeObject<List<FileEntry>>(json);

            foreach (FileEntry entry in CachedLocalFileList)
                CachedFiles[entry.Path ?? string.Empty] = entry;
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
            if (entry.Cid == null)
            {
                AddFileOptions ao = new()
                {
                    OnlyHash = true,
                };
                IFileSystemNode fsn = await IPFSConnection.Ipfs.FileSystem.AddFileAsync($"{ArteranosDir}/{entry.Path}", ao);
                entry.Cid = fsn.Id;
            }
        }

        splash.Progress = 53;
    }

    private async Task GatherRemoteFiles()
    {
        try
        {
            splash.ProgressTxt = "Looking up remote file list...";

            string rootCid = await IPFSConnection.Ipfs.ResolveAsync(BootstrapData.IPFSDeployDir);

            if (rootCid.StartsWith("/ipfs/")) rootCid = rootCid[6..];

            splash.ProgressTxt = "Reading remote files...";
            
            splash.Progress = 58;

            string json = await IPFSConnection.Ipfs.FileSystem.ReadAllTextAsync($"{rootCid}/{ArteranosRoot}-FileList.json");

            RemoteFileList = JsonConvert.DeserializeObject<List<FileEntry>>(json);

            splash.Progress = 60;
        }
        catch (System.Exception ex)
        {
            splash.ProgressTxt = $"Cannot list remote files: {ex.Message}";
            throw;
        }
    }

    private int toDownloadFiles = 0;
    private long toDownloadSize = 0;

    private Dictionary<string, FileEntry>? CompareTable = null;

    private void CompareFiles()
    {
        CompareTable = [];

        foreach (FileEntry entry in LocalFileList)
            CompareTable[entry.Path ?? string.Empty] = entry;

        foreach (FileEntry entry in RemoteFileList)
        {
            entry.Status = FileStatus.ToPatch;

            string path = entry.Path ?? string.Empty;
            if (!CompareTable.ContainsKey(path))
            {
                // New file
                CompareTable[path] = entry;
                toDownloadFiles++;
                toDownloadSize += entry.Size;
            }
            else if (CompareTable[path].Cid != entry.Cid)
            {
                // Changed file
                CompareTable[path] = entry;
                toDownloadFiles++;
                toDownloadSize += entry.Size;
            }
            else
            {
                // As-is
                entry.Status = FileStatus.Unchanged;
                CompareTable[path] = entry;
            }
        }
    }

    private async Task DownloadFromIPFS()
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
            else if (entry.Value.Status == FileStatus.ToPatch)
            {
                splash.ProgressTxt = $"D/l from IPFS ({toDownloadFiles} files, {Utils.Magnitude(toDownloadSize)})";

                using Stream s = await IPFSConnection.Ipfs.FileSystem.ReadFileAsync($"{rootCid}/{entry.Key}");
                if (File.Exists(target)) File.Delete(target);
                using Stream ts = File.Create(target);
                s.CopyTo(ts);
                toDownloadFiles--;
                toDownloadSize -= entry.Value.Size;
            }

            // And update local file list.
            if (entry.Value.Status != FileStatus.ToDelete)
                LocalFileList.Add(entry.Value);

            // Unchanged and new files need to be opened
            Utils.SetWorldWritable(target);
        }

        if (IsOnLinux) Utils.Exec($"chmod -R 755 {ArteranosDir}");

        splash.Progress = 80;
    }

    private void WriteHashCacheFile()
    {
        string json = JsonConvert.SerializeObject(LocalFileList, Formatting.Indented);
        File.WriteAllText(CacheFileName, json);
        Utils.SetWorldWritable(CacheFileName);
    }
    #endregion
    // ---------------------------------------------------------------
    #region Handoff and startup

    private void StartArteranos()
    {
        List<string> extra = Program.Extra;

        string arguments = extra.Count > 0
                ? $"\"{string.Join("\" \"", extra)}\""
                : string.Empty;

        ProcessStartInfo psi = new()
        {
            FileName = ArteranosExePath,
            Arguments = arguments, // Extra args are passed over to Arteranos main
            UseShellExecute = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
        };

        try
        {
            Console.WriteLine($"Starting {psi.FileName} {psi.Arguments}...");
            Process? process = Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    #endregion
}