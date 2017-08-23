namespace Redback.WebGraph
{
    public class HttpSiteGraph : BaseSiteGraph, ICommonGraph
    {
        #region Properties

        public string BaseDirectory { get; private set; }

        #endregion

        #region Methods

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
