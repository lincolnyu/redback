using System;
using System.Collections.Generic;
using QSharp.Scheme.Classical.Trees;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Actions;

namespace Redback.WebGraph
{
    public class SiteGraph
    {
        #region Delegates

        public delegate void ObjectProcessedEventHandler(object sender, ObjectProcessedEventArgs args);

        #endregion

        #region Nested types

        public class ObjectProcessedEventArgs : EventArgs
        {
            public GraphObject Object { get; set; }
        }
        
        #endregion

        #region Fields

        private const int MaxAgents = 64;

        private readonly Dictionary<string, WebAgent> _hostsToAgents;

        private readonly AvlTree<GraphObject> _queuedObjects;

        private int _objectCount;

        #endregion

        #region Constructors

        public SiteGraph(string startPage, string baseDirectory)
        {
            DownloadedUrl = new HashSet<string>();
            
            HostLruQueue = new LinkedList<string>();
            _hostsToAgents = new Dictionary<string, WebAgent>();

            _queuedObjects = new AvlTree<GraphObject>(TaskCompare);

            BaseDirectory = baseDirectory;
            string prefix, hostName, path;
            startPage.UrlToHostName(out prefix, out hostName, out path);
            StartHost = hostName;
            var page = new MySocketDownloader { Url = startPage, Owner=this };
            AddObject(page);

            RootObject = page;
        }

        #endregion

        #region Properties

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

        public string BaseDirectory { get; private set; }

        public string StartHost { get; private set; }

        public HashSet<string> DownloadedUrl { get; private set; }

        /// <summary>
        ///  Lookup table from host to agent 
        /// </summary>
        /// <remarks>
        ///  agents are only used for socket connection
        /// </remarks>
        public IReadOnlyDictionary<string, WebAgent> HostsToAgents
        {
            get
            {
                return _hostsToAgents;
            }
        }

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

                var action = obj as BaseAction;
                if (action != null)
                {
                    await action.Perform();
                }
                else
                {
                    var node = obj as BaseNode;
                    if (node != null)
                    {
                        await node.Analyze();
                    }
                }

                if (ObjectProcessed != null)
                {
                    ObjectProcessed(this, new ObjectProcessedEventArgs {Object = obj});
                }
            }
        }

        public WebAgent GetOrCreateWebAgent(string hostName)
        {
            WebAgent agent;
            if (!_hostsToAgents.TryGetValue(hostName, out agent))
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

            var xHasUrl = x as IHasUrl;
            var yHasUrl = y as IHasUrl;

            if (xHasUrl != null && yHasUrl != null)
            {
                var xUrl = xHasUrl.Url;
                var yUrl = yHasUrl.Url;

                var comp = StartPage.CompareUrlDistances(xUrl, yUrl);

                if (comp != 0)
                {
                    return comp;
                }

                string xPrefix, xHostName, yPrefix, yHostName, dummy;
                xUrl.UrlToHostName(out xPrefix, out xHostName, out dummy);
                yUrl.UrlToHostName(out yPrefix, out yHostName, out dummy);

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
            if (yHasUrl != null)
            {
                return -1;
            }

            if (xHasUrl != null)
            {
                return 1;
            }

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
