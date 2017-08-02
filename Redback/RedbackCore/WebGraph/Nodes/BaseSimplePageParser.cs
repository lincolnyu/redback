using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph.Nodes
{
    public abstract class BaseSimplePageParser : BaseNode
    {
        #region Properties

        public string Page
        {
            get; set;
        }

        public string OutputPage
        {
            get; private set;
        }

        #endregion

        #region Methods

        private static int FindParameter(string page, string parameterName, int start)
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

        private static int GetLink(string page, int pos, out string link)
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

        private string GetAbsoluteUrl(string baseUrl, string link)
        {
            if (link.StartsWith("http://") || link.StartsWith("https://"))
            {
                return link;
            }

            string baseAddr;
            string basePrefix;
            if (baseUrl.StartsWith("http://"))
            {
                basePrefix = "http://";
                baseAddr = baseUrl.Substring("http://".Length);
            }
            else if (baseUrl.StartsWith("https://"))
            {
                basePrefix = "https://";
                baseAddr = baseUrl.Substring("https://".Length);
            }
            else
            {
                // otherwise just use baseAddr as-is
                basePrefix = "http://";
                baseAddr = baseUrl.TrimEnd('/');
            }

            List<string> addrSegs;
            if (link.StartsWith("/"))
            {
                var firstSlash = baseAddr.IndexOf('/');
                if (firstSlash < 0) firstSlash = baseAddr.Length;
                var hostName = baseAddr.Substring(0, firstSlash);
                addrSegs = new List<string> {hostName};
                link = link.TrimStart('/');
                var linkSegs = link.Split('/');
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
                addrSegs = baseAddr.Split('/').ToList();
                if (addrSegs.Count > 1)
                {
                    addrSegs.RemoveAt(addrSegs.Count - 1);
                }
                var linkToSeg = link.TrimEnd('/');
                var linkSegs = linkToSeg.Split('/');
                foreach (var linkSeg in linkSegs)
                {
                    switch (linkSeg)
                    {
                        case "..":
                            if (addrSegs.Count < 2)
                            {
                                return null;
                            }
                            addrSegs.RemoveAt(addrSegs.Count-1);
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

        public override async Task Analyze()
        {
            var lastIndex = 0;
            var sbOutputPage = new StringBuilder();

            // TODO how about https?
            // TODO make sure the link is in HTML document rather than javascript

            var href = 0;
            var src = 0;
            var toFindHref = true;
            var toFindSrc = true;

            while (true)
            {
                if (toFindHref)
                {
                    href = FindParameter(Page, "href", lastIndex);
                    if (href < 0) href = Page.Length;
                    toFindHref = false;
                }

                if (toFindSrc)
                {
                    src = FindParameter(Page, "src", lastIndex);
                    if (src < 0) src = Page.Length;
                    toFindSrc = false;
                }

                if (href == Page.Length && src == Page.Length)
                {
                    break;
                }

                int index;
                if (src < href)
                {
                    index = src;
                    toFindSrc = true;
                }
                else
                {
                    index = href;
                    toFindHref = true;
                }

                var linkEnd = GetLink(Page, index, out string link); // one character after closing double quotation mark 
                if (linkEnd >= 0)
                {
                    var absLink = GetAbsoluteUrl(Url, link);
                    link = absLink;
                }
                if (linkEnd < 0 || link == null)
                {
                    var sb = Page.Substring(lastIndex, index - lastIndex);
                    sbOutputPage.Append(sb);
                    lastIndex = index;
                    continue;
                }

                var stringBetween = Page.Substring(lastIndex, index - lastIndex);
                sbOutputPage.Append(stringBetween);

                if (link.UrlToFilePath(out string dir, out string fileName))
                {
                    try
                    {
                        // check to see if the link has been downloaded
                        if (!Owner.HasDownloaded(link))
                        {
                            var download = CreateDownloader(link, dir, fileName);

                            Actions.Add(download);
                            Owner.AddObject(download);
                            Owner.SetHasDownloaded(link);
                        }

                        var fileUrl = string.Format("\"file:///{0}\"", dir);
                        sbOutputPage.Append(fileUrl);
                    }
                    catch (ArgumentException)
                    {
                        // skip this link which is probably invalid
                    }
                }

                lastIndex = linkEnd;
            }

            sbOutputPage.Append(Page.Substring(lastIndex, Page.Length - lastIndex));
            OutputPage = sbOutputPage.ToString();

            // writes output page to file
            if (InducingAction is BaseDownloader downloader)
            {
                await downloader.SaveAsync(OutputPage);
            }
        }

        protected abstract BaseAction CreateDownloader(string link, string dir, string fileName);

        #endregion
    }
}
