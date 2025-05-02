using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SplashProgress.ViewModels;

public partial class SplashWindowViewModel : ViewModelBase
{
    [ObservableProperty] public string? _progressTxt = string.Empty;

    [ObservableProperty] public int _progress = 0;
}
