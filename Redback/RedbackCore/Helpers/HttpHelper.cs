using System;
using System.Text;

namespace Redback.Helpers
{
    public static class HttpHelper
    {
        #region Fields

        public const string NewLine = "\r\n";

        #endregion

        #region Methods

        public static string GetParameter(this string header, string parameter, bool trim=true)
        {
            var start = header.IndexOf(parameter, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }
            start += + parameter.Length;
            var end = header.IndexOf(NewLine, start, StringComparison.Ordinal);
            if (end < 0)
            {
                return null;
            }
            var result = header.Substring(start, end - start);
            if (trim)
            {
                result = result.Trim();
            }
            return result;
        }

        public static void AddParameter(this StringBuilder sbRequest, string parameterAndValue)
        {
            sbRequest.Append(parameterAndValue + NewLine);
        }

        public static void AddParameterFormat(this StringBuilder sbRequest, string format, params object[] values)
        {
            sbRequest.AppendFormat(format + NewLine, values);
        }

        public static void ConcludeRequest(this StringBuilder sbRequest)
        {
            sbRequest.Append(NewLine);
        }
        
        #endregion
    }
}
