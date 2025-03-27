using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arteranos_Loader
{
    internal class BootstrapData
    {
        public string ArteranosBootstrapData;
        public string DeployBootstrapAddr;
        public string PrimeBootstrapAddr;
        public string IPFSDeployDir;
        public string KuboVersion;
        public string KuboWebDlRoot;
        public string ArteranosWebDlRoot;

        public static BootstrapData Defaults()
        {
            string json = @"
{
  ""ArteranosBootstrapData"": ""https://arteranos.github.io/BootstrapData.json"",
  ""DeployBootstrapAddr"": ""/dns4/deploy.arteranos.ddnss.eu/tcp/4001/p2p/12D3KooWA1qSpKLjHqWemSW1gU5wJQdP8piBbDSQi6EEgqPVVkyc"",
  ""PrimeBootstrapAddr"": ""/dns4/prime.arteranos.ddnss.eu/tcp/4001/p2p/12D3KooWA1qSpKLjHqWemSW1gU5wJQdP8piBbDSQi6EEgqPVVkyc"",
  ""IPFSDeployDir"": ""/ipns/12D3KooWFYS1mqjmmNiiTCdoKaBwohZ5kA8npagPMCxVHGhCtJcv"",
  ""KuboVersion"": ""v0.33.2"",
  ""KuboWebDlRoot"": ""https://github.com/ipfs/kubo/releases/download"",
  ""ArteranosWebDlRoot"": ""https://github.com/arteranos/Arteranos/releases/download/v3.0.0-pre""
}
";
            return JsonConvert.DeserializeObject<BootstrapData>(json);
        }
    }
}
