using System;
using System.Text;
using System.Threading.Tasks;
using Redback.Helpers;
using Redback.WebGraph.Actions;
using System.Collections.Generic;

namespace Redback.WebGraph.Nodes
{
    public abstract class BaseSimplePageParser : BaseNode, IActualUrlReceiver
    {
        #region Types

        private abstract class StringSegment
        {
            public abstract string SourceString { get; set; }
            public string TargetString;
        }

        private class StringAsIs : StringSegment
        {
            public override string SourceString
            {
                get => TargetString;
                set { TargetString = value; }
            }
        }

        private class StringToBeChanged : StringSegment
        {
            public override string SourceString
            {
                get;
                set;
            }
        }

        #endregion

        #region Fields

        private LinkedList<StringSegment> _stringSegments = new LinkedList<StringSegment>();
        private Dictionary<BaseDownloader, StringSegment> _downloaderToSegment = new Dictionary<BaseDownloader, StringSegment>();

        #endregion

        #region Properties

        public string Page
        {
            get; set;
        }
        
        #endregion

        #region Methods

        #region IReportActualUrl

        public async void ReportActualUrl(object reporter)
        {
            if (reporter is BaseDownloader downloader)
            {
                if (_downloaderToSegment.TryGetValue(downloader, out var ss))
                {
                    var originalUrl = downloader.Url;
                    var actualUrl = await downloader.GetActualUrl();
                    var fileUrl = UrlHelper.GetFileRelative(Url, actualUrl, UrlHelper.ValidateFileName);
                    var u = $"\"{fileUrl}\"";
                    ss.TargetString = u;
                }
            }
        }

        #endregion

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
        
        public async void Conclude()
        {
            var sbOutputPage = new StringBuilder();
            foreach (var ss in _stringSegments)
            {
                sbOutputPage.Append(ss);
            }
            
            // writes output page to file
            if (InducingAction is BaseDownloader downloader)
            {
                await downloader.SaveAsync(sbOutputPage.ToString());
            }
        }

        public override async Task Analyze()
        {
            var lastIndex = 0;
            
            // TODO how about https?
            // TODO make sure the link is in HTML document rather than javascript

            await Page.FindAnyParameterAsync(new[] { "href", "src" }, 0,
                async (index, parameter) =>
                {
                    var linkEnd = Page.GetLink(index, out string link); // one character after closing double quotation mark 
                    if (linkEnd < 0 || link == null)
                    {
                        var sb = Page.Substring(lastIndex, index - lastIndex);
                        _stringSegments.AddLast(new StringAsIs { SourceString = sb });
                        lastIndex = index;
                        return lastIndex;
                    }
                    var absLink = UrlHelper.GetAbsoluteUrl(Url, link);
                    link = absLink;

                    var stringBetween = Page.Substring(lastIndex, index - lastIndex);
                    _stringSegments.AddLast(new StringAsIs { SourceString = stringBetween });

                    var downloaded = false;
                    try
                    {
                        if (!Owner.UrlPool.IsInThePool(link))
                        {
                            var download = CreateDownloader(link);
                            if (download != null)
                            {
                                Actions.Add(download);
                                Owner.Graph.AddObject(download);
                                Owner.UrlPool.Subscribe(link, this);
                                _stringSegments.AddLast(new StringToBeChanged { SourceString = link});
                                downloaded = true;
                            }
                        }
                        else if (Owner.UrlPool.IsDownloaded(link, out string actualUrl))
                        {
                            _stringSegments.AddLast(new StringToBeChanged { SourceString = link, TargetString = actualUrl });
                            downloaded = true;
                        }
                        else
                        {
                            _stringSegments.AddLast(new StringToBeChanged { SourceString = link });
                            downloaded = true;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // skip this link which is probably invalid
                        System.Diagnostics.Debug.WriteLine("Argument Exception occurred when creating downloader");
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("Unexpected exception occurred when creating downloader");
                    }

                    if (!downloaded)
                    {
                        // have to use original
                        var orig = Page.Substring(index, linkEnd - index);
                        _stringSegments.AddLast(new StringAsIs { SourceString = orig });
                    }

                    System.Diagnostics.Debug.WriteLine($"{link} processed.");

                    lastIndex = linkEnd;
                    return linkEnd;
                });

            _stringSegments.AddLast(new StringAsIs { SourceString =
                Page.Substring(lastIndex, Page.Length - lastIndex) });
        }

        protected abstract BaseDownloader CreateDownloader(string link);

        #endregion
    }
}
