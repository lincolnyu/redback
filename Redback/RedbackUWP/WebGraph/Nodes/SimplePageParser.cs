using System.IO;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph.Nodes
{
    public class SimplePageParser : BaseSimplePageParser
    {
        #region Methods

        protected override BaseAction CreateDownloader(string link, string dir, string fileName)
        {
            var owner = (SiteGraph)Owner;
            dir = Path.Combine(owner.BaseDirectory, dir);

            var downloader = new MySocketDownloader
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
   
        #endregion
    }
}
