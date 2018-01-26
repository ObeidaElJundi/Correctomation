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
            Match m = Regex.Match(code, marker);
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

    }
}
