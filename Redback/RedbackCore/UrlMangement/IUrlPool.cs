using Redback.WebGraph;

namespace Redback.UrlManagement
{
    public interface IUrlPool
    {
        bool IsInThePool(string link);

        bool IsDownloaded(string link, out string target);

        void Subscribe(string original, IActualUrlReceiver receiver);

        void SetActualUrl(object source, string original, string target);
    }
}
