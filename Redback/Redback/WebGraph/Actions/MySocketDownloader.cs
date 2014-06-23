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
            sbRequest.AppendFormat(@"GET {0} HTTP/1.1" + HttpHelper.NewLine, path);
            sbRequest.Append(@"Accept:  text/html, application/xhtml+xml, */*" + HttpHelper.NewLine);
            sbRequest.AppendFormat(@"Referer: http://{0}/" + HttpHelper.NewLine, hostName);
            sbRequest.Append(@"Accept-Language:  en-AU,en;q=0.8,zh-Hans-CN;q=0.5,zh-Hans;q=0.3" + HttpHelper.NewLine);
            sbRequest.Append(@"UserAgent:  Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko" + HttpHelper.NewLine);
            sbRequest.Append(@"Accept-Encoding: gzip,deflate" + HttpHelper.NewLine);
            sbRequest.AppendFormat(@"Host: {0}" + HttpHelper.NewLine, hostName);
            sbRequest.Append(@"DNT: 1" + HttpHelper.NewLine);
            sbRequest.Append(@"Connection: keep-alive" + HttpHelper.NewLine);
            sbRequest.Append(HttpHelper.NewLine);

            try
            {
                var request = sbRequest.ToString();
                var r = await agent.SendRequest(request);
                if (!r)
                {
                    return false;
                }

                var response = await agent.GetResponse();

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
                else
                {
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
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
