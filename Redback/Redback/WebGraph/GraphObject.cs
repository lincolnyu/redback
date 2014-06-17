namespace Redback.WebGraph
{
    public class GraphObject
    {
        #region Properties

        public SiteGraph Owner
        {
            get; set;
        }

        /// <summary>
        ///  Number of steps from the starting node (0 for starting node, 1 for first level actions, 2 for first level nodes ...)
        /// </summary>
        public int Level
        {
            get; set;
        }

        #endregion
    }
}
