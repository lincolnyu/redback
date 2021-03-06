﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Redback.Helpers
{
    public static class UrlHelper
    {
        #region Types

        public enum UrlType
        {
            Absolute,
            Rootbased,
            Relative
        }

        public delegate string ValidateFileNameDelegate(string originalName);

        #endregion

        #region Properties

        public const char DirectorySeparator = '\\';

        public const string Http = "http://";
        public const string Https = "https://";

        #endregion

        #region Methods

        public static bool IsHttps(this string link)
            => link.StartsWith(Https);

        public static UrlType GetUrlType(this string link)
        {
            if (link.StartsWith(".")) // including ./ and ../
            {
                return UrlType.Relative;
            }
            if (link.StartsWith("/"))
            {
                return UrlType.Rootbased;
            }
            if (link.StartsWith(Http, StringComparison.OrdinalIgnoreCase)
                || link.StartsWith(Https, StringComparison.OrdinalIgnoreCase))
            {
                return UrlType.Absolute;
            }
            return UrlType.Relative;
        }

        public static string MakeHttpIfCan(this string link)
        {
            if (link.StartsWith(Https, StringComparison.OrdinalIgnoreCase)
                || link.StartsWith(Http, StringComparison.OrdinalIgnoreCase))
            {
                return link;
            }
            return $"{Http}{link}";
        }

        public static string MakeHttpsIfCan(this string link)
        {
            if (link.StartsWith(Https, StringComparison.OrdinalIgnoreCase)
                || link.StartsWith(Http, StringComparison.OrdinalIgnoreCase))
            {
                return link;
            }
            return $"{Https}{link}";
        }

        /// <summary>
        ///  Returns the URL with the redundant slashes removed
        /// </summary>
        /// <param name="url">The original url</param>
        /// <returns>The cleaned url</returns>
        public static string RemoveRedundantSlashesInUrl(this string url)
        {
            GetHostSeparators(url, out int endPrefix, out int rootSlash);
            var sb = new StringBuilder(url.Substring(0, endPrefix));
            var lastIsSlash = false;
            for (var i = endPrefix; i< url.Length; i++)
            {
                var c = url[i];
                if (c == '/')
                {
                    if (!lastIsSlash)
                    {
                        sb.Append(c);
                    }
                    lastIsSlash = true;
                }
                else
                {
                    sb.Append(c);
                    lastIsSlash = false;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///  Returns the absolute URL of <paramref name="link"/> based on <paramref name="baseUrl"/>
        ///  For instance, for base URL http://www.contoso.com/something.html the absolute URL of 
        ///  ./foo/bar.html is http://www.contoso.com/foo/bar.html
        /// </summary>
        /// <param name="baseUrl">The base URL</param>
        /// <param name="link">The URL to get the absolute URL for</param>
        /// <param name="enforcePrefix"></param>
        /// <returns>The absolute URL</returns>
        public static string GetAbsoluteUrl(this string orig, string url, bool enforcePrefix = true)
        {
            var urlType = url.GetUrlType();
            if (urlType == UrlType.Absolute)
            {
                return url;
            }
            orig.GetHostSeparators(out int endPrefix, out int rootSlash);
            if (enforcePrefix && endPrefix == 0)
            {
                orig = Http + orig;
                rootSlash += Http.Length;
            }
            if (urlType == UrlType.Rootbased)
            {
                var baseUrl = orig.Substring(0, rootSlash);
                return baseUrl + url;
            }
            return CombineUrl(orig, url);
        }

        private static string CombineUrl(string abs, string b)
        {
            abs.GetHostSeparators(out int dummy, out int slash);
            var patha = slash >= abs.Length? "" : abs.Substring(slash + 1);
            var heada = abs.Substring(0, slash);
            var segsa = patha.UrlSplit();
            var segsb = b.UrlSplit();
            var ai = segsa.Length-1;
            var eliminating = true;
            var sb = new StringBuilder();
            foreach (var segb in segsb)
            {
                if (eliminating)
                {
                    if (segb == "..")
                    {
                        ai--;
                    }
                    else if (segb != ".")
                    {
                        sb.Append(heada);
                        for (var i = 0; i < ai; i++)
                        {
                            sb.Append('/');
                            sb.Append(segsa[i]);
                        }
                        eliminating = false;
                    }
                }
                if (!eliminating)
                {
                    sb.Append('/');
                    sb.Append(segb);
                }
            }
            return sb.ToString();
        }


        /// <summary>
        ///  Returns the absolute URL of <paramref name="link"/> based on <paramref name="baseUrl"/>
        ///  For instance, for base URL http://www.contoso.com/something.html the absolute URL of 
        ///  ./foo/bar.html is http://www.contoso.com/foo/bar.html
        /// </summary>
        /// <param name="baseUrl">The base URL</param>
        /// <param name="link">The URL to get the absolute URL for</param>
        /// <param name="enforcePrefix">Add prefix if not existent in <paramref name="baseUrl"/></param>
        /// <returns>The absolute URL</returns>
        public static string GetAbsoluteUrl2(string baseUrl, string link, bool enforcePrefix = true)
        {
            if (link.StartsWith(Http, StringComparison.OrdinalIgnoreCase)
                || link.StartsWith(Https, StringComparison.OrdinalIgnoreCase))
            {
                return link;
            }

            string baseAddr;
            string basePrefix;
            if (baseUrl.StartsWith(Http, StringComparison.OrdinalIgnoreCase))
            {
                basePrefix = Http;
                baseAddr = baseUrl.Substring(Http.Length);
            }
            else if (baseUrl.StartsWith(Https, StringComparison.OrdinalIgnoreCase))
            {
                basePrefix = Https;
                baseAddr = baseUrl.Substring(Https.Length);
            }
            else
            {
                // otherwise just use baseAddr as-is
                basePrefix = enforcePrefix? Http : "";
                baseAddr = baseUrl.TrimEnd('/');
            }

            List<string> addrSegs;
            if (link.StartsWith("/"))
            {
                var firstSlash = baseAddr.IndexOf('/');
                if (firstSlash < 0) firstSlash = baseAddr.Length;
                var hostName = baseAddr.Substring(0, firstSlash);
                addrSegs = new List<string> { hostName };
                link = link.TrimStart('/');
                var linkSegs = link.UrlSplit();
                foreach (var linkSeg in linkSegs)
                {
                    switch (linkSeg)
                    {
                        case "..":
                            addrSegs.RemoveAt(addrSegs.Count - 1);
                            break;
                        case ".":
                            break;
                        default:
                            addrSegs.Add(linkSeg);
                            break;
                    }
                }
            }
            else
            {
                addrSegs = baseAddr.UrlSplit().ToList();
                if (addrSegs.Count > 1)
                {
                    addrSegs.RemoveAt(addrSegs.Count - 1);
                }
                var linkToSeg = link.TrimEnd('/');
                var linkSegs = linkToSeg.UrlSplit();
                foreach (var linkSeg in linkSegs)
                {
                    switch (linkSeg)
                    {
                        case "..":
                            if (addrSegs.Count < 2)
                            {
                                return null;
                            }
                            addrSegs.RemoveAt(addrSegs.Count - 1);
                            break;
                        case ".":
                            break;
                        default:
                            addrSegs.Add(linkSeg);
                            break;
                    }
                }
            }
            var sbAddr = new StringBuilder();
            sbAddr.Append(basePrefix);
            foreach (var addrSeg in addrSegs)
            {
                sbAddr.Append(addrSeg);
                sbAddr.Append('/');
            }

            if (addrSegs.Count > 1 && !link.EndsWith("/"))
            {
                sbAddr.Remove(sbAddr.Length - 1, 1);
            }
            return sbAddr.ToString();
        }

        private static void GetHostSeparators(this string abs, out int endPrefix, out int rootSlash)
        {
            endPrefix = 0;
            if (abs.StartsWith(Http, StringComparison.OrdinalIgnoreCase))
            {
                endPrefix = Http.Length;
            }
            else if (abs.StartsWith(Https, StringComparison.OrdinalIgnoreCase))
            {
                endPrefix = Https.Length;
            }
            rootSlash = abs.IndexOf('/', endPrefix);
            if (rootSlash < 0)
            {
                rootSlash = abs.Length;
            }
        }

        public static string ReplaceHost(this string original, string newHost)
        {
            original.GetHostSeparators(out int ep, out int rs);
            var sb = new StringBuilder(original.Substring(0, ep));
            sb.Append(newHost);
            sb.Append(original.Substring(rs));
            return sb.ToString();
        }


        public static string GetHost(this string abs)
            => abs.GetBaseUrl().BaseUrlToHost();

        /// <summary>
        ///  Get the base url (excluding the tailing /)
        /// </summary>
        /// <param name="abs">The original url</param>
        /// <returns>The base url</returns>
        public static string GetBaseUrl(this string abs)
        {
            abs.GetHostSeparators(out int dummy, out int rootSlash);
            return abs.Substring(0, rootSlash);
        }

        /// <summary>
        ///  Removes prefix of base to get the host address
        /// </summary>
        /// <param name="baseUrl">Base URL returned from GetBaseUrl</param>
        /// <returns>The host</returns>
        public static string BaseUrlToHost(this string baseUrl)
        {
            if (baseUrl.StartsWith(Http, StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl.Remove(0, Http.Length).Trim('/');
            }
            else if (baseUrl.StartsWith(Https, StringComparison.Ordinal))
            {
                return baseUrl.Remove(0, Https.Length).Trim('/');
            }
            return baseUrl.Trim('/');
        }

        /// <summary>
        ///  Gets the host name of the URL
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="prefix">The prefix such as http:// or https://</param>
        /// <param name="hostName">The host name part</param>
        /// <param name="path">The path part</param>
        public static void UrlToHostName(this string url, out string prefix, out string hostName, out string path)
        {
            url.GetHostSeparators(out int endPrefix, out int rootSlash);
            prefix = url.Substring(0, endPrefix);
            hostName = url.Substring(endPrefix, rootSlash - endPrefix);
            path = url.Substring(rootSlash);
        }

        /// <summary>
        ///  Converts a URL to a download file path
        ///  e.g.  http://www.contoso.com/foo/bar.asp will end up in
        ///   directory: "www.contoso.com\foo" (No trailing back slash)
        ///   file: bar.asp
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="directory">The directory to put the downloaded content in as per the URL</param>
        /// <param name="fileName">The file name of the downloaded content</param>
        /// <returns>True if successful</returns>
        public static bool UrlToFilePath(this string url, out string directory, out string fileName,
            ValidateFileNameDelegate validateFileName = null)
        {
            url = url.Trim().ToLower();
            if (url.StartsWith(Http))
            {
                url = url.Substring(Http.Length);
            }
            else if (url.StartsWith(Https))
            {
                url = url.Substring(Https.Length);
            }
            var segs = url.UrlSplit();
            var sbDirectory = new StringBuilder();
            if (segs.Length > 0)
            {
                if (segs.Length > 1)
                {
                    for (var i = 0; i < segs.Length - 2; i++)
                    {
                        sbDirectory.Append(segs[i]);
                        sbDirectory.Append(DirectorySeparator);
                    }
                    sbDirectory.Append(segs[segs.Length - 2]);
                    directory = sbDirectory.ToString();
                    fileName = segs[segs.Length - 1]; // leave validateFileName() to provide the default one
                }
                else
                {
                    directory = segs[0];
                    fileName = ""; // leave validateFileName() to provide the default one
                }
                fileName = validateFileName?.Invoke(fileName) ?? fileName;
                return true;
            }
            directory = null;
            fileName = null;
            return false;
        }

        public static string[] UrlSplit(this string url) => url.UrlSplitToEnum().ToArray();

        public static IEnumerable<string> UrlSplitToEnum(this string url)
        {
            var sb = new StringBuilder();
            var tail = false;
            foreach (var c in url)
            {
                if (c != '/' || tail)
                {
                    if (c == '?' || c == '&')
                    {
                        tail = true;
                    }
                    sb.Append(c);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                    }
                    sb.Clear();
                }
            }
            yield return sb.ToString(); // last bit should be exported regardless
        }

        public static string GetFileRelative(string baseUrl, string link,
            ValidateFileNameDelegate validateFileName = null,
            StringComparison sc = StringComparison.OrdinalIgnoreCase)
        {
            UrlToFilePath(baseUrl, out string dir1, out string fn1, validateFileName);
            UrlToFilePath(link, out string dir2, out string fn2, validateFileName);
            var split1 = dir1.Split(DirectorySeparator);
            var split2 = dir2.Split(DirectorySeparator);
            var diffStart = 0;
            for (; diffStart < Math.Min(split1.Length, split2.Length); diffStart++)
            {
                if (!string.Equals(split1[diffStart], split2[diffStart], sc))
                {
                    break;
                }
            }
            var sb = new StringBuilder();
            if (diffStart == split1.Length)
            {
                sb.Append("./");
            }
            else
            {
                for (var i = diffStart; i < split1.Length; i++)
                {
                    sb.Append("../");
                }
            }
            for (var i = diffStart; i < split2.Length; i++)
            {
                sb.Append(split2[i]);
                sb.Append('/');
            }
            sb.Append(fn2);
            return sb.ToString();
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
            if (url1 == url2)
            {
                return int.MaxValue;
            }

            url1.UrlToHostName(out string prefix1, out string host1, out string path1);
            url2.UrlToHostName(out string prefix2, out string host2, out string path2);

            if (host1 != host2)
            {
                return 1;
            }

            var pathSegs1 = path1.UrlSplit();
            var pathSegs2 = path2.UrlSplit();

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

        public static string ValidateFileName(this string original, char replaceChar)
        {
            if (string.IsNullOrWhiteSpace(original))
            {
                return "index.html"; // TODO this should be ok but who knows
            }

            var sb = new StringBuilder();
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in original)
            {
                if (!invalidChars.Contains(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(replaceChar);
                }
            }
            return sb.ToString();
        }

        public static string ValidateFileName(this string original)
            => original.ValidateFileName('_');

        #endregion
    }
}
