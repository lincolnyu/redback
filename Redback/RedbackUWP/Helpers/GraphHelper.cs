using Redback.WebGraph;
using Redback.WebGraph.Actions;
using System.Threading.Tasks;

namespace Redback.Helpers
{
    public static class GraphHelper
    {
        public static void ConstructGraph<TDownloader>(this ICommonGraph graph, 
            string startPage, string baseDirectory)
            where TDownloader : FileDownloader, new()
        {
            var page = new TDownloader
            {
                Url = startPage,
                Owner = graph,
            };
            graph.AddObject(page);
            graph.Setup(baseDirectory, page);
        }

        public static async Task InitializeGraph(this ICommonGraph graph)
        {
            var page = (FileDownloader)graph.RootObject;
            var url = await page.GetActualUrl();
            var startHost = url.GetHost();
        }
    }
}
