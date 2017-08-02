using System;
using System.Collections.Generic;
using QSharp.Scheme.Classical.Trees;
using Redback.Connections;
using Redback.Helpers;

namespace Redback.WebGraph
{
    public abstract class BaseSiteGraph<TWebAgent> : ISiteGraph, ISiteGraph<TWebAgent>
        where TWebAgent : IWebAgent
    {
        #region Delegates

        public delegate void ObjectProcessedEventHandler(object sender, ObjectProcessedEventArgs args);

        #endregion

        #region Nested types

        public class ObjectProcessedEventArgs : EventArgs
        {
            public GraphObject Object { get; set; }

            public bool Successful { get; set; }

            public string ErrorMessage { get; set; }
        }
        
        #endregion

        #region Fields

        protected const int MaxAgents = 64;

        protected readonly Dictionary<string, TWebAgent> _hostsToAgents;

        private readonly AvlTree<GraphObject> _queuedObjects;

        private int _objectCount;

        #endregion

        #region Constructors

        protected BaseSiteGraph()
        {
            DownloadedUrl = new HashSet<string>();
            HostLruQueue = new LinkedList<string>();
            _queuedObjects = new AvlTree<GraphObject>(TaskCompare);
            _hostsToAgents = new Dictionary<string, TWebAgent>();
        }

        #endregion

        #region Properties

        public string StartHost { get; protected set; }

        public GraphObject RootObject { get; set; }

        public string StartPage
        {
            get
            {
                return ((IHasUrl) RootObject).Url;
            }
        }

        /// <summary>
        ///  Returns queued objects in descending order of prioirty
        /// </summary>
        public IEnumerable<GraphObject> QueuedObjects
        {
            get
            {
                for (var node = _queuedObjects.Root.GetFirstInorder();
                    node != null;
                    node = node.GetNextInorder())
                {
                    var nodet = (AvlTreeWorker.INode<GraphObject>) node;
                    yield return nodet.Entry;
                }
            }
        }

        public int QueuedObjectCount
        {
            get
            {
                return _objectCount;
            }
        }

        public HashSet<string> DownloadedUrl { get; private set; }

        /// <summary>
        ///  Lookup table from host to agent 
        /// </summary>
        /// <remarks>
        ///  agents are only used for socket connection
        /// </remarks>
        public IReadOnlyDictionary<string, TWebAgent> HostsToAgents => _hostsToAgents;

        /// <summary>
        ///  LRU queue of hosts that contains exactly the same hosts as the keys in HostsToAgents
        ///  and gets rid of least used hosts over time
        /// </summary>
        public LinkedList<string> HostLruQueue
        {
            get;
            private set;
        }

        #endregion

        #region Events

        public event ObjectProcessedEventHandler ObjectProcessed;

        #endregion

        #region Methods

        public async void Run()
        {
            while (!_queuedObjects.IsEmpty)
            {
                var obj = PopObject();
                string errorMessage = null;
                try
                {
                    if (obj is BaseAction action)
                    {
                        await action.Perform();
                    }
                    else
                    {
                        if (obj is BaseNode node)
                        {
                            await node.Analyze();
                        }
                    }
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                }
                ObjectProcessed?.Invoke(this, new ObjectProcessedEventArgs
                {
                    Object = obj,
                    Successful = errorMessage == null,
                    ErrorMessage = errorMessage
                });
            }
        }

        public abstract TWebAgent GetOrCreateWebAgent(string hostName);
        
        private void AgeWebAgents()
        {
            while (_hostsToAgents.Count > MaxAgents)
            {
                var first = HostLruQueue.First.Value;
                HostLruQueue.RemoveFirst();
                _hostsToAgents.Remove(first);
            }
        }

        public void AddObject(GraphObject obj)
        {
            _queuedObjects.Insert(obj);
            _objectCount++;
        }

        public GraphObject PopObject()
        {
            if (_queuedObjects.IsEmpty)
            {
                return null;
            }

            var firstf = _queuedObjects.Root.GetFirstInorder();
            var firstt = (AvlTreeWorker.INode<GraphObject>)firstf;
            _queuedObjects.Remove(firstt);
            _objectCount--;
            return firstt.Entry;
        }

        private bool IsOnHostOrReferencedByHostPage(GraphObject x)
        {
            if (x is IHasUrl hasUrl && hasUrl.Url.UrlToHostName(out string dummy, out string hostName, out dummy) && hostName == StartHost)
            {
                return true;
            }

            var action = x as BaseAction;
            if (action == null || !action.SourceNode.Url.UrlToHostName(out dummy, out hostName, out dummy))
            {
                return false;
            }

            return hostName == StartHost;
        }

        private int TaskCompare(GraphObject x, GraphObject y)
        {
            if (Equals(x, y))
            {
                return 0;
            }

            var lvlcomp = x.Level.CompareTo(y.Level);
            if (lvlcomp != 0)
            {
                return lvlcomp; // BFS
            }

            // if x and y are at the same level, they should be both actions or both nodes
            if (x is BaseAction && y is BaseAction)
            {
                var xIsHostAdhered = IsOnHostOrReferencedByHostPage(x);
                var yIsHostAdhered = IsOnHostOrReferencedByHostPage(y);
                if (xIsHostAdhered && !yIsHostAdhered)
                {
                    return -1;
                }
                if (!xIsHostAdhered && yIsHostAdhered)
                {
                    return 1;
                }
            }

            var yHasUrl = y as IHasUrl;
            var xHasUrl = x as IHasUrl;
            if (xHasUrl != null && yHasUrl != null)
            {
                var xUrl = xHasUrl.Url;
                var yUrl = yHasUrl.Url;

                var comp = StartPage.CompareUrlDistances(xUrl, yUrl);

                if (comp != 0)
                {
                    return comp;
                }

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

                return -1;// doesn't matter
            }

            // task object without a URL has higher priority
            if (xHasUrl != null)
            {
                return -1;
            }

            if (yHasUrl != null)
            {
                return 1;
            }

            // NOTE x is given higher priority the tree node should appear on the left in searching
            return -1; // doesn't matter
        }

        public bool HasDownloaded(string link)
        {
            link = link.ToLower().Trim();
            return DownloadedUrl.Contains(link);
        }

        public void SetHasDownloaded(string link)
        {
            link = link.ToLower().Trim();
            DownloadedUrl.Add(link);
        }

        #endregion
    }
}
