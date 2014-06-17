using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Redback.Connections;
using Redback.Helpers;
using Redback.WebGraph.Nodes;

namespace Redback.WebGraph.Actions
{
    public class MySocketDownloader : BaseDownloader
    {
        #region Fields

        private bool _headerRead;
        private bool _isPage;

        private StringBuilder _sbPage;

        private Stream _outputStream;

        #endregion

        #region Properties

        public uint TotalBytes { get; private set; }

        public long ReadBytes { get; private set; }

        #endregion

        #region Methods

        #region BaseAction members

        public override async void Perform()
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

                _headerRead = false;
                ReadBytes = 0;
                _sbPage = new StringBuilder();
                await agent.GetResponse(GetData);
                if (_isPage)
                {
                    var page = _sbPage.ToString();
                    TargetNode = new SimplePageParser
                    {
                        Owner = Owner,
                        InducingAction = this,
                        Level = Level + 1,
                        Page = page
                    };
#if DEBUG && WRITE_ORIG_PAGE
                    using (var sw = new StreamWriter(_outputStream))
                    {
                        sw.Write(page);
                    }
                    _outputStream.Flush();
#endif
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task GetData(DataReader reader, uint actualLength)
        {
            // Assume the data is sent by section
            if (!_headerRead)
            {
                var s = reader.ReadString(actualLength);
                if (s.EndsWith("\r\n\r\n"))
                {
                    _headerRead = true;
                }
                var iContentType = s.IndexOf("Content-Type:", StringComparison.OrdinalIgnoreCase);
                if (iContentType >= 0)
                {
                    var start = iContentType + "Content-Type:".Length;
                    var end = s.IndexOf('\r', start);
                    var mime = s.Substring(start, end - start).Trim();
                    if (mime.Contains("text/html"))
                    {
                        _isPage = true;
                    }
                    
                    if (LocalDirectory != null && LocalFileName != null && _outputStream == null)
                    {
                        var folder = await LocalDirectory.GetOrCreateFolderAsync();
                        var file = await folder.GetOrCreateFileAsync(LocalFileName);
                        _outputStream = await file.OpenStreamForWriteAsync();
                    }
                }
                var iContentLength = s.IndexOf("Content-Length:", StringComparison.OrdinalIgnoreCase);
                if (iContentLength >= 0)
                {
                    var start = iContentLength + "Content-Length:".Length;
                    var end = s.IndexOf('\r', start);
                    var slen = s.Substring(start, end - start).Trim();
                    TotalBytes = uint.Parse(slen);
                }

                if (_isPage)
                {
                    var contentStart = s.IndexOf("\r\n\r\n") + "\r\n\r\n".Length;
                    _sbPage.Append(s.Substring(contentStart));
                }
                // Not expecting data succeeding header for non-page
            }
            else
            {
                if (_isPage)
                {
                    var s = reader.ReadString(actualLength);
                    _sbPage.Append(s);
                }

                if (_outputStream != null)
                {
                    var buffer = new byte[actualLength];
                    reader.ReadBytes(buffer);
                    await _outputStream.WriteAsync(buffer, 0, (int)actualLength);
                    ReadBytes += actualLength;
                }
            }
        }

        #endregion
    }
}
