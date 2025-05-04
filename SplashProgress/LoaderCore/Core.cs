using System;
using System.Threading.Tasks;
using System.Net.Http;
using SplashProgress.Views;
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;

namespace SplashProgress.LoaderCore;

public class Core
{
    private const string COMPANYNAME = "arteranos";
    private const string KUBO_ARCH_WIN64 = "windows-amd64";
    private const string KUBO_ARCH_LINUX64 = "linux-amd64";
    private const string AUTHORNAME = "willneedit";

    public Func<IProgressReporter, Task> Action => StartupAsync;

    public bool IsOnLinux { get; private set; } = false;
    public string ProgDataDir { get; private set; } = string.Empty;
    public static string IPFSExeName { get; private set; } = string.Empty;
    public static string IPFSExePath { get; private set; } = string.Empty;

    public static string ArteranosFlavor { get; private set; } = string.Empty;
    public static string ArteranosRoot { get; private set; } = string.Empty;
    public static string ArteranosDir { get; private set; } = string.Empty;
    public static string ArteranosExePath { get; private set; } = string.Empty;
    public static string CacheFileName { get; private set; } = string.Empty;

    public static string PersistentDataPath { get; private set; } = string.Empty;

    public BootstrapData BootstrapData { get; private set; } = BootstrapData.Defaults();


    private static string ipfsArchiveSource = string.Empty;
    private static string ipfsExeInArchive = string.Empty;
    private static string osArchitecture = string.Empty;

    private WebDownload? WebDownload;

    private IProgressReporter? splash;

    private async Task StartupAsync(IProgressReporter reporter)
    {
        splash = reporter;

        reporter.ProgressTxt = "Initializing..";

        Initialize0();

        HttpClient httpClient = new()
        // {
        //     Timeout = TimeSpan.FromSeconds(60)
        // }
        ;

        WebDownload = new(httpClient, reporter);

        await WebDownloadBootstrap();

        Initialize();

        await WebDownloadIPFSExe();

        await WebDownloadArteranos();


        // Do some background stuff here.
        await Task.Delay(3000);
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
        if (WebDownload == null) throw new Exception();

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
        if (WebDownload == null) throw new Exception();
        if (splash == null) throw new Exception();

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
            Console.WriteLine("Will resort to using IPFS.");
        }
        finally
        {
            if (File.Exists(arteranosArchiveFile)) File.Delete(arteranosArchiveFile);
            splash.Progress = 40;
        }
    }

    #endregion
}