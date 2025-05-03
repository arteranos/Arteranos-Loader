namespace SplashProgress.Views;

public interface IProgressReporter
{
    public int Progress { set; }
    public string ProgressTxt { set; }
}