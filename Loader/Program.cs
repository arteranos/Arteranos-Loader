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

namespace Loader
{
    internal static class Program
    {
        private const string COMPANYNAME = "arteranos";


        private const string KUBO_ARCH_WIN64 = "windows-amd64";
        private const string KUBO_ARCH_LINUX64 = "linux-amd64";


        public static bool IsOnLinux { get; private set; } = false;
        public static string ProgDataDir { get; private set; } = null;
        public static string IPFSExeName { get; private set; } = null;
        public static string IPFSExePath { get; private set; } = null;
        public static string ArteranosUserData {  get; private set; } = null;
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
            await WebDownloadBootstrap();

            Initialize();

            await WebDownloadIPFSExe(ipfsArchiveSource, ipfsExeInArchive).ConfigureAwait(false);

            await WebDownloadArteranos().ConfigureAwait(false);

            for (int i = 4; i < 10; i++)
            {
                splash.Progress = i * 10;

                await Task.Delay(1000);
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
            }
            else
            {
                string pdroot = Environment.GetEnvironmentVariable("PROGRAMDATA");
                ProgDataDir = $"{pdroot}/{COMPANYNAME}/arteranos";
                IPFSExeName = "ipfs.exe";

                ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_WIN64}.zip";
                ipfsExeInArchive = $"/kubo/{IPFSExeName}";
                osArchitecture = "Win-amd64";
            }

            Console.WriteLine($"Program Dir: {ProgDataDir}");
            Console.WriteLine($"IPFS Exe   : {IPFSExeName}");
            Console.WriteLine($"IPFS source: {ipfsArchiveSource}");
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

        private static async Task WebDownloadIPFSExe(string url, string fileInArchive)
        {
            IPFSExePath = $"{ProgDataDir}/{IPFSExeName}";
            string target = $"{ProgDataDir}/downloaded-kubo-ipfs";
            string targetDir = $"{target}.dir";
            string archiveFormat = Path.GetExtension(url);

            // Got a version, skip downloading via web.
            if (File.Exists(IPFSExePath))
            {
                Console.WriteLine("Web download of IPFS skipped, already one version there");
                return;
            }

            try
            {
                await WebDownloadFileAsync(url, target, "D/l IPFS from web", 10).ConfigureAwait(false);

                splash.ProgressTxt = "Extracting IPFS from web...";

                if (archiveFormat == ".zip")
                    ZipFile.ExtractToDirectory(target, targetDir);
                else if (archiveFormat == ".tar.gz")
                    await Utils.UnTarGzDirectoryAsync(target, targetDir).ConfigureAwait(false);

                File.Copy($"{targetDir}/{fileInArchive}", IPFSExePath);

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

            if (Directory.Exists(arteranosDir)) return;

            try
            {
                await WebDownloadFileAsync(arteranosURL, arteranosArchiveFile, "D/l Arteranos from web", 30).ConfigureAwait(false);

                splash.ProgressTxt = "Extracting Arteranos from web...";

                await Utils.UnTarGzDirectoryAsync(arteranosDir, arteranosArchiveFile).ConfigureAwait(false);
            }
            finally
            {
                if (File.Exists(arteranosArchiveFile)) File.Delete(arteranosArchiveFile);
                splash.Progress = 40;
            }
        }
    }
}
