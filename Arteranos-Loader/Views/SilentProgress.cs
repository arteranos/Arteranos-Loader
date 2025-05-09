using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArteranosLoader.ViewModels;

namespace ArteranosLoader.Views;

public class SilentProgress : IProgressReporter
{
    int _progress = 0;
    string _progressTxt = string.Empty;

    public int Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            Console.WriteLine($"{value}%");
        }
    }
    public string ProgressTxt
    {
        get => _progressTxt;
        set
        {
            _progressTxt = value;
            Console.WriteLine(value);
        }
    }

    public void Run()
    {
        Program.Loader?.Invoke(this).Wait();
    }
}