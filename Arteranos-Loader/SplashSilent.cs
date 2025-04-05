using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arteranos_Loader
{
    internal class SplashSilent : ISplash
    {
        public SplashSilent()
        {
            Task.Run(Program.LoaderWorkerThread);
        }

        public string ProgressTxt { get; set; } = "Initializing...";
        public int Progress { get; set; } = 0;
        public bool IsQuitting { get; set; } = false;

        public void Run()
        {
            while (!IsQuitting) Thread.Sleep(1000);
        }

        public void Exit()
        {
            IsQuitting = true;
        }
    }
}
