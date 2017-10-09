using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace correctomation
{
    public partial class Form2 : Form
    {

        private string visualStudioInstallationPath = string.Empty;
        private string binPath = string.Empty;
        private Process process = new Process();
        /*private StreamWriter sw;
        private StreamReader sr, err;*/

        public Form2()
        {
            InitializeComponent();

            initPaths();
            initAndStartProcess();
            initEnvironmentVariables();
        }

        //initialize installation and bin path
        private void initPaths()
        {
            if (Correctomation.Properties.Settings.Default.pathsInitialized == true)
            {
                getPaths();
            }
            else
            {
                getVisualStudioInstallationAndBinPathFromRegistryKey();
            }
        }

        private void getVisualStudioInstallationAndBinPathFromRegistryKey()
        {
            string visualStudioRegistryKeyPath = @"SOFTWARE\Microsoft\VisualStudio";
            List<Version> vsVersions = new List<Version>() { new Version("15.0"), new Version("14.0"), new Version("13.0"), new Version("12.0"), new Version("11.0"), new Version("10.0"), new Version("9.0"), new Version("8.0") };
            foreach (var version in vsVersions)
            {
                RegistryKey registryBase32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey vsVersionRegistryKey = registryBase32.OpenSubKey(string.Format(@"{0}\{1}.{2}", visualStudioRegistryKeyPath, version.Major, version.Minor));
                if (vsVersionRegistryKey != null)
                {
                    string installationPath = vsVersionRegistryKey.GetValue("InstallDir", string.Empty).ToString();
                    if (!string.IsNullOrEmpty(installationPath))
                    {
                        visualStudioInstallationPath = installationPath;
                        binPath = new DirectoryInfo(visualStudioInstallationPath).Parent.Parent.FullName + Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar + "bin";
                        savePaths();
                        return;
                    }
                }
            }
        }

        //save paths internally, so they can be retrieved next time the app is opened
        private void savePaths()
        {
            Correctomation.Properties.Settings.Default.binPath = binPath;
            Correctomation.Properties.Settings.Default.installationPath = visualStudioInstallationPath;
            Correctomation.Properties.Settings.Default.pathsInitialized = true;
            Correctomation.Properties.Settings.Default.Save();
        }

        //retrieve saved paths
        private void getPaths()
        {
            binPath = Correctomation.Properties.Settings.Default.binPath;
            visualStudioInstallationPath = Correctomation.Properties.Settings.Default.installationPath;
        }





        private void initAndStartProcess()
        {
            process.StartInfo.FileName = "cmd.exe";  //process is cmd (command prompt)
            process.StartInfo.Verb = "runas";  //run command, or process, as admin
            process.StartInfo.CreateNoWindow = true;  //do not show command prompt window
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;  //do not show command prompt window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;  //Handle out manually
            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => handleOutput(e.Data));
            process.StartInfo.RedirectStandardError = true;  //Handle error manually
            process.StartInfo.RedirectStandardInput = true;  //Handle input manually
            process.StartInfo.EnvironmentVariables["PATH"] += ";" + binPath;  //add C++ compiler path to environmnet variables
            process.Start();  //run the process >> compile
            // Asynchronously read the standard output of the spawned process. 
            // This raises OutputDataReceived events for each line of output.
            process.BeginOutputReadLine();
        }

        private void handleOutput(string output)
        {
            label1.Invoke(new MethodInvoker(delegate { label1.Text = label1.Text + "\n" + output; }));
        }

        //initialize Environment variables
        private void initEnvironmentVariables()
        {
            process.StandardInput.WriteLine("\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"");
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!process.HasExited)
            {
                Console.WriteLine("process is still running. Kill it!");
                process.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog opd = new OpenFileDialog();
            DialogResult result = opd.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                string file = opd.FileName;
                textBox1.Text = file;
                compileCPP(file);
            }
        }

        private void compileCPP(string cppPath)
        {
            process.StandardInput.WriteLine("cl /EHsc \"" + cppPath + "\" -o \"" + new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe\""); // add -o >> redirect output file
        }

    }
}