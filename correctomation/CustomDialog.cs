using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace correctomation
{
    public partial class CustomDialog : Form
    {

        private string resultsPath;

        public CustomDialog(string resultsPath)
        {
            InitializeComponent();
            this.resultsPath = resultsPath;
        }

        private void CustomDialog_Load(object sender, EventArgs e)
        {

        }

        //set background gradient
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (this.ClientRectangle.Width != 0 && this.ClientRectangle.Height != 0)
            {
                LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle, Color.White, System.Drawing.SystemColors.GradientActiveCaption, 90F);
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        private void button_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button_back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button_open_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(resultsPath);
            this.Close();
        }
    }
}
