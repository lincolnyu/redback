using System;
using System.Collections.Generic;
using QSharp.Scheme.Classical.Trees;
using Redback.Helpers;
using System.Threading.Tasks;
using Redback.UrlManagement;

namespace Redback.WebGraph
{
    public class BaseSiteGraph : ISiteGraph, IUrlRegulator
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

        private readonly AvlTree<GraphObject> _queuedObjects;

        private int _objectCount;

        private readonly HostRegulator _hostRegulator = new HostRegulator();

        #endregion

        #region Constructors
        
        protected BaseSiteGraph()
        {
            HostLruQueue = new LinkedList<string>();
            _queuedObjects = new AvlTree<GraphObject>(TaskCompare);
        }

        #endregion

        #region Properties

        public string StartHost { get; protected set; }

        public GraphObject RootObject { get; protected set; }

        public string StartPage
        {
            get
            {
                return ((IHasUrl)RootObject).Url;
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
                    var nodet = (AvlTreeWorker.INode<GraphObject>)node;
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

        #region ISiteGraph

        public void AddObject(GraphObject obj)
        {
            _queuedObjects.Insert(obj);
            _objectCount++;
        }

        #endregion

        #region UrlRegulator members

        public string RegulateUrl(string originalUrl)
            => _hostRegulator.RegulateUrl(originalUrl);

        #endregion

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

        public async Task Run()
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
                    else if (obj is BaseNode node)
                    {
                        await node.Analyze();
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
            System.Diagnostics.Debug.WriteLine("All graph objects processed, crawling completed");
        }

        protected virtual int TaskCompare(GraphObject x, GraphObject y)
        {
            var r = BaseTaskCompare(x, y);

            // NOTE x is given higher priority the tree node should appear on the left in searching
            if (r == 0) return -1; // doesn't matter

            return r;
        }

        protected int BaseTaskCompare(GraphObject x, GraphObject y)
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

                return StartPage.CompareUrlDistances(xUrl, yUrl);
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

            return 0;
        }

        private bool IsOnHostOrReferencedByHostPage(GraphObject x)
        {
            string dummy, hostName;
            if (x is IHasUrl hasUrl)
            {
                hasUrl.Url.UrlToHostName(out dummy, out hostName, out dummy);
                if (hostName == StartHost)
                {
                    return true;
                }
            }

            var action = x as BaseAction;
            if (action == null)
            {
                return false;
            }

            action.SourceNode.Url.UrlToHostName(out dummy, out hostName, out dummy);
            return hostName == StartHost;
        }

        #endregion
    }
}
