using Redback.Helpers;
using System.Collections.Generic;

namespace Redback.UrlManagement
{
    public class HostRegulator : IUrlRegulator
    {
        private HashSet<string> _hosts = new HashSet<string>();

        public string RegulateUrl(string originalUrl)
        {
            var host = originalUrl.GetHost();
            string result = originalUrl;
            if (!_hosts.Contains(host))
            {
                if (host.StartsWith("www."))
                {
                    var stripped = host.Substring("www.".Length);
                    if (_hosts.Contains(stripped))
                    {
                        result = originalUrl.ReplaceHost(stripped);
                    }
                    else
                    {
                        _hosts.Add(host);
                    }
                }
                else
                {
                    var prefixed = "www." + host;
                    if (_hosts.Contains(prefixed))
                    {
                        result = originalUrl.ReplaceHost(prefixed);
                    }
                    else
                    {
                        _hosts.Add(host);
                    }
                }

            }
            
            return result;
        }
    }
}
