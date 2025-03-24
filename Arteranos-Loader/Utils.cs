﻿using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Arteranos_Loader
{
    internal static class Utils
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
            string common = null;

            using (TarInputStream tar = new(fs, Encoding.UTF8))
            {
                tar.IsStreamOwner = false;
                for (TarEntry entry = tar.GetNextEntry(); entry != null; entry = tar.GetNextEntry())
                {
                    if (entry.IsDirectory) continue;

                    common ??= entry.Name;

                    int i = common.CommonStart(entry.Name);
                    common = entry.Name[0..i];
                }
            }

            fs.Seek(0, SeekOrigin.Begin);
            int cutoff = common.Length;

            using (TarInputStream tar = new(fs, Encoding.UTF8))
            {
                for (TarEntry entry = tar.GetNextEntry(); entry != null; entry = tar.GetNextEntry())
                {
                    if (entry.IsDirectory) continue;

                    string filePath = $"{path}/{entry.Name[cutoff..]}";
                    string dirName = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                    using Stream stream = File.Create(filePath);
                    await tar.CopyEntryContentsAsync(stream, token);
                }
            }
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
    }
}
