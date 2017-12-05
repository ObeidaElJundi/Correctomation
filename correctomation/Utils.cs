using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace correctomation
{
    class Utils
    {


        public static string getVisualStudioBinPathFromRegistryKey()
        {
            string visualStudioRegistryKeyPath = @"SOFTWARE\Microsoft\VisualStudio";
            List<Version> vsVersions = new List<Version>() { new Version("17.0"), new Version("16.0"), new Version("15.0"), new Version("14.0"), new Version("13.0"), new Version("12.0"), new Version("11.0"), new Version("10.0"), new Version("9.0"), new Version("8.0") };
            foreach (var version in vsVersions)
            {
                RegistryKey registryBase32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey vsVersionRegistryKey = registryBase32.OpenSubKey(string.Format(@"{0}\{1}.{2}", visualStudioRegistryKeyPath, version.Major, version.Minor));
                if (vsVersionRegistryKey != null)
                {
                    string installationPath = vsVersionRegistryKey.GetValue("InstallDir", string.Empty).ToString();
                    if (!string.IsNullOrEmpty(installationPath))
                    {
                        return new DirectoryInfo(installationPath).Parent.Parent.FullName + Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar + "bin";
                    }
                }
            }
            return string.Empty;
        }



        public static Process getProcess(string binPath)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";  //process is cmd (command prompt)
            process.StartInfo.CreateNoWindow = true;  //do not show command prompt window
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;  //do not show command prompt window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;  //Handle out manually
            process.StartInfo.RedirectStandardInput = true;  //Handle input manually
            process.StartInfo.EnvironmentVariables["PATH"] += ";" + binPath;  //add C++ compiler path to environmnet variables
            //process.Start();  //run the process
            return process;
        }

    }
}
