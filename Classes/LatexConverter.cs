using System.Text.RegularExpressions;

namespace NewtonV2.Classes
{
    internal class LatexConverter
    {
        public static string LatexToPlain(string latex)
        {
            string s = latex;

            // Remove spaces
            s = s.Replace(" ", "");

            // Basic replacements
            s = s.Replace(@"\left", "").Replace(@"\right", "");
            s = s.Replace(@"\cdot", "*");

            s = s.Replace(@"\sin", "sin")
                 .Replace(@"\cos", "cos")
                 .Replace(@"\tan", "tan")
                 .Replace(@"\log", "log")
                 .Replace(@"\exp", "exp");

            s = s.Replace(@"\pi", "pi")
                 .Replace(@"\mathrm{i}", "i")
                 .Replace(@"\imath", "i");

            // Handle all \frac occurrences (including nested)
            s = ReplaceFractions(s);

            // Superscripts: x^{3} → x^3
            s = Regex.Replace(s, @"\^\{([^}]*)\}", "^$1");

            // Implicit multiplication: 2z → 2*z
            s = Regex.Replace(s, @"(\d)([a-zA-Z])", "$1*$2");

            // Implicit multiplication: z( → z*(
            s = Regex.Replace(s, @"z\(", "z*(");

            // Implicit multiplication for pi z → pi*z
            s = Regex.Replace(s, @"pi([a-zA-Z])", "pi*$1");

            return s;
        }

        private static string ReplaceFractions(string s)
        {
            const string frac = @"\frac{";
            while (true)
            {
                int start = s.IndexOf(frac, StringComparison.Ordinal);
                if (start == -1)
                    break;

                int numStart = start + frac.Length;
                int numEnd = FindMatchingBrace(s, numStart - 1); // position of closing '}'

                int denOpenBrace = numEnd + 1;
                if (denOpenBrace >= s.Length || s[denOpenBrace] != '{')
                    throw new Exception(@"Invalid \frac: missing { for denominator");

                int denStart = denOpenBrace + 1;
                int denEnd = FindMatchingBrace(s, denOpenBrace); // closing '}' of denominator

                string numerator = s.Substring(numStart, numEnd - numStart);
                string denominator = s.Substring(denStart, denEnd - denStart);

                string before = s.Substring(0, start);
                string after = s.Substring(denEnd + 1);

                string replacement = $"({numerator})/({denominator})";
                s = before + replacement + after;
            }

            return s;
        }

        private static int FindMatchingBrace(string s, int openingBraceIndex)
        {
            if (s[openingBraceIndex] != '{')
                throw new ArgumentException("Expected '{' at openingBraceIndex");

            int depth = 0;
            for (int i = openingBraceIndex; i < s.Length; i++)
            {
                if (s[i] == '{')
                    depth++;
                else if (s[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            throw new Exception("Unbalanced braces in LaTeX string");
        }
    }
}
