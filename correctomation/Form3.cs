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
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
                pictureBox1.Visible = true;
                startInParallel(textBox_cpps_dir.Text, textBox_cpp_file_name.Text);
            }
        }

        private bool checkInputs()
        {
            if (string.IsNullOrEmpty(textBox_properties.Text))
            {
                MessageBox.Show("Please enter some properties");
                return false;
            }
            if (textBox_properties.Text.Contains(' '))
            {
                MessageBox.Show("No spaces are allowed in properties!");
                return false;
            }
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
            return true;
        }

        private void startInParallel(string path, string cppFile)
        {
            string[] dirs = Directory.GetDirectories(path);
            int counter = dirs.Length;
            Parallel.ForEach(dirs, dir => {

                //get student number, which is the last chunk of the path
                string[] n = dir.Split(Path.DirectorySeparatorChar);
                string studentNumber = n[n.Length - 1];

                //get student name: the txt file name (assuming there is one txt file only)
                //clean txt file name: remove extention and replace underscore by space
                string[] s = getSpecificFileRecursively(dir, "*.txt").Split(Path.DirectorySeparatorChar);
                string studentName = s[s.Length - 1].Replace(".txt", "").Replace("_", " ");

                //get the path of the cpp file to be compiled
                string cppFilePath = getSpecificFileRecursively(dir, cppFile);

                if (string.IsNullOrEmpty(cppFilePath))
                {
                    updateResult(studentName, -3);
                }
                else if (appendProperties(cppFilePath))
                {
                    string tmpCpp = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "temp.cpp";
                    if (compile(tmpCpp))
                    {
                        string exePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
                        int grade = runExeWithTestCases(exePath);
                        updateResult(studentName, grade);
                    }
                    else
                    {
                        updateResult(studentName, -2);
                    }
                }
                else
                {
                    updateResult(studentName, -1);
                }

                checkIfDone(--counter,path,cppFile);

            });
        }

        //read properties entered by user and append them to cpp file
        private bool appendProperties(string cppPath)
        {
            //read props - append cout<<pros to cpp file - compile cpp - read in.txt - iterate through in.txt - init & run new process that runs exe - feed input for exe - append to output - check output with out.txt
            string[] props = textBox_properties.Text.Split(';'); //get properties to be appended to cpp file
            string toBeAppended = "\n\n\t";
            string tmpCpp = new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + "temp.cpp";
            if (props.Length > 0)
            { //construct the cout statement: properties separated by spaces
                toBeAppended += "cout<<" + "\">>>\"<<" + props[0];
                for (int i = 1; i < props.Length; i++)
                    toBeAppended += "<<\" \"<<" + props[i];
                toBeAppended += "<<\"<<<\"" + ";\n\n\t";
            }
            //read cpp code to locate 'return 0;' to append properties before it
            string code = File.ReadAllText(cppPath);
            Match m = RegexUtils.return0InCpp(code);
            if (m.Success)
            {
                code = code.Insert(m.Index, toBeAppended);
                File.WriteAllText(tmpCpp, code);
                return true;
            }
            else
            {
                //MessageBox.Show("could not append! could not locate return!");
            }
            return false;
        }

        // compiles cpp file executing 'cl' command through cmd process
        // return true if cpp is compiled successfully, false otherwise
        private bool compile(string cppPath)
        {
            Process p = Utils.getProcess(binPath);  //get nre process
            p.Start();  //run the process
            p.StandardInput.WriteLine("\"" + binPath + Path.DirectorySeparatorChar + "vcvars32.bat" + "\"");  //set environment variables
            string exePath = new FileInfo(cppPath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
            p.StandardInput.WriteLine("cl /EHsc \"" + cppPath + "\" -o \"" + exePath + "\"");  // add -o >> redirect output file
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
        //TODO: stuck at reading output! dummy solution: sleep...(search)
        private bool runExe_checkOutput(string exePath, string input, string expectedOutput)
        {
            Process runExeProcess = Utils.getProcess(binPath);
            runExeProcess.Start();
            runExeProcess.StandardInput.WriteLine("\"" + exePath + "\"");
            Thread.Sleep(200);
            string[] inputs = input.Split(' ');
            for (int i = 0; i < inputs.Length; i++)
            {
                runExeProcess.StandardInput.WriteLine(inputs[i]);
                Thread.Sleep(200);
            }
            //Thread.Sleep(500);
            runExeProcess.StandardInput.WriteLine("exit");
            Thread.Sleep(200);
            runExeProcess.WaitForExit();
            string output = runExeProcess.StandardOutput.ReadToEnd(); // <<<< stuck here if there is no Thread.Sleep !!!
            //runExeProcess.WaitForExit();
            string exeOutput = RegexUtils.getExeOutput(output);
            return exeOutput.Equals(expectedOutput);
        }

        private void checkIfDone(int c, string path, string cppName)
        {
            Console.WriteLine("checkIfDone >> c = " + c);
            if (c == 0) // we are done...
            {
                //pictureBox1.Visible = false;
                File.WriteAllText(path + Path.DirectorySeparatorChar + "final_results.txt", cppName + " Grades\n\n" + finalResult);
                MessageBox.Show("Results are available @ final_results.txt", "DONE");
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
                    r = "could not append properties! could not locate return 0 in cpp file!";
                    break;
                case -2:
                    r = "Compilation Error!";
                    break;
                case -3:
                    r = "CPP file not found!";
                    break;
                default:
                    r = result.ToString();
                    break;
            }
            Console.WriteLine("updateResult for : " + studentName + " >> result: " + r);
            if (!string.IsNullOrEmpty(finalResult)) finalResult += "\n";
            finalResult += studentName + "\t" + r;
        }

    }
}
