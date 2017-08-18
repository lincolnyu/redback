using System;
using System.Text;
using System.Threading.Tasks;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Nodes;

namespace Redback.WebGraph.Actions
{
    public class SocketDownloader : FileDownloader
    {
        #region Types

        private enum PageResults
        {
            Successful,
            IsHttps,
            Failed
        }

        #endregion

        #region Properties

        public bool UseReferrer { get; set; }

        #endregion

        #region Methods

        #region BaseAction members

        public override async Task Perform()
        {
            //TODO we may not be able to do https now
            Url.UrlToHostName(out string prefix, out string hostName, out string path);
            var owner = (SocketSiteGraph)Owner;
            var agent = owner.GetOrCreateWebAgent(hostName);
            var connected = await agent.SocketConnect();
            var tryHttps = !connected;
            if (connected)
            {
                var res = await AcquirePage(agent, hostName, path);
                tryHttps = res == PageResults.IsHttps;
            }
            if (tryHttps)
            {
                Url.UrlToHostName(out prefix, out hostName, out path);
                connected = await agent.SocketConnect(true);
                await AcquirePage(agent, hostName, path, true);
            }
            // TODO report error otherwise?
        }

        #endregion

        private async Task<PageResults> AcquirePage(SocketWebAgent agent, string hostName, string path, bool isHttps = false)
        {
            var sbRequest = new StringBuilder();

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            sbRequest.AddParameterFormat(@"GET {0} HTTP/1.1", path);
            // TODO accept type as per a priori knowledge of the data type (inferred from metadata/extension etc)
            // TODO like application/javascript for js
            // TODO      text/css for css
            // TODO      image/png, image/svg+xml, image/* for images
            // NOTE however as far as */* is included, the it's supposed to be able to accept anything
            sbRequest.AddParameter(@"Accept: text/html, application/xhtml+xml, */*");
            if (UseReferrer)
            {
                sbRequest.AddParameterFormat(@"Referer: {0}://{1}/",
                    isHttps ? "https" : "http", hostName);
            }
            sbRequest.AddParameter(@"Accept-Language: en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");
            sbRequest.AddParameter(@"User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");
            sbRequest.AddParameter(@"Accept-Encoding: gzip, deflate");
            sbRequest.AddParameterFormat(@"Host: {0}", hostName);
            sbRequest.AddParameter(@"DNT: 1");
            sbRequest.AddParameter(@"Connection: Keep-Alive");
            sbRequest.ConcludeRequest();

            try
            {
                var request = sbRequest.ToString();
                var r = await agent.SendRequest(request);
                if (!r)
                {
                    return PageResults.Failed;
                }

                var response = await agent.GetResponse();

                if (!string.IsNullOrWhiteSpace(response.Location))
                {
                    // Update to the most accurate URL
                    if (!Url.IsHttps() && response.Location.IsHttps())
                    {
                        Url = response.Location;
                        return PageResults.IsHttps;
                    }
                    Url = response.Location;
                }

                if (response.IsSession)
                {
                    return await ProcessSessionalPage(agent, hostName, path, response) ? PageResults.Successful : PageResults.Failed;
                }

                return await ProcessPageResponse(response) ? PageResults.Successful : PageResults.Failed;
            }
            catch
            {
                return PageResults.Failed;
            }
        }

        private async Task<bool> ProcessPageResponse(SocketWebAgent.HttpResponse response)
        {
            if (response.IsPage)
            {
                TargetNode = new SimplePageParser((owner, source, level, url, localDir, localFile) =>
                        new SocketDownloader
                        {
                            Owner = owner,
                            SourceNode = source,
                            Level = level,
                            Url = url,
                            LocalDirectory = localDir,
                            LocalFileName = localFile,
                            UseReferrer = UseReferrer
                        })
                {
                    Owner = Owner,
                    Url = Url,
                    InducingAction = this,
                    Level = Level + 1,
                    Page = response.PageContent
                };
                Owner.AddObject(TargetNode);
#if !NO_WRITE_ORIG_PAGE
                await SaveAsync(response.PageContent);
#endif
            }
            else if (response.IsSession)
            {
                return false;
            }
            else
            {
                // NOTE this is to save non page data which has no chance to save otherwise
                // NOTE page data is supposed to be saved if wanted by the node (parser)
                await SaveDataAsync(response.DataContent);
            }
            return true;
        }

        private async Task<bool> ProcessSessionalPage(SocketWebAgent agent, string hostName, string path, SocketWebAgent.HttpResponse response)
        {
            var recursive = 0;
            const int maxAttempt = 5;
            while (recursive < maxAttempt)
            {
                var location = response.Location;
                var token = GetTokenFromUrlInResponse(location);
                var sbRequest = new StringBuilder();
                sbRequest.AddParameterFormat(@"GET /?responseToken={0} HTTP/1.1", token);
                sbRequest.AddParameter(@"Accept: text/html, application/xhtml+xml, */*");
                sbRequest.AddParameter(@"Accept-Language: en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");
                sbRequest.AddParameter(@"User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");
                sbRequest.AddParameter(@"Accept-Encoding: gzip, deflate");
                sbRequest.AddParameter(@"DNT: 1");
                // NOTE host name remains the same
                sbRequest.AddParameterFormat(@"Host: {0}", hostName);
                // TODO we should check response to see if cookie is enabled. Here we are being slack and assuming it is
                sbRequest.AddParameter(@"Connection: Keep-Alive");
                sbRequest.AddParameter(@"Cookie: test=1");
                sbRequest.ConcludeRequest();

                var request = sbRequest.ToString();
                var r = await agent.SendRequest(request);
                if (!r)
                {
                    return false;
                }

                response = await agent.GetResponse();

                var cookie = GetCookieFromResponse(response.SetCookie);

                if (cookie == null)
                {
                    if (!response.IsSession)
                    {
                        return false;
                    }
                    recursive = recursive + 1;
                    continue;
                }

                sbRequest.Clear();
                sbRequest.AddParameterFormat(@"GET {0} HTTP/1.1", path);
                sbRequest.AddParameter(@"Accept: text/html, application/xhtml+xml, */*");
                sbRequest.AddParameter(@"Accept-Language: en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");
                sbRequest.AddParameter(@"User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");
                sbRequest.AddParameter(@"Accept-Encoding: gzip,deflate");
                sbRequest.AddParameterFormat(@"Cookie: test=1; slave={0}", cookie);
                sbRequest.AddParameterFormat(@"Host: {0}", hostName);
                sbRequest.AddParameter(@"Connection: Keep-Alive");
                sbRequest.AddParameter(@"DNT: 1");
                Owner.AddObject(TargetNode);

                request = sbRequest.ToString();
                r = await agent.SendRequest(request);
                if (!r)
                {
                    return false;
                }

                var pageResponse = await agent.GetResponse();
                return await ProcessPageResponse(pageResponse);
            }
            return false;
        }

        private static string GetTokenFromUrlInResponse(string location)
        {
            var eqSign = location.LastIndexOf('=');
            if (eqSign < 0)
            {
                return "";
            }
            return location.Substring(eqSign + 1);
        }

        private static string GetCookieFromResponse(string cookie)
        {
            if (cookie == null)
            {
                return null;
            }
            var start = cookie.IndexOf("slave=", System.StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }
            start += "slave=".Length;
            var end = cookie.IndexOf(";", start, System.StringComparison.Ordinal);
            if (end < 0)
            {
                end = cookie.Length;
            }
            return cookie.Substring(start, end - start);
        }

        #endregion
    }
}
