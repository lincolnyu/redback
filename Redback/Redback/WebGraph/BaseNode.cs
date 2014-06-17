using System.Collections.Generic;
using System.Threading.Tasks;

namespace Redback.WebGraph
{
    /// <summary>
    ///  Base class of any entity that analyzes a web object at the specified location
    /// </summary>
    public abstract class BaseNode : GraphObject, IHasUrl
    {
        #region Constructors

        protected BaseNode()
        {
            Actions = new List<BaseAction>();
        }

        #endregion

        #region Properties

        /// <summary>
        ///  The URL to the object
        /// </summary>
        public virtual string Url { get; set; }

        /// <summary>
        ///  The directory that keeps this downloaded object
        /// </summary>
        public virtual string LocalDirectory { get; set; }

        /// <summary>
        ///  The name of the file for keeping the downloaded object if any
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        ///  The action that leads to this node
        /// </summary>
        public BaseAction InducingAction { get; set; }
        
        /// <summary>
        ///  All the actions involved in the node as the result of analysis
        ///  It is the engine (current task) that picks the action and runs it
        /// </summary>
        public IList<BaseAction> Actions { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        ///  Initiates the analysis from this node
        /// </summary>
        public abstract Task Analyze();

        #endregion
    }
}
