namespace Redback.WebGraph
{
    public interface ISiteGraph
    {
        void AddObject(GraphObject gobj);
        bool HasDownloaded(string link);
        void SetHasDownloaded(string link);
    }
}
