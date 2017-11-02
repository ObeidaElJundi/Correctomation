using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace correctomation
{
    public class RegexUtils
    {

        public static Match return0InCpp(string code)
        {
            return Regex.Match(code, @"return 0;\n*\s*\S*}");
        }

        public static bool cppCompilationOutput(string compilationOutput)
        {
            Match m = Regex.Match(compilationOutput, "\"/out:.*output\\.exe\"");
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
