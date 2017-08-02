using Redback.Connections;

namespace Redback.WebGraph
{
    public interface ISiteGraph
    {
        void AddObject(GraphObject gobj);
        bool HasDownloaded(string link);
        void SetHasDownloaded(string link);
    }

    public interface ISiteGraph<out TWebAgent> where TWebAgent : IWebAgent
    {
        TWebAgent GetOrCreateWebAgent(string hostName);
    }
}
