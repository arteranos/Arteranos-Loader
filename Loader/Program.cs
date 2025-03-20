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

        private const string KUBO_VERSION = "v0.32.0";

        private static readonly string KUBO_EXECUTABLE_ROOT = $"https://github.com/ipfs/kubo/releases/download/{KUBO_VERSION}/kubo_{KUBO_VERSION}";
        private const string KUBO_ARCH_WIN64 = "windows-amd64";
        private const string KUBO_ARCH_LINUX64 = "linux-amd64";

        public static bool IsOnLinux { get; private set; } = false;
        public static string ProgDataDir { get; private set; } = null;
        public static string IPFSExeName { get; private set; } = null;
        public static string IPFSExePath { get; private set; } = null;


        private static string ipfsArchiveSource;
        private static string ipfsExeInArchive;


        private static Splash splash = null;

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Initialize();

            Application.Run(splash);
        }

        public static async Task LoaderWorkerThread()
        {
            await WebDownloadIPFSExe(ipfsArchiveSource, ipfsExeInArchive).ConfigureAwait(false);

            for (int i = 2; i < 10; i++)
            {
                splash.Progress = i * 10;

                await Task.Delay(1000);
            }

            Application.Exit();
        }

        private static readonly HttpClient HttpClient = new();

        private static void Initialize()
        {
            splash = new();

            IsOnLinux = Environment.OSVersion.Platform == PlatformID.Unix;

            string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (IsOnLinux)
            {
                ProgDataDir = $"{userHomeDir}/{COMPANYNAME}/arteranos";
                IPFSExeName = "ipfs";

                ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_LINUX64}.tsr.gz";
                ipfsExeInArchive = $"/{IPFSExeName}";
            }
            else
            {
                string pdroot = Environment.GetEnvironmentVariable("PROGRAMDATA");
                ProgDataDir = $"{pdroot}/{COMPANYNAME}/arteranos";
                IPFSExeName = "ipfs.exe";

                ipfsArchiveSource = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_WIN64}.zip";
                ipfsExeInArchive = $"/kubo/{IPFSExeName}";
            }

            Console.WriteLine($"Program Dir: {ProgDataDir}");
            Console.WriteLine($"IPFS Exe   : {IPFSExeName}");
            Console.WriteLine($"IPFS source: {ipfsArchiveSource}");

            if(!Directory.Exists(ProgDataDir)) Directory.CreateDirectory(ProgDataDir);
        }

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
                string[] prefixes = { "", "k", "M", "G", "T", "E" };
                for (int i = 0; i < prefixes.Length - 1; i++)
                {
                    if (val < 900) return string.Format("{0:F1} {1}{2}", val, prefixes[i], suffix);
                    // SI numbers prefixes, sorry, no powers of two...
                    val /= 1000;
                }
                return string.Format("{0:F1} {1}{2}", val, prefixes[prefixes.Length-1], suffix);
            }


            return (actual, total) => ReportProgressFunc(actual, total);
        }

        private static async Task WebDownloadFileAsync(string source, string target, string what, int from, int to)
        {
            if (File.Exists(target)) File.Delete(target);

            HttpClient.Timeout = TimeSpan.FromSeconds(60);

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
                await WebDownloadFileAsync(url, target, "D/l IPFS from web", 0, 10).ConfigureAwait(false);

                splash.ProgressTxt = "Extracting IPFS from web...";

                if (archiveFormat == ".zip")
                    ZipFile.ExtractToDirectory(target, targetDir);

                File.Copy($"{targetDir}/{fileInArchive}", IPFSExePath);

            }
            finally
            {
                if(File.Exists(target)) File.Delete(target);
                if(Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                splash.Progress = 20;
            }
        }
    }
}
