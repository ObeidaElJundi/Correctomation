using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace correctomation
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
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

        private void Form3_Resize(object sender, EventArgs e)
        {
            Console.WriteLine(" >>> ResizeEnd");
            this.Invalidate();
        }

        private void button_cpps_dir_Click(object sender, EventArgs e)
        {
            string cppsDirectory = directoryDialog();
            if (!string.IsNullOrEmpty(cppsDirectory))
            {
                textBox_cpps_dir.Text = cppsDirectory;
            }
            //TODO: handle else
        }

        private void button_in_out_Click(object sender, EventArgs e)
        {
            string inOutDirectory = directoryDialog();
            if (!string.IsNullOrEmpty(inOutDirectory))
            {
                textBox_in_out.Text = inOutDirectory;
            }
            //TODO: handle else
        }

        private string directoryDialog()
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return string.Empty;
        }

        private void start(string path, string cppFile)
        {
            foreach (string dir in Directory.GetDirectories(path))
            {
                //ArrayList txtFiles = getSpecificFilesRecursively(dir, "*.txt"); //should be one txt file
                //ArrayList cppFiles = getSpecificFilesRecursively(dir, "*.cpp");

                //get student number, which is the last chunk of the path
                string[] n = dir.Split(Path.DirectorySeparatorChar);
                string studentNumber = n[n.Length - 1];

                //get student name: the txt file name (assuming there is one txt file only)
                //clean txt file name: remove extention and replace underscore by space
                string[] s = getSpecificFileRecursively(dir, "*.txt").Split(Path.DirectorySeparatorChar);
                string studentName = s[s.Length - 1].Replace(".txt", "").Replace("_", " ");

                //get the path of the cpp file to be compiled
                string cppFilePath = getSpecificFileRecursively(dir, cppFile);
                Console.WriteLine(studentNumber + " >> " + studentName + " >> " + cppFilePath + " <<");
            }
        }

        //returns a list of all the files in a particular directory recursilvely
        private ArrayList listFilesRecursively(string path)
        {
            ArrayList ls = new ArrayList();
            foreach (string f in Directory.GetFiles(path)) ls.Add(f);
            foreach (string dir in Directory.GetDirectories(path)) ls.AddRange(listFilesRecursively(dir));
            return ls;
        }

        //returns a list of all txt files in a particular directory recursilvely
        private ArrayList getSpecificFilesRecursively(string path, string pattern)
        {
            ArrayList ls = new ArrayList();
            foreach (string f in Directory.GetFiles(path,pattern)) ls.Add(f);
            foreach (string dir in Directory.GetDirectories(path)) ls.AddRange(getSpecificFilesRecursively(dir, pattern));
            return ls;
        }

        //returns a specific file (txt or cpp) in a particular directory recursilvely
        private string getSpecificFileRecursively(string path, string pattern)
        {
            ArrayList ls = new ArrayList();
            foreach (string f in Directory.GetFiles(path, pattern)) ls.Add(f);
            foreach (string dir in Directory.GetDirectories(path)) ls.AddRange(getSpecificFilesRecursively(dir, pattern));
            if (ls.Count > 0) return ls[0].ToString();
            else return string.Empty;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            start(textBox_cpps_dir.Text, "p1a.cpp");
        }

    }
}
