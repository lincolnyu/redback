using System.IO;
using Redback.WebGraph.Actions;
using Redback.Helpers;

namespace Redback.WebGraph.Nodes
{
    public class SimplePageParser : BaseSimplePageParser
    {
        #region Methods

        protected override BaseAction CreateDownloader(string link)
        {
            if (!link.UrlToFilePath(out string dir, out string fileName))
            {
                return null;
            }

            var owner = (SocketSiteGraph)Owner;
            dir = GetProperDirectory(owner.BaseDirectory, dir);

            var downloader = new SocketDownloader
            {
                Owner = Owner,
                SourceNode = this,
                Level = Level + 1,
                Url = link,
                LocalDirectory = dir,
                LocalFileName = fileName,
                UseReferrer = true
            };

            return downloader;
        }     

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
