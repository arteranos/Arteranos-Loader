using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loader
{
    internal class BootstrapData
    {
        public string ArteranosBootstrapData = "https://arteranos.github.io/BootstrapData.json";

        public string DeployBootstrapAddr = "/dns4/deploy.arteranos.ddnss.eu/tcp/4001/p2p/12D3KooWA1qSpKLjHqWemSW1gU5wJQdP8piBbDSQi6EEgqPVVkyc";
        public string PrimeBootstrapAddr = "/dns4/prime.arteranos.ddnss.eu/tcp/4001/p2p/12D3KooWA1qSpKLjHqWemSW1gU5wJQdP8piBbDSQi6EEgqPVVkyc";

        public string IPFSDeployDir = "/ipns/deployKeyTODO";

        public string KuboVersion = "v0.32.0";

        public string KuboWebDlRoot = "https://github.com/ipfs/kubo/releases/download";

        public string ArteranosWebDlRoot = "https://github.com/arteranos/Arteranos/releases/download/v3.0.0-pre";
    }
}
