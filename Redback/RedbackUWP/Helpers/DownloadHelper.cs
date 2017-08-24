using Redback.UrlManagement;
using Redback.WebGraph;
using Redback.WebGraph.Actions;
using System.Threading.Tasks;

namespace Redback.Helpers
{
    public static class DownloadHelper
    {
        public static DownloadManager<TGraph, TUrlPool, TUrlRegulator> 
            CreateManager<TGraph, TUrlPool, TUrlRegulator, TDownloader>(
            string startPage, string baseDirectory)
            where TGraph : ICommonGraph, new()
            where TUrlPool : IUrlPool, new()
            where TUrlRegulator : IUrlRegulator, new()
            where TDownloader : FileDownloader, new()
        {
            var manager = new DownloadManager<TGraph, TUrlPool, TUrlRegulator>();
            var page = new TDownloader
            {
                Url = startPage,
                Owner = manager,
            };
            manager.Graph = new TGraph();
            manager.Graph.AddObject(page);
            manager.Graph.Setup(baseDirectory, page);

            manager.UrlPool = new TUrlPool();

            manager.UrlRegulator = new TUrlRegulator();

            return manager;
        }

        public static async Task Initialize<TGraph, TUrlPool, TUrlRegulator>(this DownloadManager<TGraph, TUrlPool, TUrlRegulator> manager)
            where TGraph : ICommonGraph, new()
            where TUrlPool : IUrlPool, new()
            where TUrlRegulator : IUrlRegulator, new()
        {
            var page = (FileDownloader)manager.Graph.RootObject;
            var url = await page.GetActualUrl();
            manager.Graph.SetStartHost(url.GetHost());
            manager.UrlPool.Subscribe(url, null);
        }
    }
}
