﻿using Ipfs;
using Ipfs.Cryptography.Proto;
using Ipfs.Http;
using Arteranos_Loader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


namespace Arteranos_Loader
{
    public static class IPFSConnection
    {
        // API ports to avoid
        public static readonly HashSet<int> bannedAPIPorts =
    [
        5001, // Default API
            4001, // Default IPFS
            8080, // Default Web Gateway 
        ];

        public enum Status
        {
            OK = 0,
            NoDaemonExecutable,
            NoRepository,
            CommandFailed,
            DeadDaemon,
            PortSquatter
        }

        public static IpfsClientEx Ipfs { get; private set; } = null;
        public static Peer Self { get; private set; } = null;
        public static string RepoDir { get; private set; } = null;

        private static bool? _IPFSAccessible = null;
        private static bool? _RepoExists = null;
        private static bool? _DaemonRunning = null;
        private static int? APIPort = null;

        private static ProcessStartInfo BuildDaemonCommand(string arguments, bool includeDefaultOptions = true)
        {
            List<string> sb = [];

            if (includeDefaultOptions)
            {
                // Just set up the suitable RepoDir, don't care if it actually exists or not.
                CheckRepository(false);

                sb.Add($"--repo-dir={RepoDir}");
            }

            if (arguments != null)
                sb.Add(arguments);

            string argLine = string.Join(" ", sb);

            return new()
            {
                FileName = Program.IPFSExePath,
                Arguments = argLine,
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                CreateNoWindow = true,
            };
        }

        private static Status RunDaemonCommand(ProcessStartInfo psi, bool synced)
        {
            if (IPFSAccessible() != Status.OK) return IPFSAccessible();

            try
            {
                // Console.WriteLine($"Command: {psi.FileName} {psi.Arguments}");
                Process process = Process.Start(psi);

                if (synced) process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Status.CommandFailed;
            }

            return Status.OK;
        }

        public static Status RunDaemonCommand(string arguments, bool synced)
        {
            ProcessStartInfo psi = BuildDaemonCommand(arguments);
            return RunDaemonCommand(psi, synced);
        }

        public static Status IPFSAccessible()
        {
            // Got it earlier?
            if (_IPFSAccessible != null)
                return _IPFSAccessible.Value ? Status.OK : Status.NoDaemonExecutable;

            Console.WriteLine($"Probing for viable IPFS backend executable ({Program.IPFSExePath})");
            // Just try 'ipfs.exe help'
            ProcessStartInfo psi = BuildDaemonCommand("help", false);

            try
            {
                Process process = Process.Start(psi);

                _IPFSAccessible = true;
                return Status.OK;
            }
            catch
            {
                _IPFSAccessible = false;
                return Status.NoDaemonExecutable;
            }
        }

        public static Status CheckRepository(bool reinitAllowed)
        {
            if (_RepoExists ?? false) return Status.OK;

            RepoDir = $"{Program.PersistentDataPath}/.ipfs";

            if(!Directory.Exists(RepoDir)) Directory.CreateDirectory(RepoDir);

            try
            {
                _ = IpfsClientEx.ReadDaemonPrivateKey(RepoDir);

                _RepoExists = true;
                return Status.OK;
            }
            catch
            {
                if (!reinitAllowed)
                {
                    // Just checking, and it isn't there. So, stop right now.
                    _RepoExists = false;
                    return Status.NoRepository;
                }
            }

            ProcessStartInfo psi = BuildDaemonCommand("init");
            if (RunDaemonCommand(psi, true) != Status.OK)
            {
                // Init failed!
                _RepoExists = false;
                return Status.NoRepository;
            }

            // Daemon says we're successful, but we have to verify.
            _RepoExists = null;
            return CheckRepository(false);
        }

        public static Status AddBootstraps()
        {
            ProcessStartInfo psi = BuildDaemonCommand($"bootstrap add {Program.BootstrapData.DeployBootstrapAddr}");
            Status res = RunDaemonCommand(psi, true);

            if (res != Status.OK) return res;

            psi = BuildDaemonCommand($"bootstrap add {Program.BootstrapData.PrimeBootstrapAddr}");
            return RunDaemonCommand(psi, true);
        }

        public static Status StartDaemon(bool forceRestart)
        {
            if ((_DaemonRunning ?? false) && !forceRestart) return Status.OK;
            if (forceRestart) StopDaemon();

            ProcessStartInfo psi = BuildDaemonCommand("daemon --enable-pubsub-experiment");
            Status res = RunDaemonCommand(psi, false);
            _DaemonRunning = res == Status.OK;
            return res;
        }

        public static Status StopDaemon()
        {
            ProcessStartInfo psi = BuildDaemonCommand("shutdown");
            _DaemonRunning = null;
            return RunDaemonCommand(psi, true);
        }

        public static int GetAPIPort()
        {
            if (APIPort != null) return APIPort.Value;

            if (CheckRepository(false) != Status.OK) return -1;
            int port = -1;

            try
            {
                MultiAddress apiAddr = IpfsClientEx.ReadDaemonAPIAddress(RepoDir);
                foreach (NetworkProtocol protocol in apiAddr.Protocols)
                    if (protocol.Code == 6)
                    {
                        port = int.Parse(protocol.Value);
                        break;
                    }
            }
            catch { }

            APIPort = port;
            return port;
        }

        public static async Task<Status> CheckAPIConnection(int attempts, bool verify = true)
        {
            int port = GetAPIPort();

            if (port < 0) return Status.NoRepository;

            IpfsClientEx ipfs = new($"http://localhost:{port}");

            Status status = Status.DeadDaemon;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    _ = await ipfs.Config.GetAsync();
                    status = Status.OK;
                    break;
                }
                catch // (Exception es) 
                {
                    // Debug.LogException(es);
                }

                await Task.Delay(1000);
            }

            if (status != Status.OK || !verify) return status;

            try
            {
                PrivateKey pk = IpfsClientEx.ReadDaemonPrivateKey(RepoDir);

                await ipfs.VerifyDaemonAsync(pk);

                Ipfs = ipfs;

                Self = await ipfs.IdAsync();
            }
            catch // (Exception ex)
            {
                // Debug.LogError(ex);
                return Status.PortSquatter;
            }

            return Status.OK;
        }

        public static void ReconfigurePorts(int apiPort, int ipfsPort)
        {
            // Reconfigure API port
            ProcessStartInfo psi = BuildDaemonCommand($"config Addresses.API /ip4/127.0.0.1/tcp/{apiPort}");
            _ = RunDaemonCommand(psi, true);

            // Reconfigure IPFS port
            psi = BuildDaemonCommand($"config --json Addresses.Swarm " +
                $"\"[ " +
                $"  \\\"/ip4/0.0.0.0/tcp/{ipfsPort}\\\", " +
                $"  \\\"/ip6/::/tcp/{ipfsPort}\\\", " +
                $"  \\\"/ip4/0.0.0.0/udp/{ipfsPort}/webrtc-direct\\\", " +
                $"  \\\"/ip4/0.0.0.0/udp/{ipfsPort}/quic-v1\\\", " +
                $"  \\\"/ip4/0.0.0.0/udp/{ipfsPort}/quic-v1/webtransport\\\", " +
                $"  \\\"/ip6/::/udp/{ipfsPort}/webrtc-direct\\\", " +
                $"  \\\"/ip6/::/udp/{ipfsPort}/quic-v1\\\", " +
                $"  \\\"/ip6/::/udp/{ipfsPort}/quic-v1/webtransport\\\" " +
                $"]\"");
            _ = RunDaemonCommand(psi, true);


            // Additionally, remove the Web Gateway port.
            psi = BuildDaemonCommand("config --json Addresses.Gateway []");
            _ = RunDaemonCommand(psi, true);

            // Reset detection data in lieu of the changed settings
            APIPort = null;
            _RepoExists = null;
            Ipfs = null;
            _IPFSAccessible = null;
            _RepoExists = null;
        }
    }
}
