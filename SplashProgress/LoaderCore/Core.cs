using System;
using System.Threading.Tasks;
using SplashProgress.Views;

namespace SplashProgress.LoaderCore;

public class Core
{
    public Func<IProgressReporter, Task> Action { get => StartupAsync; }

    private void Startup(IProgressReporter reporter)
    {
        StartupAsync(reporter).Wait();
    }

    private async Task StartupAsync(IProgressReporter reporter)
    {
        reporter.ProgressTxt = "Initializing..";
        reporter.Progress = 20;

        // Do some background stuff here.
        await Task.Delay(3000);

        reporter.ProgressTxt = "Exiting..";
        reporter.Progress = 90;

        // Do some background stuff here.
        await Task.Delay(3000);
    }
}