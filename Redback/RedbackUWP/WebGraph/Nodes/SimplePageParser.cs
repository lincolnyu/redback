using System.IO;
using Redback.WebGraph.Actions;
using Redback.Helpers;

namespace Redback.WebGraph.Nodes
{
    public class SimplePageParser : BaseSimplePageParser
    {
        #region Types

        public delegate FileDownloader CreateDownloaderDelegate(
            IDownloadManager owner, 
            SimplePageParser source,
    int level, string url);

        #endregion

        #region Constructors

        public SimplePageParser(CreateDownloaderDelegate createDownloader)
        {
            CreateDownloaderCallback = createDownloader;
        }

        #endregion

        #region Methods

        protected override BaseDownloader CreateDownloader(string link)
            => CreateDownloaderCallback(Owner, this, Level + 1, link);

        public CreateDownloaderDelegate CreateDownloaderCallback { get; private set; }

        private string GetProperDirectory(string baseDir, string dir)
        {
            var split = dir.Split(UrlHelper.DirectorySeparator);
            if (split[0].StartsWith("www.", System.StringComparison.OrdinalIgnoreCase))
            {
                var trim = split[0].Substring(4);
                if (Directory.Exists(Path.Combine(baseDir, trim)))
                {
                    return Path.Combine(baseDir, trim);
                }
                return Path.Combine(baseDir, dir);
            }

            if(Directory.Exists(Path.Combine(baseDir, "www." + split[0])))
            {
                return Path.Combine(baseDir, "www." + dir);
            }
            return Path.Combine(baseDir, dir);
        }

        #endregion
    }
}
