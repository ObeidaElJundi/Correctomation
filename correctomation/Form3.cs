﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace correctomation
{
    public partial class Form3 : Form
    {

        private string finalResult, binPath;

        public Form3()
        {
            InitializeComponent();
            binPath = Utils.getVisualStudioBinPathFromRegistryKey();
            Console.WriteLine("start");
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
            //Console.WriteLine(" >>> ResizeEnd");
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
            if (checkInputs())
            {
                finalResult = string.Empty;
                //pictureBox1.Visible = true;
                startInParallel(textBox_cpps_dir.Text, textBox_cpp_file_name.Text);
            }
        }

        private bool checkInputs()
        {
            if (string.IsNullOrEmpty(textBox_cpp_file_name.Text))
            {
                MessageBox.Show("Please enter CPP file name");
                return false;
            }
            if (string.IsNullOrEmpty(textBox_in_out.Text))
            {
                MessageBox.Show("Please enter input/output directory");
                return false;
            }
            if (string.IsNullOrEmpty(textBox_cpps_dir.Text))
            {
                MessageBox.Show("Please enter CPPs directory");
                return false;
            }
            if (checkBox_customize.Checked)
            {
                if (string.IsNullOrEmpty(textBox_customize_marker.Text) || string.IsNullOrEmpty(textBox_customize_code.Text))
                {
                    MessageBox.Show("Please enter custom code and its marker");
                    return false;
                }
            }
            return true;
        }

        private void startInParallel(string path, string cppFileName)
        {
            string[] dirs = Directory.GetDirectories(path);
            int counter = dirs.Length;
            Parallel.ForEach(dirs, dir => {

                //get student number, which is the last chunk of the path
                //string[] n = dir.Split(Path.DirectorySeparatorChar);
                //string studentNumber = n[n.Length - 1];

                //get student name from the directory name: split on underscores and take the first string
                string[] n = dir.Split(Path.DirectorySeparatorChar);
                string studentName = n[n.Length - 1].Split('_')[0];
                
                //get archive file (rar. if no rar, zip) path then extract it
                string archivePath = getSpecificFileRecursively(dir, "*.rar");
                if (string.IsNullOrEmpty(archivePath)) archivePath = getSpecificFileRecursively(dir, "*.zip");
                if (string.IsNullOrEmpty(archivePath))
                {
                    updateResult(studentName, -5);
                }
                else
                {
                    extract(archivePath, new FileInfo(archivePath).Directory.FullName);

                    //get the path of the cpp file
                    string cppFilePath = getSpecificFileRecursively(dir, cppFileName);
                    if (string.IsNullOrEmpty(cppFilePath))
                    { //cpp not found!
                        updateResult(studentName, -3);
                    }
                    else
                    {
                        //read code from cpp file and append custom code if necessary
                        string code = File.ReadAllText(cppFilePath);
                        if (checkBox_customize.Checked)
                        {
                            if (!appendCustomCorrectionCode(cppFilePath, ref code, textBox_customize_code.Text, textBox_customize_marker.Text))
                            { //error appending custom code!
                                updateResult(studentName, -4);
                                checkIfDone(--counter, path, cppFileName);
                                return;
                            }
                        }
                        //create a temp cpp file to be compiled
                        string tempCppFilePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + cppFileName + "_temp.cpp";
                        File.WriteAllText(tempCppFilePath, code);
                        if (compile(tempCppFilePath, studentName))
                        { //compiled successfully. run output exe with test cases and get grade
                            string exePath = new FileInfo(tempCppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
                            int grade = runExeWithTestCases(exePath);
                            updateResult(studentName, grade);
                        }
                        else
                        {
                            updateResult(studentName, -2);
                        }
                    }
                }

                checkIfDone(--counter,path,cppFileName);

            });
        }

        //read custom correction code entered by user and append it to cpp file at specific marker
        private bool appendCustomCorrectionCode(string cppPath, ref string originalCode, string customCorrectionCode, string marker)
        {
            customCorrectionCode = "\n\t" + customCorrectionCode + "\n";
            //locate marker. Then append custom code after marker
            int markerIndex = RegexUtils.locateCustomCodeMarker(originalCode, marker);
            if (markerIndex != -1)
            {
                originalCode = originalCode.Insert(markerIndex, customCorrectionCode);
                return true;
            }
            return false;
        }

        // compiles cpp file executing 'cl' command through cmd process
        // return true if cpp is compiled successfully, false otherwise
        private bool compile(string cppPath, string studentName)
        {
            Process p = Utils.getProcess(binPath);  //get cmd process
            p.Start();  //run the process
            p.StandardInput.WriteLine("\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"");  //set environment variables
            string exePath = new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
            string objPath = new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + studentName.Replace(' ','_') + ".obj";
            //p.StandardInput.WriteLine("cl /EHsc \"" + cppPath + "\" -o \"" + exePath);  // add -o >> redirect output file
            p.StandardInput.WriteLine("cl /EHsc \"/Fe" + exePath + "\" \"/Fo" + objPath + "\" \"" + cppPath + "\"");  // /Fe: redirect output.exe file, /Fo: redirect obj file
            p.StandardInput.WriteLine("exit");  // close cmd
            string output = p.StandardOutput.ReadToEnd();  // get process output
            Console.WriteLine(cppPath + " >> compilation output: " + output);
            p.WaitForExit();
            return RegexUtils.cppCompilationOutput(output);
        }

        // 1. read inputs (test cases) from input.txt
        // 2. read expected outputs from output.txt
        // 3. run exe with custom inputs (test cases)
        // 4. compare exe output with expected output
        // returns grade percentage
        private int runExeWithTestCases(string exePath)
        {
            string exeDirectory = new FileInfo(exePath).Directory.FullName;
            string[] inputs_testCases = File.ReadAllLines(textBox_in_out.Text + Path.DirectorySeparatorChar + "input.txt");
            string[] expectedOutputs = File.ReadAllLines(textBox_in_out.Text + Path.DirectorySeparatorChar + "output.txt");
            int correct = 0;
            for (int i = 0; i < inputs_testCases.Length; i++)
            {
                if (runExe_checkOutput(exePath, inputs_testCases[i], expectedOutputs[i]))
                    correct++;
            }
            int percent = 0;
            if (correct > 0)
                percent = correct * 100 / inputs_testCases.Length;
            return percent;
        }

        //run the compiled exe and compare the exe output with the expected output
        //returns true if the same, false otherwise
        private bool runExe_checkOutput(string exePath, string input, string expectedOutput)
        {
            Process process = new Process();
            process.StartInfo.FileName = "\"" + exePath + "\"";
            process.StartInfo.CreateNoWindow = true;  //do not show command prompt window
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;  //do not show command prompt window
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;  //Handle output manually
            process.StartInfo.RedirectStandardInput = true;  //Handle input manually
            process.Start();  //run the process
            string[] inputs = input.Split(' ');
            for (int i = 0; i < inputs.Length; i++)
            {
                process.StandardInput.WriteLine(inputs[i]);
            }
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            string exeOutput = RegexUtils.getExeOutput(output);
            return exeOutput.Equals(expectedOutput);
        }

        private void checkIfDone(int c, string path, string cppName)
        {
            Console.WriteLine("checkIfDone >> c = " + c);
            if (c == 0) // we are done...
            {
                //pictureBox1.Visible = false;
                File.WriteAllText(path + Path.DirectorySeparatorChar + "final_results.csv", "# " + cppName + " Grades\n" + "Student Name,Grade (over100)\n"+ finalResult);
                MessageBox.Show("Results are available @ final_results.csv", "DONE");
            }
        }

        //result = -1  >>  could not append properties! could not locate return 0 in cpp file!
        //result = -2  >>  Compilation Error!
        //result = -3  >>  CPP file not found!
        //result >= 0  >>  his/her grade
        private void updateResult(string studentName, int result)
        {
            string r = string.Empty;
            switch (result)
            {
                case -1:
                    r = "Could not append properties! Could not locate return 0 in cpp file!";
                    break;
                case -2:
                    r = "Compilation Error!";
                    break;
                case -3:
                    r = "CPP file not found!";
                    break;
                case -4:
                    r = "Could not locate custom code marker!";
                    break;
                case -5:
                    r = "No archive file!";
                    break;
                default:
                    r = result.ToString();
                    break;
            }
            Console.WriteLine("updateResult for : " + studentName + " >> result: " + r);
            if (!string.IsNullOrEmpty(finalResult)) finalResult += "\n";
            finalResult += studentName + "," + r;
        }

        private void checkBox_customize_CheckedChanged(object sender, EventArgs e)
        {
            textBox_customize_marker.Visible = checkBox_customize.Checked;
            textBox_customize_code.Visible = checkBox_customize.Checked;
        }

        private void extract(string archivedPath, string destinationPath)
        {
            var archive = ArchiveFactory.Open(archivedPath);
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    //Console.WriteLine(entry.Key);
                    entry.WriteToDirectory(destinationPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            extract("C:\\Users\\User\\Desktop\\New folder\\New folder\\bla2.rar", "C:\\Users\\User\\Desktop\\New folder\\New folder");
        }

    }
}
