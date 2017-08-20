using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Redback.WebGraph.Nodes;
using Redback.Helpers;

namespace Redback.WebGraph.Actions
{
    public class HttpDownloader : FileDownloader
    {
        private class RequestState
        {
            public HttpWebResponse Response;
            public Stream ResponseStream;
            public HttpWebRequest Request;
            public CancellationTokenSource CompleteByCancel;
            public Exception Exception;
            public Exception Exception2;
        }

        private Uri _actualUrl;
        private RequestState _requestState;

        public override async Task<string> GetActualUrl()
        {
            await DownloadIfNot();
            return _actualUrl?.ToString() ?? await base.GetActualUrl();
        }


        private async Task DownloadIfNot()
        {
            if (_requestState != null)
            {
                return;
            }
            _requestState = new RequestState();
            try
            {
                var url = Url.MakeHttpIfCan(); // This is essential
                // TODO https?
                var request = (HttpWebRequest)WebRequest.Create(url);
                var source = new CancellationTokenSource();

                _requestState.Request = request;
                _requestState.CompleteByCancel = source;

                var result = request.BeginGetResponse(ResponseCallback, _requestState);

                var cancelToken = source.Token;
                await Task.Delay(TimeSpan.FromMilliseconds(-1), cancelToken);
            }
            catch (TaskCanceledException)
            {
                if (_requestState.Exception == null)
                {
                    _actualUrl = _requestState.Response.ResponseUri;
                    if (_actualUrl.ToString().UrlToFilePath(out string dir, out string filename, UrlHelper.ValidateFileName))
                    {
                        LocalDirectory = Path.Combine(((ICommonGraph)Owner).BaseDirectory, dir);
                        LocalFileName = filename;
                    }
                }
                else
                {
                    // TODO log requestState.Exception or something
                }
            }
            catch (WebException e)
            {
                _requestState.Exception2 = e;
            }
            catch (Exception e)
            {
                _requestState.Exception2 = e;
            }
        }

        public override async Task Perform()
        {
            await DownloadIfNot();
            if (_requestState.Exception != null || _requestState.Exception2 != null)
            {
                return;
            }
            var contentType = _requestState.Response.ContentType;
            if (contentType.Contains("text/html"))
            {
                Encoding enc;
                if (contentType.Contains("utf8"))
                {
                    enc = new UTF8Encoding();
                }
                else
                {
                    // just use plain ASCII
                    enc = new ASCIIEncoding();
                }
                // TODO encoding
                using (var sr = new StreamReader(_requestState.ResponseStream, enc))
                {
                    System.Diagnostics.Debug.Assert(_actualUrl != null);
                    var content = sr.ReadToEnd();
                    TargetNode = new SimplePageParser((owner, source, level, url) =>
                        new HttpDownloader
                        {
                            Owner = owner,
                            SourceNode = source,
                            Level = level,
                            Url = url,
                        })
                    {
                        Owner = Owner,
                        Url = _actualUrl.ToString(),
                        InducingAction = this,
                        Level = Level + 1,
                        Page = content
                    };
                    Owner.AddObject(TargetNode);
#if !NO_WRITE_ORIG_PAGE
                    await SaveAsync(content);
#endif
                }
            }
            else
            {
                var len = _requestState.Response.ContentLength;
                if (len >= 0)
                {
                    using (var br = new BinaryReader(_requestState.ResponseStream))
                    {
                        const int maxBufLen = 16 * 1024;
                        var buflen = (int)Math.Min(len, maxBufLen);
                        await SaveDataAsync(() =>
                        {
                            var buf = br.ReadBytes(buflen);
                            return buf;
                        });
                    }
                }
            }
        }

        private void ResponseCallback(IAsyncResult asyncResult)
        {
            var requestState = (RequestState)asyncResult.AsyncState;
            try
            {
                var request = requestState.Request;
                requestState.Response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                requestState.ResponseStream = requestState.Response.GetResponseStream();
            }
            catch (WebException e)
            {
                requestState.Exception = e;
            }
            catch (Exception e)
            {
                requestState.Exception = e;
            }
            finally
            {
                requestState.CompleteByCancel.Cancel();
            }
        }
    }
}
