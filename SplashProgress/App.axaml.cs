using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SplashProgress.ViewModels;
using SplashProgress.Views;

namespace SplashProgress;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if(Program.Loader == null) return;
            
            desktop.MainWindow = new SplashWindow(Program.Loader)
            {
                DataContext = new SplashWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
        
    }
}