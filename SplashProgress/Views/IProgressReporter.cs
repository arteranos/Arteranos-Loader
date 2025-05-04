namespace SplashProgress.Views;

public interface IProgressReporter
{
    public int Progress { get; set; }
    public string ProgressTxt { get; set; }
}