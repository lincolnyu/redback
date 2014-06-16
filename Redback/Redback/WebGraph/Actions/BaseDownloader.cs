namespace Redback.WebGraph.Actions
{
    public abstract class BaseDownloader : BaseAction, IHasUrl
    {
        #region Properties

        public virtual string Url { get; set; }

        /// <summary>
        ///  The directory that keeps this downloaded object
        /// </summary>
        public virtual string LocalDirectory { get; set; }

        /// <summary>
        ///  The name of the file for keeping the downloaded object if any
        /// </summary>
        public virtual string LocalFileName { get; set; }

        #endregion
    }
}
