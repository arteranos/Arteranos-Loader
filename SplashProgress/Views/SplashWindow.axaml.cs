using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SplashProgress.ViewModels;

namespace SplashProgress.Views;

public partial class SplashWindow : Window
{
    public int Progress
    {
        set
        {
            if(ViewModel == null) return;
            ViewModel.Progress = value;
        }
    }
    public string ProgressTxt
    {
        set
        {
            if(ViewModel == null) return;
            ViewModel.ProgressTxt = value;
        }
    }

    private SplashWindowViewModel? ViewModel = null;

    public SplashWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        DummyLoad();
    }
    
    private async void DummyLoad()
    {
        ViewModel = DataContext as SplashWindowViewModel;

        ProgressTxt = "Initializing..";
        Progress = 20;

        // Do some background stuff here.
        await Task.Delay(3000);

        ProgressTxt = "Exiting..";
        Progress = 90;

        // Do some background stuff here.
        await Task.Delay(3000);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Close();
        });
    }
}