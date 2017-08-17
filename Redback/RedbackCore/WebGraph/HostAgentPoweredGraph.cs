using Redback.Helpers;
using System;
using System.Collections.Generic;

namespace Redback.WebGraph
{
    public abstract class HostAgentPoweredGraph<TWebAgent> : BaseSiteGraph
    {
        #region Fields

        protected const int MaxAgents = 64;

        protected readonly Dictionary<string, TWebAgent> _hostsToAgents = new Dictionary<string, TWebAgent>();

        #endregion

        #region Properties

        /// <summary>
        ///  Lookup table from host to agent 
        /// </summary>
        /// <remarks>
        ///  agents are only used for socket connection
        /// </remarks>
        public IReadOnlyDictionary<string, TWebAgent> HostsToAgents => _hostsToAgents;

        #endregion

        #region Methods

        public abstract TWebAgent GetOrCreateWebAgent(string hostName);

        protected void AgeWebAgents()
        {
            while (_hostsToAgents.Count > MaxAgents)
            {
                var first = HostLruQueue.First.Value;
                HostLruQueue.RemoveFirst();
                _hostsToAgents.Remove(first);
            }
        }

        protected override int TaskCompare(GraphObject x, GraphObject y)
        {
            var r = BaseTaskCompare(x, y);
            if (r != 0) return r;

            if (x is IHasUrl xHasUrl && y is IHasUrl yHasUrl)
            {
                var xUrl = xHasUrl.Url;
                var yUrl = yHasUrl.Url;

                xUrl.UrlToHostName(out string xPrefix, out string xHostName, out string dummy);
                yUrl.UrlToHostName(out string yPrefix, out string yHostName, out dummy);

                var xIsStart = string.Equals(xHostName, StartHost, StringComparison.OrdinalIgnoreCase);
                var yIsStart = string.Equals(yHostName, StartHost, StringComparison.OrdinalIgnoreCase);
                if (xIsStart && !yIsStart)
                {
                    return -1;
                }
                if (!xIsStart && yIsStart)
                {
                    return 1;
                }

                var xCached = _hostsToAgents.ContainsKey(xHostName);
                var yCached = _hostsToAgents.ContainsKey(yHostName);

                if (xCached && !yCached)
                {
                    return -1;
                }
                if (!xCached && yCached)
                {
                    return 1;
                }
            }
            return -1;// doesn't matter
        }

        #endregion
    }
}
