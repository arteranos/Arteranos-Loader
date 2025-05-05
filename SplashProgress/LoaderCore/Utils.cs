using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Ipfs;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Runtime.Versioning;

namespace SplashProgress.LoaderCore;

public enum FileStatus
{
    ToDelete = -1,
    Unchanged,
    ToPatch
}

public class FileEntry
{
    public Cid? Cid;
    public string? Path;
    public long Size;

    [JsonIgnore]
    public FileStatus Status;
}

public static class Utils
{

    public static async Task UnTarGzDirectoryAsync(string arteranosDir, string arteranosArchiveFile)
    {
        Directory.CreateDirectory(arteranosDir);
        using FileStream compressedFileStream = File.Open(arteranosArchiveFile, FileMode.Open);
        using MemoryStream outputFileStream = new();
        using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
        decompressor.CopyTo(outputFileStream);

        outputFileStream.Position = 0;
        await Utils.UnTarToDirectoryAsync(outputFileStream, arteranosDir).ConfigureAwait(false);
    }

    public static async Task UnTarToDirectoryAsync(Stream fs, string path, CancellationToken token = default)
    {
        using (TarInputStream tar = new(fs, Encoding.UTF8))
        {
            for (TarEntry entry = tar.GetNextEntry(); entry != null; entry = tar.GetNextEntry())
            {
                if (entry.IsDirectory) continue;

                string filePath = $"{path}/{entry.Name}";
                string dirName = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                using Stream stream = File.Create(filePath);
                await tar.CopyEntryContentsAsync(stream, token);
            }
        }
    }

    public static List<string> ListDirectory(string dir, bool recursive, bool self)
    {
        List<string> list = [];

        if (recursive)
        {
            foreach (string subdir in Directory.EnumerateDirectories(dir))
                list.AddRange(ListDirectory(subdir, recursive, false));
        }

        List<string> files = Directory.EnumerateFiles(dir).ToList();
        list.AddRange(files);

        if (self) list.Add(dir);

        return list;
    }

    public static void Exec(string cmd)
    {
        var escapedArgs = cmd.Replace("\"", "\\\"");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\""
            }
        };

        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception($"{escapedArgs} returned {process.ExitCode}");
    }

    public static void GetUsedPorts(HashSet<int> occupied)
    {
        static void AddCollection<T>(HashSet<T> set, IEnumerable<T> collection)
        {
            foreach (T t in collection) set.Add(t);
        }

        var properties = IPGlobalProperties.GetIPGlobalProperties();
        // Ignore active connections
        var connections = properties.GetActiveTcpConnections();
        AddCollection(occupied, from n in connections select n.LocalEndPoint.Port);

        // Ignore active tcp listners
        var endPoints = properties.GetActiveTcpListeners();
        AddCollection(occupied, from n in endPoints select n.Port);

        // Ignore active UDP listeners
        endPoints = properties.GetActiveUdpListeners();
        AddCollection(occupied, from n in endPoints select n.Port);
    }

    [SupportedOSPlatform("windows")]
    public static void SetWorldWritableWindows(string path)
    {
        FileInfo fi = new(path);

        FileSecurity fileSecurity = fi.GetAccessControl();
        SecurityIdentifier everyone = new(WellKnownSidType.AuthenticatedUserSid, null);
        FileSystemAccessRule rule = new(everyone, FileSystemRights.FullControl, AccessControlType.Allow);
        fileSecurity.AddAccessRule(rule);

        fi.SetAccessControl(fileSecurity);
    }

    public static void SetWorldWritable(string path)
    {
        if(OperatingSystem.IsWindows()) SetWorldWritableWindows(path);
        // else SerWorldWritableLKinux()
    }
}
