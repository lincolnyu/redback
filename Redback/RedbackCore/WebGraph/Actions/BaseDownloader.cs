using System.Threading.Tasks;

namespace Redback.WebGraph.Actions
{
    public abstract class BaseDownloader : BaseAction, IHasUrl
    {
        #region Properties

        /// <summary>
        ///  The user requested original URL
        /// </summary>
        public virtual string Url { get; set; }

        #endregion

        #region Methods

        public abstract Task SaveAsync(string content);

        /// <summary>
        ///  Predownload to get the actual URL
        /// </summary>
        /// <returns></returns>
        public virtual async Task<string> GetActualUrl() => Url;

        #endregion
    }
}
