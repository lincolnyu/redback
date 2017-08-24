using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Redback.Helpers
{
    public static class HtmlNaiveHelper
    {
        /// <summary>
        ///  Find the next parameter assigment 'parameterName = parameterValue' and returns the 
        ///  index of the first character after the equal sign
        /// </summary>
        /// <param name="page">The page frokm which to find the parameter assignment</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="start">The position in the page where the search starts</param>
        /// <returns>The position after the equal sign</returns>
        public static int FindParameter(this string page, string parameterName, int start)
        {
            var q = start;
            while (q < page.Length)
            {
                var p = page.IndexOf(parameterName, q, StringComparison.Ordinal);
                if (p < 0) return -1;
                q = p + parameterName.Length;
                for (; q < page.Length; q++)
                {
                    var c = page[q];
                    if (c == '=')
                    {
                        return q + 1;
                    }
                    if (!char.IsWhiteSpace(c))
                    {
                        break;
                    }
                }
            }
            return -1;
        }

        public delegate Task<int> ProcessParameterDelegate(int pos, string parameter);

        public static async Task FindAnyParameterAsync(this string page, ICollection<string> parameters, int start,
            ProcessParameterDelegate processParameter)
        {
            var parDict = new Dictionary<string, int>();
            var min = int.MaxValue;
            string minPar = null;
            foreach(var parameter in parameters)
            {
                var p = page.FindParameter(parameter, start);
                parDict[parameter] = p;
                if (p >= 0 && p < min)
                {
                    minPar = parameter;
                    min = p;
                }
            }

            while (minPar != null)
            { 
                var next = await processParameter(min, minPar);

                if (next < 0) break; // early quit requested by user

                var p = page.FindParameter(minPar, next);
                parDict[minPar] = p;

                // find min
                min = int.MaxValue;
                minPar = null;
                foreach (var pair in parDict)
                {
                    if (pair.Value >= 0 && pair.Value < min)
                    {
                        minPar = pair.Key;
                        min = pair.Value;
                    }
                }
            }
        }

        /// <summary>
        ///  Get the link value from within the quotation marks (double or single)
        /// </summary>
        /// <param name="page">The page</param>
        /// <param name="pos">The position which should be ahead of quoted string with no more than spaces in between</param>
        /// <param name="link">The link value (without quotation marks)</param>
        /// <returns>The position one character after the closing quotation mark</returns>
        public static int GetLink(this string page, int pos, out string link)
        {
            for (; pos < page.Length && char.IsWhiteSpace(page[pos]); pos++)
            {
            }

            link = null;

            if (pos >= page.Length)
            {
                return -1;
            }

            var qm = page[pos];
            if (qm != '\'' && qm != '"')
            {
                return -1;
            }

            var end = page.IndexOf(qm, pos + 1);
            if (end < 0)
            {
                return -1;
            }

            link = page.Substring(pos + 1, end - pos - 1);
            return end + 1;
        }

    }
}
