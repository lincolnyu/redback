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

        public static bool UrlToHostName(this string url, out string prefix, out string hostName, out string path)
        {
            var urlLc = url.ToLower();
            string processed;
            if (urlLc.StartsWith("http://"))
            {
                processed = urlLc.Substring("http://".Length);
                prefix = "http://";
            }
            else if (urlLc.StartsWith("https://"))
            {
                processed = urlLc.Substring("https://".Length);
                prefix = "https://";
            }
            else
            {
                processed = urlLc;
                prefix = "http://";
            }
            var end = processed.IndexOf('/');
            if (end >= 0)
            {
                hostName = processed.Substring(0, end);
                path = processed.Substring(end);
                return true;
            }

            hostName = processed;
            path = "";
            return true;
        }

        public static bool UrlToFilePath(this string url, out string directory, out string fileName)
        {
            url = url.Trim().ToLower();
            if (url.StartsWith("http://"))
            {
                url = url.Substring("http://".Length);
            }
            else if (url.StartsWith("https://"))
            {
                url = url.Substring("https://".Length);
            }
            var segs = url.Split('/');
            var sbDirectory = new StringBuilder();
            if (segs.Length > 0)
            {
                for (var i = 0; i < segs.Length - 1; i++)
                {
                    sbDirectory.Append(segs[i]);
                    sbDirectory.Append('\\');
                }
                directory = sbDirectory.ToString();
                fileName = segs[segs.Length - 1];
                return true;
            }
            directory = null;
            fileName = null;
            return false;
        }

        /// <summary>
        ///  Returns the URL distance indicator between two URLs
        /// </summary>
        /// <param name="url1">The 1st URL</param>
        /// <param name="url2">The 2nd URL</param>
        /// <returns>The distance indicator</returns>
        /// <remarks>
        ///  if two URLs are equal, the distance indictor is int.MaxValue
        ///  if two URLs are different in host, the distance indicator is 1 indicating the greatest possible difference
        ///  otherwise the indicator indicates the level of path they start to be different
        /// </remarks>
        public static int UrlDistance(this string url1, string url2)
        {
            string prefix1, host1, path1;
            string prefix2, host2, path2;

            if (url1 == url2)
            {
                return int.MaxValue;
            }

            url1.UrlToHostName(out prefix1, out host1, out path1);
            url2.UrlToHostName(out prefix2, out host2, out path2);

            if (host1 != host2)
            {
                return 1;
            }

            var pathSegs1 = path1.Split('/');
            var pathSegs2 = path2.Split('/');

            int i;
            for (i = 0; i < pathSegs1.Length && i < pathSegs2.Length; i++)
            {
                var seg1 = pathSegs1[i];
                var seg2 = pathSegs2[i];

                if (seg1 != seg2)
                {
                    return i + 2;
                }
            }

            return i + 2;
        }

        public static int CompareUrlDistances(this string url, string url1, string url2)
        {
            var dist1 = url.UrlDistance(url1);
            var dist2 = url.UrlDistance(url2);

            return dist1.CompareTo(dist2);
        }

        public static string GetParameter(this string header, string parameter, bool trim=true)
        {
            var start = header.IndexOf(parameter, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }
            start += + parameter.Length;
            var end = header.IndexOf("\r\n", start, StringComparison.Ordinal);
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
