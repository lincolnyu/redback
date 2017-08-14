using System;
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
                if (linkEnd < 0 || link == null)
                {
                    var sb = Page.Substring(lastIndex, index - lastIndex);
                    sbOutputPage.Append(sb);
                    lastIndex = index;
                    continue;
                }
                var absLink = UrlHelper.GetAbsoluteUrl(Url, link);
                link = absLink;

                var stringBetween = Page.Substring(lastIndex, index - lastIndex);
                sbOutputPage.Append(stringBetween);

                var downloaded = Owner.HasDownloaded(link);
                try
                {
                    // check to see if the link has been downloaded
                    if (!downloaded)
                    {
                        var download = CreateDownloader(link);
                        if (download != null)
                        {
                            Actions.Add(download);
                            Owner.AddObject(download);
                            Owner.SetHasDownloaded(link);
                            downloaded = true;
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // skip this link which is probably invalid
                }

                if (downloaded)
                {
                    var fileUrl = UrlHelper.GetFileRelative(Url, link);
                    var u = $"\"{fileUrl}\"";
                    sbOutputPage.Append(u);
                }
                else
                {
                    // have to use original
                    sbOutputPage.Append(Page.Substring(index, linkEnd));
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

        protected abstract BaseAction CreateDownloader(string link);

        #endregion
    }
}
