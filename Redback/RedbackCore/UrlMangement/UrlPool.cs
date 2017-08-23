using System.Collections.Generic;
using Redback.WebGraph;

namespace Redback.UrlManagement
{
    public class UrlPool : IUrlPool
    {
        private Dictionary<string, string> _downloaded = new Dictionary<string, string>();
        private Dictionary<string, HashSet<IActualUrlReceiver>> _temp 
            = new Dictionary<string, HashSet<IActualUrlReceiver>>();

        public void SetActualUrl(object source, string original, string target)
        {
            if (_temp.TryGetValue(original, out var receivers))
            {
                foreach (var receiver in receivers)
                {
                    receiver.ReportActualUrl(source);
                }
                _temp.Remove(original);
            }

            _downloaded[original] = target;
        }

        public void Subscribe(string original, IActualUrlReceiver receiver)
        {
            if (!_temp.TryGetValue(original, out var receivers))
            {
                receivers = new HashSet<IActualUrlReceiver>();
                _temp[original] = receivers;
            }
            if (receiver != null && !receivers.Contains(receiver))
            {
                receivers.Add(receiver);
            }
        }

        public bool IsDownloaded(string link, out string target)
            => _downloaded.TryGetValue(link, out target);

        public bool IsInThePool(string link)
            =>_downloaded.ContainsKey(link) || _temp.ContainsKey(link);
    }
}
