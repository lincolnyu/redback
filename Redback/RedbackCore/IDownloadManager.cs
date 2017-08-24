using Redback.UrlManagement;
using Redback.WebGraph;

namespace Redback
{
    public interface IDownloadManager
    {
        ISiteGraph Graph { get; }
        IUrlPool UrlPool { get; }
        IUrlRegulator UrlRegulator { get; }
    }

    public interface IDownloadManager<out TSiteGraph, out TUrlPool, out TUrlRegulator>
        : IDownloadManager
        where TSiteGraph : ISiteGraph where TUrlPool : IUrlPool where TUrlRegulator : IUrlRegulator
    {
        new TSiteGraph Graph { get; }
        new TUrlPool UrlPool { get; }
        new TUrlRegulator UrlRegulator { get; }
    }
}
