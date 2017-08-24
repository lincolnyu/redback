using Redback.UrlManagement;
using Redback.WebGraph;

namespace Redback
{
    public class DownloadManager<TSiteGraph, TUrlPool, TUrlRegulator>  
        : IDownloadManager<TSiteGraph, TUrlPool, TUrlRegulator>
        where TSiteGraph : ISiteGraph where TUrlPool : IUrlPool where TUrlRegulator : IUrlRegulator 
    {
        public TSiteGraph Graph { get; set; }

        public TUrlPool UrlPool{ get; set; } 

        public TUrlRegulator UrlRegulator { get; set; }

        ISiteGraph IDownloadManager.Graph => Graph;

        IUrlPool IDownloadManager.UrlPool => UrlPool;

        IUrlRegulator IDownloadManager.UrlRegulator => UrlRegulator;
    }
}
