namespace Arteranos_Loader
{
    public interface ISplash
    {
        int Progress { get; set; }
        string ProgressTxt { get; set; }
        bool IsQuitting { get; set; }
        void Run();
        void Exit();
    }
}
