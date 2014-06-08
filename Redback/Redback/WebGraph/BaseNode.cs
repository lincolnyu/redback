namespace Redback.WebGraph
{
    /// <summary>
    ///  Base class of any entity that analyzes a web object at the specified location
    /// </summary>
    public abstract class BaseNode
    {
        public virtual string Url { get; set; }

        #region Methods

        public abstract void Analyze();

        #endregion
    }
}
