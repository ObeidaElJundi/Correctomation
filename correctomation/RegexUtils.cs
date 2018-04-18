using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace correctomation
{
    public class RegexUtils
    {

        private static int locate(string code, string pattern, bool start)
        {
            int markerIndex = -1;
            Match m = Regex.Match(code, pattern);
            if (m.Success)
            {
                if (start) markerIndex = m.Index;
                else markerIndex = m.Index + m.Length;
            }
            return markerIndex;
        }

        public static int locateMainContentStart(string code)
        {
            //return locate(code, @"main\s*\(\s*\)(.|\n)*\{", false);
            return locate(code, @"main\s*\(\s*\)[^\{]*\{", false);
        }

        public static int locateMainContentEnd(string code)
        {
            return locate(code, @"return 0;\n*\s*\S*}", true);
        }

        public static Match return0InCpp(string code)
        {
            return Regex.Match(code, @"return 0;(.|\n)*}");
        }

        //locate custom code marker and return its index
        public static int locateCustomCodeMarker(string code, string marker)
        {
            int markerIndex = -1;
            //Match m = Regex.Match(code, marker); // if marker is followed by extra text (ex: //////), this won't work. search untill \n
            Match m = Regex.Match(code, marker + ".*\n");
            if (m.Success)
            {
                Console.WriteLine("index: " + m.Index + "\nLength: " + m.Length);
                markerIndex = m.Index + m.Length;
            }
            return markerIndex;
        }

        public static bool cppCompilationOutput(string compilationOutput)
        {
            Match m = Regex.Match(compilationOutput, "/out:.*output\\.exe");
            return m.Success;
        }

        public static string getExeOutput(string runExeOutput)
        {
            Match m = Regex.Match(runExeOutput, ">>>.*<<<");
            if (m.Success)
            {
                string exeOutput = m.Value;
                exeOutput = exeOutput.Substring(3, exeOutput.Length - 6);
                return exeOutput;
            }
            else
            {
                return string.Empty;
            }
        }

        public static int getExecutionTime(string runExeOutput)
        {
            Match m = Regex.Match(runExeOutput, @":::(\d+):::");
            if (m.Success)
            {
                int executionTime = Int32.Parse(m.Groups[1].Value);
                return executionTime;
            }
            else
            {
                return -1;
            }
        }

        public static string replaceInputOrOutputFilePath(string code, string filePath, string pattern){
            return Regex.Replace(code, pattern, delegate(Match m){
                //System.Diagnostics.Debug.WriteLine("\n\nm.Groups[1].Value = " + m.Groups[1].Value + "\n\n");
                //System.Diagnostics.Debug.WriteLine("\n\nm.Groups[2].Value = " + m.Groups[2].Value + "\n\n");
                //System.Diagnostics.Debug.WriteLine("\n\nm.Groups[3].Value = " + m.Groups[3].Value + "\n\n");
                //string replacement = code.Replace(m.Groups[1].Value, "\"" + filePath + "\"");
                string replacement = m.Groups[1].Value + "\"" + filePath + "\"" + m.Groups[3].Value;
                return replacement;
            });
        }

        public static string replaceInputFilePath(string code, string inputFilePath)
        {
            //string pattern = @"ifstream(.|\n)+\((.+)\)";
            string pattern = @"(ifstream[^\(]+\()([^\(]+)(\))";
            return replaceInputOrOutputFilePath(code, inputFilePath, pattern);
        }

        public static string replaceOutputFilePath(string code, string outputFilePath)
        {
            //string pattern = @"ofstream(.|\n)+\((.+)\)";
            string pattern = @"(ofstream[^\(]+\()([^\(]+)(\))";
            return replaceInputOrOutputFilePath(code, outputFilePath, pattern);
        }

        /*public static string replaceFilePath(string code, string filePath, bool input)
        {
            string ch = (input) ? "i" : "o";
            string pattern1 = @"(" + ch + @"fstream\s+\w+\s*\()([^\)]+)\)";
            //Match match = Regex.Match(code, pattern1);
            //if (match.Success)
            if (Regex.IsMatch(code, pattern1))
            {
                return Regex.Replace(code, pattern1, delegate(Match m)
                {
                    string replacement = m.Groups[1].Value + "\"" + filePath + "\")";
                    return replacement;
                });
            }
            else
            {
                string pattern2 = ch + @"fstream\s+(\w+)\(([^\)]+)\)";
            }
        }*/


        public static string replaceFilePath(string code, string filePath, bool input)
        {
            string ch = (input) ? "i" : "o";
            string pattern1 = ch + @"fstream\s+(\w+)"; // ex: ifstream input
            if (Regex.IsMatch(code, pattern1))
            {
                Console.WriteLine("pattern1 match :)");
                string pattern2 = pattern1 + @"\s*\(([^\)]+)\)"; // ex: ifstream input ("file.txt")
                //string pattern3 = pattern1 + @"(.*\1\.open)\(([^\)]+)\)";
                string pattern3 = pattern1 + @"([^\1]+\1\.open\s*)\(([^\)]+)\)";
                Match match2 = Regex.Match(code, pattern2);
                Match match3 = Regex.Match(code, pattern3);
                if (match2.Success)
                {
                    Console.WriteLine("pattern2 match :)");
                    string replacement = ch + "fstream " + match2.Groups[1].Value + "(\"" + filePath + "\")";
                    return Regex.Replace(code, pattern2, replacement);
                }
                else if (match3.Success)
                {
                    Console.WriteLine("patten3 match :)");
                    string replacement = ch + "fstream " + match3.Groups[1].Value + match3.Groups[2].Value + "(\"" + filePath + "\")";
                    return Regex.Replace(code, pattern3, replacement);
                }
                else return code;
            }
            else return code;
        }

    }
}
