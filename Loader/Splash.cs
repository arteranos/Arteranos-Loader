using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Loader
{
    public partial class Splash : Form
    {
        public Splash()
        {
            InitializeComponent();
            
            _DisplayTimer.Start();

            Task.Run(Program.LoaderWorkerThread);
        }

        public string ProgressTxt { get; set; } = "Initializing...";
        public int Progress { get; set; } = 0;

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            _ProgressBar.Value = Progress;
            _ProgressTxt.Text = ProgressTxt;
        }
    }
}
