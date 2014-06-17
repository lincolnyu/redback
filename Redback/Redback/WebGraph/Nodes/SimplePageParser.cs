using System.IO;
using System.Text;
using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph.Nodes
{
    public class SimplePageParser : BaseNode
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

        public override void Analyze()
        {
            var lastIndex = 0;
            var index = 0;
            var sbOutputPage = new StringBuilder();

            // TODO how about https?
            // TODO make sure the link is in HTML document rather than javascript
            while ((index = Page.IndexOf("\"http://", index, System.StringComparison.Ordinal)) >= 0)
            {
                var stringBetween = Page.Substring(lastIndex, index - lastIndex);
                sbOutputPage.Append(stringBetween);

                var end = Page.IndexOf('"', index + "http://".Length);
                var link = Page.Substring(index + 1, end-index-1);

                string dir, fileName;
                if (link.UrlToFilePath(out dir, out fileName))
                {
                    dir = Path.Combine(Owner.BaseDirectory, dir);
                }

                // check to see if the link has been downloaded
                if (!Owner.HasDownloaded(link))
                {
                    var download = new MySocketDownloader
                    {
                        Owner = Owner,
                        SourceNode = this,
                        Level = Level + 1,
                        Url = link,
                        LocalDirectory = dir,
                        LocalFileName = fileName
                    };

                    Actions.Add(download);

                    Owner.SetHasDownloaded(link);
                }

                var fileUrl = string.Format("\"file:///{0}\"", dir);
                sbOutputPage.Append(fileUrl);

                lastIndex = end+1; // from one character after closing double quotation mark 
            }

            sbOutputPage.Append(Page.Substring(lastIndex, Page.Length - lastIndex));
            OutputPage = sbOutputPage.ToString();
        }        
   
        #endregion
    }
}
