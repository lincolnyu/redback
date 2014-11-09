using System.IO;
using System.Text;
using System.Threading.Tasks;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Nodes;

namespace Redback.WebGraph.Actions
{
    public class MySocketDownloader : BaseDownloader
    {
        #region Methods

        #region BaseAction members

        public override async Task Perform()
        {
            string hostName;
            string path;
            string prefix;
            //TODO we may not be able to do https now
            Url.UrlToHostName(out prefix, out hostName, out path);
            var agent = Owner.GetOrCreateWebAgent(hostName);
            var connected = await agent.SocketConnect();
            if (connected)
            {
                await AcquirePage(agent, hostName, path);
            }
            // TODO report error otherwise?
        }

        #endregion

        private async Task<bool> AcquirePage(WebAgent agent, string hostName, string path)
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
            sbRequest.AddParameter(@"Accept:  text/html, application/xhtml+xml, */*");
            sbRequest.AddParameterFormat(@"Referer: http://{0}/", hostName);
            sbRequest.AddParameter(@"Accept-Language:  en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");
            sbRequest.AddParameter(@"UserAgent:  Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");
            sbRequest.AddParameter(@"Accept-Encoding: gzip,deflate");
            sbRequest.AddParameterFormat(@"Host: {0}", hostName);
            sbRequest.AddParameter(@"DNT: 1");
            sbRequest.AddParameter(@"Connection: keep-alive");
            sbRequest.ConcludeRequest();

            try
            {
                var request = sbRequest.ToString();
                var r = await agent.SendRequest(request);
                if (!r)
                {
                    return false;
                }

                var response = await agent.GetResponse();

                if (response.IsSession)
                {
                    return await ProcessSessionalPage(agent, hostName, path, response);
                }

                return await ProcessPageResponse(response);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ProcessPageResponse(WebAgent.HttpResponse response)
        {
            if (response.IsPage)
            {
                TargetNode = new SimplePageParser
                {
                    Owner = Owner,
                    Url = Url,
                    InducingAction = this,
                    Level = Level + 1,
                    Page = response.PageContent
                };
                Owner.AddObject(TargetNode);
#if !NO_WRITE_ORIG_PAGE
                var folder = await LocalDirectory.GetOrCreateFolderAsync();
                var file = await folder.CreateNewFileAsync(LocalFileName);
                using (var outputStream = await file.OpenStreamForWriteAsync())
                {
                    using (var sw = new StreamWriter(outputStream))
                    {
                        sw.Write(response.PageContent);
                    }
                }
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
                var folder = await LocalDirectory.GetOrCreateFolderAsync();
                var file = await folder.CreateNewFileAsync(LocalFileName);
                using (var outputStream = await file.OpenStreamForWriteAsync())
                {
                    await outputStream.WriteAsync(response.DataContent, 0, response.DataContent.Length);
                    await outputStream.FlushAsync();
                }
            }
            return true;
        }
        
        private async Task<bool> ProcessSessionalPage(WebAgent agent, string hostName, string path, WebAgent.HttpResponse response)
        {
            var location = response.Location;
            var token = GetTokenFromUrlInResponse(location);
            var sbRequest = new StringBuilder();
            sbRequest.AddParameterFormat(@"GET /?responseToken={0} HTTP/1.1", token);
            sbRequest.AddParameter(@"Accept:  text/html, application/xhtml+xml, */*");
            sbRequest.AddParameter(@"Accept-Language:  en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");
            sbRequest.AddParameter(@"UserAgent:  Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");
            sbRequest.AddParameter(@"Accept-Encoding: gzip,deflate");
            // NOTE host name remains the same
            sbRequest.AddParameterFormat(@"Host: {0}", hostName);
            // TODO we should check response to see if cookie is enabled. Here we are being slack and assuming it is
            sbRequest.AddParameter(@"Cookie: test=1");
            sbRequest.AddParameter(@"DNT: 1");
            sbRequest.AddParameter(@"Connection: keep-alive");
            sbRequest.ConcludeRequest();

            var request = sbRequest.ToString();
            var r = await agent.SendRequest(request);
            if (!r)
            {
                return false;
            }

            var cookieResponse = await agent.GetResponse();
            var cookie = GetCookieFromResponse(cookieResponse.SetCookie);

            sbRequest.Clear();
            sbRequest.AddParameterFormat(@"GET {0} HTTP/1.1", path);
            sbRequest.AddParameter(@"Accept:  text/html, application/xhtml+xml, */*");
            sbRequest.AddParameter(@"Accept-Language:  en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3");
            sbRequest.AddParameter(@"UserAgent:  Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko");
            sbRequest.AddParameter(@"Accept-Encoding: gzip,deflate");
            sbRequest.AddParameterFormat(@"Cookie: test=1; slave={0}", cookie);
            sbRequest.AddParameterFormat(@"Host: {0}", hostName);
            sbRequest.AddParameter(@"DNT: 1");
            sbRequest.AddParameter(@"Connection: keep-alive");
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
