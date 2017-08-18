using Redback.WebGraph;
using Redback.WebGraph.Actions;
using System.IO;

namespace Redback.Helpers
{
    public static class GraphHelper
    {
        public static void Initialize<TDownloader>(this ICommonGraph graph, 
            string startPage, string baseDirectory)
            where TDownloader : FileDownloader, new()
        {
            startPage.UrlToHostName(out string prefix, out string hostName, out string path);

            if (startPage.UrlToFilePath(out string dir, out string fileName))
            {
                dir = Path.Combine(baseDirectory, dir);
            }

            var page = new TDownloader
            {
                Url = startPage,
                Owner = graph,
                LocalDirectory = dir,
                LocalFileName = fileName
            };
            graph.AddObject(page);
            graph.Setup(baseDirectory, hostName, page);
        }
    }
}
