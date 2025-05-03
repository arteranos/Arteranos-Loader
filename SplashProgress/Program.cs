using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;

using Mono.Options;
using SplashProgress.LoaderCore;
using SplashProgress.Views;

namespace SplashProgress;

class Program
{
    public static Func<IProgressReporter, Task>? Loader { get; private set; } = null;
    public static bool Server { get; private set; } = false;

    private static bool _quiet = false;
    private static bool _showHelp = false;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        ParseCommandlineOptions(args);        

        if(_showHelp) return 1;

        Core core = new();

        Loader = core.Action;
        
        if(!_quiet)
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        else
            new SilentProgress().Run();
        
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void ParseCommandlineOptions(string[] args)
    {
        List<string> extra;

        Console.WriteLine($"Commandline arguments: {string.Join(" ", args)}");

        OptionSet options = new()
        {
            { "q|quiet", "No splash/loading progress window", q => _quiet = q != null },
            { "s|server", "Run the dedicated server, implies -q", s => Server = s != null },
            { "h|help", "This text :)", h => _showHelp = h != null }
        };

        try
        {
            extra = options.Parse(args);

            if(Server) _quiet = true;
        }
        catch (OptionException)
        {
            _showHelp = true;
        }

        if(_showHelp) options.WriteOptionDescriptions(Console.Out);
    }
}