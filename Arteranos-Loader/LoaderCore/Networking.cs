using Ipfs;
using Ipfs.Cryptography.Proto;
using Ipfs.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace ArteranosLoader.LoaderCore;

public class Networking
{

    // Services from https://stackoverflow.com/questions/3253701/get-public-external-ip-address
    private static readonly List<string> services =
    [
        "https://ipv4.icanhazip.com",
        "https://api.ipify.org",
        "https://ipinfo.io/ip",
        "https://checkip.amazonaws.com",
        "https://wtfismyip.com/text",
        "http://icanhazip.com"
    ];


    public static async Task<IPAddress?> GetExternalIPAddress()
    {
        using HttpClient webclient = new();

        async Task<IPAddress> GetMyIPAsync(string service, CancellationToken cancel)
        {
            try
            {
                HttpResponseMessage response = await webclient.GetAsync(service, cancel);
                string ipString = await response.Content.ReadAsStringAsync();

                // https://ihateregex.io/expr/ip
                Match m = Regex.Match(ipString, @"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}");

                if(!m.Success) throw new InvalidDataException($"{service} yielded no viable IP address");
                return IPAddress.Parse(m.Value);
            }
            catch { throw; }
        }

        services.Shuffle();

        foreach(string service in services)
        {
            using CancellationTokenSource cts = new(1000);

            try
            {
                return await GetMyIPAsync(service, cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        return null;
    }
}