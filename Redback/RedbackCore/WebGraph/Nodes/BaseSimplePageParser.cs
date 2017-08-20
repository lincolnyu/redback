using System;
using System.Text;
using System.Threading.Tasks;
using Redback.Helpers;
using Redback.WebGraph.Actions;
using System.Collections.Generic;

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

        private static IEnumerable<int> FindAnyParameter(string page, ICollection<string> parameterName, int start)
        {
            for (var pos = start; ; )
            {

            }
        }

        public override async Task Analyze()
        {
            var lastIndex = 0;
            var sbOutputPage = new StringBuilder();

            // TODO how about https?
            // TODO make sure the link is in HTML document rather than javascript

            await Page.FindAnyParameterAsync(new[] { "href", "src" }, 0,
                async (index, parameter) =>
                {
                    var linkEnd = Page.GetLink(index, out string link); // one character after closing double quotation mark 
                    if (linkEnd < 0 || link == null)
                    {
                        var sb = Page.Substring(lastIndex, index - lastIndex);
                        sbOutputPage.Append(sb);
                        lastIndex = index;
                        return lastIndex;
                    }
                    var absLink = UrlHelper.GetAbsoluteUrl(Url, link);
                    link = absLink;

                    var stringBetween = Page.Substring(lastIndex, index - lastIndex);
                    sbOutputPage.Append(stringBetween);

                    var downloaded = Owner.HasDownloaded(link);
                    string actualUrl = link;
                    try
                    {
                        // check to see if the link has been downloaded
                        if (!downloaded)
                        {
                            var download = CreateDownloader(link);
                            if (download != null)
                            {
                                actualUrl = await download.GetActualUrl();
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
                        var fileUrl = UrlHelper.GetFileRelative(Url, actualUrl, UrlHelper.ValidateFileName);
                        var u = $"\"{fileUrl}\"";
                        sbOutputPage.Append(u);
                    }
                    else
                    {
                        // have to use original
                        sbOutputPage.Append(Page.Substring(index, linkEnd));
                    }
                    lastIndex = linkEnd;
                    return linkEnd;
                });

            sbOutputPage.Append(Page.Substring(lastIndex, Page.Length - lastIndex));
            OutputPage = sbOutputPage.ToString();

            // writes output page to file
            if (InducingAction is BaseDownloader downloader)
            {
                await downloader.SaveAsync(OutputPage);
            }
        }

        protected abstract BaseDownloader CreateDownloader(string link);

        #endregion
    }
}
