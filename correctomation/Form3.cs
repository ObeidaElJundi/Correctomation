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
using SharpCompress.Archives;
using SharpCompress.Readers;
using System.Collections.Generic;

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

        private void Form3_Load(object sender, EventArgs e)
        {
            if (correctomation.Properties.Settings.Default.userInputsSaved)
                getLatestUserInputs();
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
        }

        private void button_in_out_Click(object sender, EventArgs e)
        {

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
                savePaths(textBox_cpps_dir.Text, textBox_input.Text, checkBox_inputIsFile.Checked, textBox_output.Text, checkBox_outputIsFile.Checked,
                    textBox_cpp_file_name.Text, checkBox_customize.Checked, textBox_customize_marker.Text, textBox_customize_code.Text);
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
            if (string.IsNullOrEmpty(textBox_input.Text))
            {
                MessageBox.Show("Please enter input directory");
                return false;
            }
            if (string.IsNullOrEmpty(textBox_output.Text))
            {
                MessageBox.Show("Please enter output directory");
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
                string archivePath = getSpecificFileRecursively(dir, "*.zip");
                if (string.IsNullOrEmpty(archivePath)) archivePath = getSpecificFileRecursively(dir, "*.rar");
                if (string.IsNullOrEmpty(archivePath))
                {
                    updateResult(studentName, -5);
                }
                else
                {
                    if (!extract(archivePath, new FileInfo(archivePath).Directory.FullName))
                    { // could not extract archive!
                        updateResult(studentName, -7);
                    }
                    else
                    {
                        //get the path of the cpp file
                        string cppFilePath = getSpecificFileRecursively(dir, cppFileName);
                        if (string.IsNullOrEmpty(cppFilePath))
                        { //cpp not found!
                            updateResult(studentName, -3);
                        }
                        else
                        {
                            //read code from cpp file and append timer & custom code if necessary
                            string code = File.ReadAllText(cppFilePath);

                            if (checkBox_timer.Checked)
                            { // timer code should be added
                                if (!appendTimerCode(ref code))
                                { //error appending timer code!
                                    updateResult(studentName, -6);
                                    checkIfDone(--counter, path, cppFileName);
                                    return;
                                }
                            }

                            if (checkBox_customize.Checked)
                            { // custom code should be added
                                if (!appendCustomCorrectionCode(cppFilePath, ref code, textBox_customize_code.Text, textBox_customize_marker.Text))
                                { //error appending custom code!
                                    updateResult(studentName, -4);
                                    checkIfDone(--counter, path, cppFileName);
                                    return;
                                }
                            }

                            //List<string> EXEsToRun = new List<string>();
                            if (checkBox_inputIsFile.Checked)
                            {
                                //EXEsToRun = inputIsFile_compileCPP_getExePath(cppFilePath, code, cppFileName, studentName, textBox_input.Text, textBox_output.Text);
                                if (checkBox_outputIsFile.Checked) inputIsFile_and_outputIsFile(cppFilePath, code, cppFileName, studentName, textBox_input.Text, textBox_output.Text);
                                else inputIsFile_and_outputIsNotFile(cppFilePath, code, cppFileName, studentName, textBox_input.Text, textBox_output.Text);
                            }
                            else
                            {
                                //EXEsToRun = inputIsNotFile_compileCPP_getExePath(cppFilePath, code, cppFileName, studentName);
                                if (checkBox_outputIsFile.Checked) inputIsNotFile_and_outputIsFile(cppFilePath, code, cppFileName, studentName, textBox_input.Text, textBox_output.Text);
                                else inputIsNotFile_and_outputIsNotFile(cppFilePath, code, cppFileName, studentName, textBox_input.Text, textBox_output.Text);
                            }

                            /*if (checkBox_outputIsFile.Checked)
                            {
                                
                            }
                            else
                            {
                                outputIsNotFile(EXEsToRun, studentName);
                            }*/

                            /*//create a temp cpp file to be compiled
                            string tempCppFilePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + cppFileName + "_temp.cpp";
                            File.WriteAllText(tempCppFilePath, code);
                            if (compile(tempCppFilePath, studentName))
                            { //compiled successfully. run output exe with test cases and get grade
                                string exePath = new FileInfo(tempCppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
                                Dictionary<string, object> results = runExeWithTestCases(exePath);
                                int grade = (int)results["percent"];
                                string testCases = (string)results["test cases"];
                                int executionTime = (int)results["average execution time"];
                                updateResult(studentName, grade, testCases, executionTime);
                            }
                            else
                            {
                                updateResult(studentName, -2);
                            }*/
                        }
                    }
                }

                checkIfDone(--counter,path,cppFileName);

            });
        }

        private void inputIsFile_and_outputIsFile(string cppFilePath, string code, string problemName, string studentName, string inputTxtPath, string outputTxtPath)
        {
            problemName = problemName.Replace(".cpp", "").Replace(".CPP", "");
            string[] inputsFilesPaths = File.ReadAllLines(inputTxtPath);
            string[] outputsFilesPaths = File.ReadAllLines(outputTxtPath);

            int correct = 0;
            string testCasesResult = string.Empty;

            string txtOutputPath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.txt";
            txtOutputPath = txtOutputPath.Replace("\\", "\\\\");

            for (int i = 0; i < inputsFilesPaths.Length; i++)
            {
                string newCode = RegexUtils.replaceInputFilePath(code, inputsFilesPaths[i]);
                newCode = RegexUtils.replaceOutputFilePath(newCode, txtOutputPath);
                //create a temp cpp file to be compiled
                string tempCppFilePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + problemName + "_temp.cpp";
                File.WriteAllText(tempCppFilePath, newCode);
                if (compile(tempCppFilePath, studentName))
                {
                    string exePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
                    runExe(exePath, "");
                    string expectedOutput = File.ReadAllText(outputsFilesPaths[i]);
                    string output = File.ReadAllText(txtOutputPath);
                    if (expectedOutput.Equals(output)) {
                        correct++;
                        testCasesResult += "1";
                    } else
                        testCasesResult += "0";
                }
                else
                {
                    updateResult(studentName, -2);
                    return;
                }
            }

            int percent = 0;
            if (correct > 0) percent = correct * 100 / inputsFilesPaths.Length;
            updateResult(studentName, percent, testCasesResult, 0);
        }


        private void inputIsFile_and_outputIsNotFile(string cppFilePath, string code, string problemName, string studentName, string inputTxtPath, string outputTxtPath)
        {
            problemName = problemName.Replace(".cpp", "").Replace(".CPP", "");
            string[] inputsFilesPaths = File.ReadAllLines(inputTxtPath);
            string[] expectedOutputs = File.ReadAllLines(outputTxtPath);

            int correct = 0;
            string testCasesResult = string.Empty;

            for (int i = 0; i < inputsFilesPaths.Length; i++)
            {
                string newCode = RegexUtils.replaceInputFilePath(code, inputsFilesPaths[i]);
                //Console.WriteLine("********************************\niteration : " + i + "\n" + newCode);
                //create a temp cpp file to be compiled
                string tempCppFilePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + problemName + "_temp.cpp";
                File.WriteAllText(tempCppFilePath, newCode);
                if (compile(tempCppFilePath, studentName))
                {
                    string exePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
                    string output = runExe(exePath, "");
                    string solution = RegexUtils.getExeOutput(output);
                    if (expectedOutputs[i].Equals(solution))
                    {
                        correct++;
                        testCasesResult += "1";
                    }
                    else
                        testCasesResult += "0";
                }
                else
                {
                    updateResult(studentName, -2);
                    return;
                }
            }

            int percent = 0;
            if (correct > 0) percent = correct * 100 / inputsFilesPaths.Length;
            updateResult(studentName, percent, testCasesResult, 0);
        }


        private void inputIsNotFile_and_outputIsFile(string cppFilePath, string code, string problemName, string studentName, string inputTxtPath, string outputTxtPath)
        {
            problemName = problemName.Replace(".cpp", "").Replace(".CPP", "");
            string[] inputs = File.ReadAllLines(inputTxtPath);
            string[] outputsFilesPaths = File.ReadAllLines(outputTxtPath);

            int correct = 0;
            string testCasesResult = string.Empty;

            string tempCppFilePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + problemName + "_temp.cpp";
            string txtOutputPath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.txt";
            txtOutputPath = txtOutputPath.Replace("\\", "\\\\");
            string newCode = RegexUtils.replaceOutputFilePath(code, txtOutputPath);
            File.WriteAllText(tempCppFilePath, newCode);
            if (!compile(tempCppFilePath, studentName))
            {
                updateResult(studentName, -2);
                return;
            }

            string exePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
            for (int i = 0; i < inputs.Length; i++)
            {
                runExe(exePath, inputs[i]);
                string expectedOutput = File.ReadAllText(outputsFilesPaths[i]);
                string output = File.ReadAllText(txtOutputPath);
                if (expectedOutput.Equals(output))
                {
                    correct++;
                    testCasesResult += "1";
                }
                else
                    testCasesResult += "0";
            }

            int percent = 0;
            if (correct > 0) percent = correct * 100 / inputs.Length;
            updateResult(studentName, percent, testCasesResult, 0);
        }


        private void inputIsNotFile_and_outputIsNotFile(string cppFilePath, string code, string problemName, string studentName, string inputTxtPath, string outputTxtPath)
        {
            
            problemName = problemName.Replace(".cpp", "").Replace(".CPP", "");
            string[] inputs = File.ReadAllLines(inputTxtPath);
            string[] expectedOutputs = File.ReadAllLines(outputTxtPath);

            int correct = 0;
            string testCasesResult = string.Empty;

            string tempCppFilePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + problemName + "_temp.cpp";
            File.WriteAllText(tempCppFilePath, code);
            if (!compile(tempCppFilePath, studentName))
            {
                updateResult(studentName, -2);
                return;
            }

            string exePath = new FileInfo(cppFilePath).Directory.FullName + Path.DirectorySeparatorChar + "output.exe";
            for (int i = 0; i < inputs.Length; i++)
            {

                string exeOutput = runExe(exePath, inputs[i]);
                string solution = RegexUtils.getExeOutput(exeOutput);
                if (expectedOutputs[i].Equals(solution))
                {
                    correct++;
                    testCasesResult += "1";
                }
                else
                    testCasesResult += "0";
            }

            int percent = 0;
            if (correct > 0) percent = correct * 100 / inputs.Length;
            updateResult(studentName, percent, testCasesResult, 0);
        }





        //append timer code to cpp file to measure execution time
        private bool appendTimerCode(ref string originalCode)
        {
            //add the include statement at the beginning
            originalCode = originalCode.Insert(0, "#include <ctime>\n");
            //locate the beginning of main function content. Then append timer code
            int mainContentStartIndex = RegexUtils.locateMainContentStart(originalCode);
            if (mainContentStartIndex == -1) return false;
            originalCode = originalCode.Insert(mainContentStartIndex, "\n\tclock_t begin = clock();\n");
            //locate the end of main function content. Then append timer code
            int mainContentEndIndex = RegexUtils.locateMainContentEnd(originalCode);
            if (mainContentEndIndex == -1) return false;
            string timerEndCode = "clock_t end = clock();\n\tdouble elapsed = double(end-begin) / (CLOCKS_PER_SEC/1000000.0);\n\tcout<<\":::\"<<elapsed<<\":::\"<<endl;\n\t";
            originalCode = originalCode.Insert(mainContentEndIndex, timerEndCode);
            return true;
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
        // returns grade percentage, successful test cases, and execution time as dictionary
        private Dictionary<string, object> runExeWithTestCases(string exePath)
        {
            string exeDirectory = new FileInfo(exePath).Directory.FullName;
            string[] inputs_testCases = File.ReadAllLines(textBox_input.Text + Path.DirectorySeparatorChar + "input.txt");
            string[] expectedOutputs = File.ReadAllLines(textBox_input.Text + Path.DirectorySeparatorChar + "output.txt");
            string testCasesResult = string.Empty;
            int correct = 0;
            List<int> executionTimes = new List<int>();
            for (int i = 0; i < inputs_testCases.Length; i++)
            {
                string processOutput = runExe(exePath, inputs_testCases[i]);
                Console.WriteLine("run Exe output = " + processOutput);
                if(checkBox_timer.Checked) {
                    executionTimes.Add(RegexUtils.getExecutionTime(processOutput));
                    Console.WriteLine("execution time = " + RegexUtils.getExecutionTime(processOutput));
                }
                string solution = RegexUtils.getExeOutput(processOutput);
                if (solution.Equals(expectedOutputs[i]))
                {
                    correct++;
                    testCasesResult += "1";
                }
                else
                {
                    testCasesResult += "0";
                }
            }
            int percent = 0;
            if (correct > 0)
                percent = correct * 100 / inputs_testCases.Length;
            Dictionary<string, object> results = new Dictionary<string, object>();
            results.Add("percent", percent);
            results.Add("test cases", testCasesResult);
            int avg = 0;
            if(checkBox_timer.Checked) avg = (int)executionTimes.Average();
            results.Add("average execution time", avg);
            return results;
        }

        //run the compiled exe and compare the exe output with the expected output
        //returns process output as string
        private string runExe(string exePath, string input)
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
            return output;
            //string exeOutput = RegexUtils.getExeOutput(output);
            //return exeOutput.Equals(expectedOutput);
        }

        private void runExe(string exePath)
        {
            string s = runExe(exePath, string.Empty);
        }

        private void checkIfDone(int c, string path, string cppName)
        {
            Console.WriteLine("checkIfDone >> c = " + c);
            if (c == 0) // we are done...
            {
                writeFinalResultsToFile(path, cppName);
            }
        }

        private void writeFinalResultsToFile(string path, string cppName)
        {
            try
            {
                //pictureBox1.Visible = false;
                string results = "# " + cppName + " Grades\n" + "Student Name,Grade (over 100),Tese Cases";
                if (checkBox_timer.Checked) results += ",Execution Time (microSeconds)";
                results += "\n" + finalResult;
                string resultsPath = path + Path.DirectorySeparatorChar + "final_results.csv";
                File.WriteAllText(resultsPath, results);
                //MessageBox.Show("Results are available @ final_results.csv", "DONE");
                new CustomDialog(resultsPath).ShowDialog();
            }
            catch (Exception e)
            {
                DialogResult d = MessageBox.Show("Could not write results to final_results.csv\nPlease close it and try again...", "ERROR!", MessageBoxButtons.RetryCancel);
                if (d == DialogResult.Retry)
                {
                    writeFinalResultsToFile(path, cppName);
                }
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
                case -6:
                    r = "Could not append timer code!";
                    break;
                case -7:
                    r = "Could not extract archive!!";
                    break;
                default:
                    r = result.ToString();
                    break;
            }
            Console.WriteLine("updateResult for : " + studentName + " >> result: " + r);
            if (!string.IsNullOrEmpty(finalResult)) finalResult += "\n";
            finalResult += studentName + "," + r;
        }

        private void updateResult(string studentName, int result, string testCases, int executionTime)
        {
            if (!string.IsNullOrEmpty(finalResult)) finalResult += "\n";
            finalResult += studentName + "," + result + "," + testCases;
            if (checkBox_timer.Checked) finalResult += "," + executionTime;
        }

        private void checkBox_customize_CheckedChanged(object sender, EventArgs e)
        {
            textBox_customize_marker.Visible = checkBox_customize.Checked;
            textBox_customize_code.Visible = checkBox_customize.Checked;
        }

        private bool extract(string archivedPath, string destinationPath)
        {
            try
            {
                var archive = ArchiveFactory.Open(archivedPath);
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(destinationPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("extraction exception! " + ex.Message);
                return false;
            }
        }

        //save user inputs, so they can be retrieved next time the app is opened
        private void savePaths(string cppsDirectort, string inputDirectory, bool inputIsFile, string outputDirectory, bool outputIsFile, string cppFileName, bool customize, string customizeMarker, string customizeCode)
        {
            correctomation.Properties.Settings.Default.cppsDirectory = cppsDirectort;
            correctomation.Properties.Settings.Default.inputDirectory = inputDirectory;
            correctomation.Properties.Settings.Default.inputIsFile = inputIsFile;
            correctomation.Properties.Settings.Default.outputDirectory = outputDirectory;
            correctomation.Properties.Settings.Default.outputIsFile = outputIsFile;
            correctomation.Properties.Settings.Default.cppFileName = cppFileName;
            correctomation.Properties.Settings.Default.customize = customize;
            if (customize)
            {
                correctomation.Properties.Settings.Default.customizeMarker = customizeMarker;
                correctomation.Properties.Settings.Default.customizeCode = customizeCode;
            }
            correctomation.Properties.Settings.Default.userInputsSaved = true;
            correctomation.Properties.Settings.Default.Save();
        }

        //retrieve latest user inputs
        private void getLatestUserInputs()
        {
            textBox_cpps_dir.Text = correctomation.Properties.Settings.Default.cppsDirectory;
            textBox_input.Text = correctomation.Properties.Settings.Default.inputDirectory;
            checkBox_inputIsFile.Checked = correctomation.Properties.Settings.Default.inputIsFile;
            textBox_output.Text = correctomation.Properties.Settings.Default.outputDirectory;
            checkBox_outputIsFile.Checked = correctomation.Properties.Settings.Default.outputIsFile;
            textBox_cpp_file_name.Text = correctomation.Properties.Settings.Default.cppFileName;
            if (correctomation.Properties.Settings.Default.customize)
            {
                checkBox_customize.Checked = true;
                textBox_customize_marker.Text = correctomation.Properties.Settings.Default.customizeMarker;
                textBox_customize_code.Text = correctomation.Properties.Settings.Default.customizeCode;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            extract("C:\\Users\\User\\Desktop\\New folder\\New folder\\bla2.rar", "C:\\Users\\User\\Desktop\\New folder\\New folder");
        }

        private void button_input_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select input txt file";
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                textBox_input.Text = openFileDialog.FileName;
            }
        }

        private void button_output_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select output txt file";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox_output.Text = openFileDialog.FileName;
            }
        }

    }
}
