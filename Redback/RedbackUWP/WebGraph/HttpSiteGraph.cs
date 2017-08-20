using Redback.Helpers;
using Redback.WebGraph.Actions;
using System.Threading.Tasks;

namespace Redback.WebGraph
{
    public class HttpSiteGraph : BaseSiteGraph, ICommonGraph
    {
        #region Constructors
        
        public HttpSiteGraph(string startPage, string baseDirectory)
        {
            this.ConstructGraph<HttpDownloader>(startPage, baseDirectory);
        }

        #endregion

        #region Properties

        public string BaseDirectory { get; private set; }

        #endregion

        #region Methods

        public async Task Initialize()
        {
            await this.InitializeGraph();
        }

        public void SetStartHost(string startHost)
        {
            StartHost = startHost;
        }

        public void Setup(string baseDirectory, GraphObject root)
        {
            BaseDirectory = baseDirectory;
            RootObject = root;
        }

        #endregion
    }
}
