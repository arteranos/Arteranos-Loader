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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Splash));
            _Background = new PictureBox();
            _ProgressBar = new ProgressBar();
            _ProgressTxt = new Label();
            _DisplayTimer = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)_Background).BeginInit();
            SuspendLayout();
            // 
            // _Background
            // 
            _Background.BackColor = Color.Black;
            _Background.BackgroundImage = (Image)resources.GetObject("_Background.BackgroundImage");
            _Background.BackgroundImageLayout = ImageLayout.Stretch;
            _Background.Location = new Point(0, 0);
            _Background.Margin = new Padding(0);
            _Background.Name = "_Background";
            _Background.Size = new Size(640, 640);
            _Background.TabIndex = 1;
            _Background.TabStop = false;
            // 
            // _ProgressBar
            // 
            _ProgressBar.Location = new Point(20, 580);
            _ProgressBar.Margin = new Padding(0);
            _ProgressBar.Name = "_ProgressBar";
            _ProgressBar.Size = new Size(600, 33);
            _ProgressBar.Step = 0;
            _ProgressBar.TabIndex = 2;
            // 
            // _ProgressTxt
            // 
            _ProgressTxt.BackColor = Color.Black;
            _ProgressTxt.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _ProgressTxt.ForeColor = Color.White;
            _ProgressTxt.Location = new Point(20, 520);
            _ProgressTxt.Margin = new Padding(5, 0, 5, 0);
            _ProgressTxt.Name = "_ProgressTxt";
            _ProgressTxt.Size = new Size(600, 40);
            _ProgressTxt.TabIndex = 3;
            _ProgressTxt.Text = "label1";
            _ProgressTxt.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _DisplayTimer
            // 
            _DisplayTimer.Tick += DisplayTimer_Tick;
            // 
            // Splash
            // 
            AutoScaleDimensions = new SizeF(15F, 38F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 640);
            ControlBox = false;
            Controls.Add(_ProgressTxt);
            Controls.Add(_ProgressBar);
            Controls.Add(_Background);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(5, 6, 5, 6);
            MaximizeBox = false;
            MaximumSize = new Size(640, 640);
            MinimizeBox = false;
            MinimumSize = new Size(640, 640);
            Name = "Splash";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Loader";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)_Background).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox _Background;
        private System.Windows.Forms.ProgressBar _ProgressBar;
        private System.Windows.Forms.Label _ProgressTxt;
        private System.Windows.Forms.Timer _DisplayTimer;
    }
}

