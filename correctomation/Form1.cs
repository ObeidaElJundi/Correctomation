using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;

namespace correctomation
{
    public partial class Form1 : Form
    {

        private string visualStudioInstalledPath = string.Empty;
        private string binPath = string.Empty;
        private System.Diagnostics.Process proc = new System.Diagnostics.Process();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            visualStudioInstalledPath = getVisualStudioInstalledPath();
            binPath = new DirectoryInfo(visualStudioInstalledPath).Parent.Parent.FullName + Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar + "bin";
            if (!string.IsNullOrEmpty(visualStudioInstalledPath))
            {
                //setEnvironmentVars();
            }
            else
            {
                MessageBox.Show("Can't get Visual Studio installation path!");
            }
        }


        private void compileCPP(string cppPath)
        {
            //var proc = new System.Diagnostics.Process();
            Console.WriteLine("bin path = " + binPath + Path.DirectorySeparatorChar + "cl.exe" + " /EHsc " + cppPath);
            //proc.StartInfo.FileName = binPath + Path.DirectorySeparatorChar + "cl.exe"; //C++ compiler (cl.exe)
            proc.StartInfo.FileName = "cl.exe"; //C++ compiler (cl.exe)
            proc.StartInfo.Arguments = "/EHsc \"" + cppPath + "\"";  //cpp file to be compiled
            proc.StartInfo.Verb = "runas";  //run command, or process, as admin
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();  //run the process >> compile
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(); // call only if you would like that the program execution waits until that process finishes
            Console.WriteLine("compilation output : " + output);
            Console.WriteLine("compileCPP process exit code = " + proc.ExitCode);

        }

        private void setEnvironmentVars()
        {
            binPath = new DirectoryInfo(visualStudioInstalledPath).Parent.Parent.FullName + Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar + "bin";
            //processes >> vcvars32.bat as Admin
            //var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = binPath + Path.DirectorySeparatorChar + "vcvars32.bat"; //C++ compiler (cl.exe)
            //proc.StartInfo.Arguments = "/EHsc " + cppPath;  //cpp file to be compiled
            proc.StartInfo.Verb = "runas";  //run command, or process, as admin
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();  //run the process >> compile
            string output = proc.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            proc.WaitForExit(); // call only if you would like that the program execution waits until that process finishes
            Console.WriteLine("setEnvironmentVars process exit code = " + proc.ExitCode);
        }

        private void setEnvironmentVarsThenCompileCpp(string cppPath)
        {
            string varsPath = "\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"";
            string compileCommand = "cl /EHsc \"" + cppPath + "\"";
            //binPath = new DirectoryInfo(visualStudioInstalledPath).Parent.Parent.FullName + Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar + "bin";
            //processes >> vcvars32.bat as Admin
            //var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "cmd.exe";
            //proc.StartInfo.Arguments = "/k " + varsPath + " && " + compileCommand;  //cpp file to be compiled
            Console.WriteLine(proc.StartInfo.Arguments);
            proc.StartInfo.Verb = "runas";  //run command, or process, as admin
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;

            proc.Start();  //run the process >> compile
            string output = string.Empty;
            StreamWriter sw = proc.StandardInput;
            StreamReader sr = proc.StandardOutput;
            StreamReader err = proc.StandardError;
            sw.AutoFlush = true;
            sw.WriteLine("\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"");
            sw.WriteLine("cl /EHsc \"" + cppPath + "\""); // add -o >> redirect
            sw.Close();
            output = sr.ReadToEnd();
            output += err.ReadToEnd();
            Console.WriteLine(output);
            //sw.WriteLine("cl /EHsc \"" + cppPath + "\"");
            //output += sr.ReadToEnd();
            //output += err.ReadToEnd();
            //Console.WriteLine(output);
            //sw.Close();
            proc.WaitForExit(); // call only if you would like that the program execution waits until that process finishes
            Console.WriteLine("setEnvironmentVarsThenCompileCpp process exit code = " + proc.ExitCode);
        }

        private string getVisualStudioInstalledPath() {
            string visualStudioRegistryKeyPath = @"SOFTWARE\Microsoft\VisualStudio";
            List<Version> vsVersions = new List<Version>() { new Version("15.0"), new Version("14.0"), new Version("13.0"), new Version("12.0"), new Version("11.0"), new Version("10.0"), new Version("9.0"), new Version("8.0") };
            foreach (var version in vsVersions)
            {
                    RegistryKey registryBase32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                    RegistryKey vsVersionRegistryKey = registryBase32.OpenSubKey(string.Format(@"{0}\{1}.{2}",visualStudioRegistryKeyPath, version.Major, version.Minor));
                    if (vsVersionRegistryKey != null)
                    {
                        string path = vsVersionRegistryKey.GetValue("InstallDir", string.Empty).ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            Console.WriteLine("PATH = " + path);
                            return path;
                        }
                    }
            }
            return null;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[]; // get all files droppeds  
            compileCPP(files.First());
            //if (files != null && files.Any())
              //  MessageBox.Show(files.First());
        }

        private void extractZip(string zipPath)
        {
            
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
            /*if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;*/
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog opd = new OpenFileDialog();
            DialogResult result = opd.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                try
                {
                    string file = opd.FileName;
                    //compileCPP(file);
                    setEnvironmentVarsThenCompileCpp(file);
                }
                catch (Exception ex) {
                    Console.WriteLine("Exception : " + ex.Message);
                }
            }
        }

    }
}
