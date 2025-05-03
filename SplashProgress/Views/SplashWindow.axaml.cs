using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SplashProgress.ViewModels;

namespace SplashProgress.Views;

public partial class SplashWindow : Window, IProgressReporter
{
    private Func<IProgressReporter, Task>? _doStuff = null;

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
    }

    public SplashWindow(Func<IProgressReporter, Task> action)
    {
        _doStuff = action;
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        DummyLoad();
    }
    
    private async void DummyLoad()
    {
        ViewModel = DataContext as SplashWindowViewModel;

        if(_doStuff != null)
            await _doStuff.Invoke(this);
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Close();
        });
    }
}