using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Arteranos_Loader
{
    public partial class Splash : Form, ISplash
    {
        public Splash()
        {
            InitializeComponent();

            _DisplayTimer.Start();

            Task.Run(Program.LoaderWorkerThread);
        }

        public string ProgressTxt { get; set; } = "Initializing...";
        public int Progress { get; set; } = 0;
        public bool IsQuitting { get; set; } = false;

        public void Run()
        {
            Application.Run(this);
        }

        public void Exit()
        { 
            IsQuitting = true;
            Application.Exit(); 
        }

        private void Background_Click(object sender, EventArgs e)
        {

        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            _ProgressBar.Value = Progress;
            _ProgressTxt.Text = ProgressTxt;
        }
    }
}
