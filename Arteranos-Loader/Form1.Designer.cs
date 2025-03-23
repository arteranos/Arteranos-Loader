using System.Drawing;
using System.Windows.Forms;

namespace Loader
{
    partial class Splash
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Splash));
            this._Background = new System.Windows.Forms.PictureBox();
            this._ProgressBar = new System.Windows.Forms.ProgressBar();
            this._ProgressTxt = new System.Windows.Forms.Label();
            this._DisplayTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this._Background)).BeginInit();
            this.SuspendLayout();
            // 
            // _Background
            // 
            this._Background.BackColor = System.Drawing.Color.Black;
            this._Background.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("_Background.BackgroundImage")));
            this._Background.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this._Background.Location = new System.Drawing.Point(0, 0);
            this._Background.Margin = new System.Windows.Forms.Padding(0);
            this._Background.Name = "_Background";
            this._Background.Size = new System.Drawing.Size(640, 640);
            this._Background.TabIndex = 1;
            this._Background.TabStop = false;
            this._Background.Click += new System.EventHandler(this._Background_Click);
            // 
            // _ProgressBar
            // 
            this._ProgressBar.Location = new System.Drawing.Point(20, 600);
            this._ProgressBar.Margin = new System.Windows.Forms.Padding(0);
            this._ProgressBar.Name = "_ProgressBar";
            this._ProgressBar.Size = new System.Drawing.Size(600, 20);
            this._ProgressBar.Step = 0;
            this._ProgressBar.TabIndex = 2;
            // 
            // _ProgressTxt
            // 
            this._ProgressTxt.BackColor = System.Drawing.Color.Black;
            this._ProgressTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._ProgressTxt.ForeColor = System.Drawing.Color.White;
            this._ProgressTxt.Location = new System.Drawing.Point(20, 540);
            this._ProgressTxt.Name = "_ProgressTxt";
            this._ProgressTxt.Size = new System.Drawing.Size(600, 40);
            this._ProgressTxt.TabIndex = 3;
            this._ProgressTxt.Text = "label1";
            this._ProgressTxt.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _DisplayTimer
            // 
            this._DisplayTimer.Tick += new System.EventHandler(this._DisplayTimer_Tick);
            // 
            // Splash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 640);
            this.ControlBox = false;
            this.Controls.Add(this._ProgressTxt);
            this.Controls.Add(this._ProgressBar);
            this.Controls.Add(this._Background);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(640, 640);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 640);
            this.Name = "Splash";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Loader";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this._Background)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox _Background;
        private System.Windows.Forms.ProgressBar _ProgressBar;
        private System.Windows.Forms.Label _ProgressTxt;
        private Timer _DisplayTimer;
    }
}

