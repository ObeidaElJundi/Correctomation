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

        public static Match return0InCpp(string code)
        {
            return Regex.Match(code, @"return 0;\n*\s*\S*}");
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

    }
}
