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
using System.Threading;
using System.Text.RegularExpressions;

namespace correctomation
{
    public partial class Form2 : Form
    {

        private string visualStudioInstallationPath = string.Empty;
        private string binPath = string.Empty;
        //private string lastFileName = string.Empty;
        private string lastFilePath = string.Empty;
        private Process process = new Process();
        private bool running = false, compiledSuccessfully = false;
        private string executingOutput = string.Empty;
        //private bool error = false;
        /*private StreamWriter sw;
        private StreamReader sr, err;*/

        public Form2()
        {
            InitializeComponent();

            initPaths();
            //initAndStartProcess();
            //initEnvironmentVariables();
        }

        //initialize installation and bin path
        private void initPaths()
        {
            if (correctomation.Properties.Settings.Default.pathsInitialized == true)
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
            correctomation.Properties.Settings.Default.binPath = binPath;
            correctomation.Properties.Settings.Default.installationPath = visualStudioInstallationPath;
            correctomation.Properties.Settings.Default.pathsInitialized = true;
            correctomation.Properties.Settings.Default.Save();
        }

        //retrieve saved paths
        private void getPaths()
        {
            binPath = correctomation.Properties.Settings.Default.binPath;
            visualStudioInstallationPath = correctomation.Properties.Settings.Default.installationPath;
        }





        private void initAndStartProcess()
        {
            process.StartInfo.FileName = "cmd.exe";  //process is cmd (command prompt)
            //process.StartInfo.Verb = "runas";  //run command, or process, as admin
            process.StartInfo.CreateNoWindow = true;  //do not show command prompt window
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;  //do not show command prompt window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;  //Handle out manually
            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => handleOutput(e.Data));
            //process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => handleError(e.Data));
            //process.StartInfo.RedirectStandardError = true;  //Handle error manually
            process.StartInfo.RedirectStandardInput = true;  //Handle input manually
            process.StartInfo.EnvironmentVariables["PATH"] += ";" + binPath;  //add C++ compiler path to environmnet variables
            process.Start();  //run the process >> compile
            // Asynchronously read the standard output of the spawned process. 
            // This raises OutputDataReceived events for each line of output.
            process.BeginOutputReadLine();
            //process.BeginErrorReadLine();
            //readOutput();
        }

        private void handleOutput(string output)
        {
            label1.Invoke(new MethodInvoker(delegate { label1.Text = label1.Text + "\n" + output; }));

            if (output.Contains("error") && output.Contains(lastFilePath)) { //compilation output >> error
                compiledSuccessfully = false;
                MessageBox.Show(output, "COMPILATION ERROR!");
            }
            if (output.Contains("/out") && output.Contains("output.exe")) {  //compilation output >> success
                compiledSuccessfully = true;
                //System.Threading.Thread.Sleep(333);
                //runWithCustomInput();
                //MessageBox.Show("Compiled successfully :)");
            }
            if (output.Contains(".obj") && compiledSuccessfully) {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Thread.Sleep(555);
                    runWithCustomInput();
                }).Start();
                //runWithCustomInput();
            }
            if (running && !output.Contains(new FileInfo(lastFilePath).Directory.FullName))
            { //running compiled cpp output
                running = false;
                checkOutput(output);
            }
                //executingOutput += "\n" + output;
        }

        private void handleError(string error)
        {
            Console.WriteLine("Error! >> " + error);
            label1.Invoke(new MethodInvoker(delegate { label1.Text = label1.Text + "\nError! >> " + error; }));
        }

        //initialize Environment variables
        private void initEnvironmentVariables()
        {
            process.StandardInput.WriteLine("\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"");
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*if (process != null && !process.HasExited)
            {
                Console.WriteLine("process is still running. Kill it!");
                process.Close();
            }*/
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog opd = new OpenFileDialog();
            DialogResult result = opd.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                running = false;
                string file = opd.FileName;
                fileReady(file);
            }
        }

        private void compileCPP(string cppPath)
        {
            process.StandardInput.WriteLine("cl /EHsc \"" + cppPath + "\" -o \"" + new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe\""); // add -o >> redirect output file
            //readOutput();
        }

        private void runWithCustomInput()
        {
            //System.Threading.Thread.Sleep(333);
            string dir = new FileInfo(lastFilePath).Directory.FullName;
            running = true;
            process.StandardInput.WriteLine("\"" + dir + Path.DirectorySeparatorChar + "output.exe\""); // run the program
            string[] props = textBox2.Text.Split(';');
            string input = File.ReadAllLines(dir + Path.DirectorySeparatorChar + "input.txt")[0];
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("EMPTY input!");
                return;
            }
            else
                process.StandardInput.WriteLine(input);

            /*string[] inputLines = File.ReadAllLines(dir);
            for (int i = 0; i < inputLines.Length;i++)
            {
                string line = inputLines[i];

            }*/
        }

        private void checkOutput(string output)
        {
            string dir = new FileInfo(lastFilePath).Directory.FullName;
            //string expectedOutput = File.ReadAllLines(dir + Path.DirectorySeparatorChar + "input.txt")[0];
           
            string expectedOutput = File.ReadAllLines(dir + Path.DirectorySeparatorChar + "output.txt")[0];
            if (string.IsNullOrEmpty(expectedOutput))
            {
                MessageBox.Show("EMPTY output!");
                return;
            }
            //Console.WriteLine("PATH = " + dir + Path.DirectorySeparatorChar + "output.txt");
            //Console.WriteLine("executingOutput = " + executingOutput);
            if (output.Equals(expectedOutput))
                MessageBox.Show("CORRECT :)");
            else
                MessageBox.Show("Expected output >> " + expectedOutput + "\nActual output >> " + output, "WRONG OUTPUT!");
        }


















        private void Form2_DragEnter(object sender, DragEventArgs e)
        {
            //e.Effect = DragDropEffects.All;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form2_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[]; // get all files droppeds
            string file = files.First();
            fileReady(file);
        }

        private void fileReady(string file)
        {
            if(checkCppExtension(file) && checkProperties())
            {
                textBox1.Text = file;
                label1.Text = string.Empty;
                lastFilePath = file;
                appendPropertiesThenCompile(file);
            }
        }

        private bool checkCppExtension(string file)
        {
            string extension = new FileInfo(file).Extension.ToLower();
            Console.WriteLine("Extension >> " + extension);
            if (!extension.Equals(".cpp"))
            {
                MessageBox.Show("Only CPP files are allowed!");
                return false;
            }
            return true;
        }

        private bool checkProperties()
        {
            string props = textBox2.Text;
            if (string.IsNullOrEmpty(props))
            {
                MessageBox.Show("Please enter some properties");
                return false;
            }
            if (props.Contains(' '))
            {
                MessageBox.Show("No spaces are allowed in properties!");
                return false;
            }
            return true;
        }


        private void appendPropertiesThenCompile(string cppPath)
        {
            //read props - append cout<<pros to cpp file - compile cpp - read in.txt - iterate through in.txt - init & run new process that runs exe - feed input for exe - append to output - check output with out.txt
            string[] props = textBox2.Text.Split(';'); //get properties to be appended to spp file
            string toBeAppended = "\n\n\t";
            string tmpCpp = new FileInfo(lastFilePath).Directory.FullName + Path.DirectorySeparatorChar + "temp.cpp";
            if (props.Length > 0)
            { //construct the cout statement: properties separated by spaces
                toBeAppended += "cout<<" + "\">>>\"<<" + props[0];
                for(int i=1;i<props.Length;i++)
                    toBeAppended += "<<\" \"<<" + props[i];
                toBeAppended += "<<\"<<<\"" + ";\n\n\t";
            }
            Console.WriteLine("toBeAppended: " + toBeAppended);
            string code = File.ReadAllText(cppPath);
            Match m = RegexUtils.return0InCpp(code);
            if (m.Success)
            {
                Console.WriteLine("index = " + m.Index);
                code = code.Insert(m.Index, toBeAppended);
                File.WriteAllText(tmpCpp, code);
                label1.Text += "\nAppending: " + toBeAppended;
                compile(tmpCpp);
            }
            else
            {
                MessageBox.Show("could not append! could not locate return!");
            }

            //TODO: compile then run as many times as in.txt has lines.. each in a seprated process, so I can read each process output
        }

        private void compile(string cppPath)
        {
            label1.Text += "\nCompiling...";
            Process p = getProcess();
            p.Start();  //run the process
            p.StandardInput.WriteLine("\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"");
            string exePath = new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
            p.StandardInput.WriteLine("cl /EHsc \"" + cppPath + "\" -o \"" + exePath + "\""); // add -o >> redirect output file
            p.StandardInput.WriteLine("exit");
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            label1.Text += "\n" + output;
            Console.WriteLine("output >> " + output);
            if (RegexUtils.cppCompilationOutput(output))
            {
                MessageBox.Show("Compiled successfully :)");
                //TODO: MessageBox with 2 buttons: 'run exe' & 'abort'
                label1.Text = string.Empty;
                prepareInputs_runExe(exePath);
            }
            else
            {
                MessageBox.Show(output, "Compilation Failed!");
            }
        }

        // 1. read inputs (test cases) from input.txt
        // 2. read expected outputs from output.txt
        // 3. run exe with custom inputs (test cases)
        // 4. compare exe output with expected output
        private void prepareInputs_runExe(string exePath)
        {
            string exeDirectory = new FileInfo(exePath).Directory.FullName;
            string[] inputs_testCases = File.ReadAllLines(exeDirectory + Path.DirectorySeparatorChar + "input.txt");
            string[] expectedOutputs = File.ReadAllLines(exeDirectory + Path.DirectorySeparatorChar + "output.txt");
            int correct = 0;
            for (int i = 0; i < inputs_testCases.Length; i++)
            {
                Console.WriteLine("running test case: " + (i + 1));
                if (runExe_checkOutput(exePath, inputs_testCases[i], expectedOutputs[i]))
                    correct++;
            }
            Console.WriteLine("DONE running test cases");
            int percent = 0;
            if(correct > 0)
                percent = correct * 100 / inputs_testCases.Length;
            MessageBox.Show(correct + " correct test cases out of " + inputs_testCases.Length + "\n" + percent + "%", "DONE");
        }

        //run the compiled exe and compare the exe output with the expected output
        //returns true if the same, false otherwise
        private bool runExe_checkOutput(string exePath, string input, string expectedOutput)
        {
            Process runExeProcess = getProcess();
            Console.WriteLine("running exe process");
            //Console.WriteLine("exe path : " + exePath);
            try { runExeProcess.Start(); }
            catch (Exception ex) { Console.WriteLine("exception! >> " + ex.Message); }
            //Thread.Sleep(100);
            runExeProcess.StandardInput.WriteLine("\"" + exePath + "\"");
            string[] inputs = input.Split(' ');
            for (int i = 0; i < inputs.Length; i++)
            {
                //Thread.Sleep(100);
                runExeProcess.StandardInput.WriteLine(inputs[i]);
            }
            Thread.Sleep(100);
            runExeProcess.StandardInput.WriteLine("exit");
            //Thread.Sleep(100);
            string output = runExeProcess.StandardOutput.ReadToEnd(); // <<<< stuck here if there is no Thread.Sleep !!!
            Console.WriteLine("running exe process exiting");
            runExeProcess.WaitForExit();
            Console.WriteLine("running exe process exited");
            //label1.Text += "\n" + output;
            string exeOutput = RegexUtils.getExeOutput(output);
            Console.WriteLine("exeOutput >> " + exeOutput);
            label1.Text += "\n" + "Program output >> " + exeOutput + "  --  " + "Expected output >> " + expectedOutput;
            if (string.IsNullOrEmpty(exeOutput))
                return false;
            string[] expectedOutputs = expectedOutput.Split(' ');
            string[] exeOutputs = exeOutput.Split(' ');
            if (exeOutputs.Length != expectedOutputs.Length)
                return false;
            for (int i = 0; i < expectedOutputs.Length; i++)
            {
                if (!exeOutputs[i].Equals(expectedOutputs[i]))
                    return false;
            }
            return true;
        }

        /*private void readOutput()
        {
            string output = string.Empty;
            while ((output = process.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine("Output >> " + output);
                label1.Text = label1.Text + "\n" + output;
                if (output.Contains("error")) Console.WriteLine("ERROR!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            /*while (process.StandardOutput.Peek() > -1)
            {
                output = process.StandardOutput.ReadLine();
                Console.WriteLine("Output >> " + output);
                label1.Text = label1.Text + "\n" + output;
                if (output.Contains("error")) Console.WriteLine("ERROR!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }*/
        //}



        private Process getProcess()
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