using System.Threading.Tasks;

namespace Redback.WebGraph
{
    public abstract class BaseAction : GraphObject
    {
        #region Properties

        public BaseNode SourceNode { get; set; }

        public BaseNode TargetNode { get; set; }

        #endregion

        #region Methods

        public abstract Task Perform();

        #endregion
    }
}
