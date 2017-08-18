using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph
{
    public class HttpSiteGraph : BaseSiteGraph, ICommonGraph
    {
        #region Constructors
        
        public HttpSiteGraph(string startPage, string baseDirectory)
        {
            this.Initialize<HttpDownloader>(startPage, baseDirectory);
        }

        #endregion

        #region Properties

        public string BaseDirectory { get; private set; }

        #endregion

        #region Methods

        public void Setup(string baseDirectory, string startHost, GraphObject root)
        {
            BaseDirectory = baseDirectory;
            StartHost = startHost;
            RootObject = root;
        }

        #endregion
    }
}
