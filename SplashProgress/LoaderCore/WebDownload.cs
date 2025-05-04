using System;
using System.Threading.Tasks;
using System.Net.Http;
using SplashProgress.Views;
using System.IO;
using Newtonsoft.Json;

namespace SplashProgress.LoaderCore;

public class WebDownload
{
    private HttpClient _client;
    private IProgressReporter _reporter;

    public WebDownload(HttpClient client, IProgressReporter reporter)
    {
        _client = client;
        _reporter = reporter;
    }

    private Action<long, long> ReportProgress(string pattern, int frompercent, int topercent)
    {
        void ReportProgressFunc(long actual, long total)
        {
            float normalized = (float)actual / total;
            string msg = string.Format(pattern, Magnitude(actual), Magnitude(total));
            int progress = (int)(frompercent + ((float)topercent - frompercent) * normalized);

            _reporter.Progress = progress;
            _reporter.ProgressTxt = msg;
        }


        return (actual, total) => ReportProgressFunc(actual, total);
    }

    private string Magnitude(long value, string suffix = "B")
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

    public async Task WebDownloadFileAsync(string source, string target, string what, int to)
    {
        int from = _reporter.Progress;

        if (File.Exists(target)) File.Delete(target);

        using Stream file = File.Create(target);

        try
        {
            await _client.DownloadAsync(source, file, ReportProgress($"{what} ({{0}} of {{1}})", from, to));
        }
        catch (Exception)
        {
            File.Delete(target);
            throw;
        }

    }

}