using System.IO;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph
{
    public class SiteGraph : BaseSiteGraph<WebAgent>
    {
        #region Constructors

        public SiteGraph(string startPage, string baseDirectory)
        {
            BaseDirectory = baseDirectory;
            startPage.UrlToHostName(out string prefix, out string hostName, out string path);
            StartHost = hostName;

            if (startPage.UrlToFilePath(out string dir, out string fileName))
            {
                dir = Path.Combine(BaseDirectory, dir);
            }

            var page = new MySocketDownloader
            {
                Url = startPage,
                Owner = this,
                LocalDirectory = dir,
                LocalFileName = fileName
            };
            AddObject(page);

            RootObject = page;
        }

        #endregion

        #region Properties
        
        public string BaseDirectory { get; private set; }

        #endregion
        
        #region Methods
        
        public override WebAgent GetOrCreateWebAgent(string hostName)
        {
            if (!_hostsToAgents.TryGetValue(hostName, out WebAgent agent))
            {
                agent = new WebAgent(hostName);
                _hostsToAgents[hostName] = agent;
                HostLruQueue.AddLast(hostName);
                AgeWebAgents();
            }
            else
            {
                // find it in the queue
                HostLruQueue.Remove(hostName);
                HostLruQueue.AddLast(hostName);
            }
            return agent;
        }

        private void AgeWebAgents()
        {
            while (_hostsToAgents.Count > MaxAgents)
            {
                var first = HostLruQueue.First.Value;
                HostLruQueue.RemoveFirst();
                _hostsToAgents.Remove(first);
            }
        }
        
        #endregion
    }
}
