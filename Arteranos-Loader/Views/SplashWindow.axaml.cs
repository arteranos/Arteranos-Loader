using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArteranosLoader.ViewModels;

namespace ArteranosLoader.Views;

public partial class SplashWindow : Window, IProgressReporter
{
    private Func<IProgressReporter, Task> _doStuff = pr => Task.CompletedTask;

    public int Progress
    {
        get => ViewModel?.Progress ?? 0;
        set
        {
            if(ViewModel == null) return;
            ViewModel.Progress = value;
        }
    }
    public string ProgressTxt
    {
        get => ViewModel?.ProgressTxt ?? string.Empty;
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

        await _doStuff.Invoke(this);
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Close();
        });
    }
}