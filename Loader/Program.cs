using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
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

        private static HttpClient HttpClient = new();

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

        private static Task WebDownloadFile(string source, string target)
        {
            return Task.Run(async () =>
            {
                long totalBytes = 0;
                if (File.Exists(target)) File.Delete(target);

                HttpClient.Timeout = TimeSpan.FromSeconds(60);
                using HttpResponseMessage response = await HttpClient.GetAsync(source).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                totalBytes = response.Content.Headers.ContentLength ?? -1;
                byte[] binary = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                using Stream s = File.Create(target);
                s.Write(binary, 0, binary.Length);
                s.Flush();
                s.Close();
            });
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
                splash.ProgressTxt = "Downloading IPFS from web...";

                await WebDownloadFile(url, target).ConfigureAwait(false);

                splash.Progress = 10;

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
