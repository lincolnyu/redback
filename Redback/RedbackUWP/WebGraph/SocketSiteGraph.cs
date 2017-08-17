using System.IO;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph
{
    public class SocketSiteGraph : HostAgentPoweredGraph<SocketWebAgent>
    {
        #region Constructors

        public SocketSiteGraph(string startPage, string baseDirectory)
        {
            BaseDirectory = baseDirectory;
            startPage.UrlToHostName(out string prefix, out string hostName, out string path);
            StartHost = hostName;

            if (startPage.UrlToFilePath(out string dir, out string fileName))
            {
                dir = Path.Combine(BaseDirectory, dir);
            }

            var page = new SocketDownloader
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
        
        public override SocketWebAgent GetOrCreateWebAgent(string hostName)
        {
            if (!_hostsToAgents.TryGetValue(hostName, out SocketWebAgent agent))
            {
                agent = new SocketWebAgent(hostName);
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
        
        #endregion
    }
}
