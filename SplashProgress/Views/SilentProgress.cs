using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SplashProgress.ViewModels;

namespace SplashProgress.Views;

public class SilentProgress : IProgressReporter
{
    public int Progress 
    { 
        set => Console.WriteLine($"{value}%");
    }
    public string ProgressTxt 
    { 
        set => Console.WriteLine(value);
    }

    public void Run() => Program.Loader?.Invoke(this);
}