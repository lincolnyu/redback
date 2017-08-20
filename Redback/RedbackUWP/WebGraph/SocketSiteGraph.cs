using System.Threading.Tasks;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph
{
    public class SocketSiteGraph : HostAgentPoweredGraph<SocketWebAgent>, ICommonGraph
    {
        #region Constructors

        public SocketSiteGraph(string startPage, string baseDirectory)
        {
            this.ConstructGraph<SocketDownloader>(startPage, baseDirectory);
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
