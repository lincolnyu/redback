using System.Threading.Tasks;

namespace Redback.WebGraph.Actions
{
    public abstract class BaseDownloader : BaseAction, IHasUrl
    {
        #region Properties

        public virtual string Url { get; set; }

        #endregion

        #region Methods

        public abstract Task SaveAsync(string content);

        #endregion
    }
}
