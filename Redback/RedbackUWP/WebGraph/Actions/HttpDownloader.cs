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
            public WebException Exception;
        }

        public override async Task Perform()
        {
            var requestState = new RequestState();
            try
            {
                var url = Url.MakeHttpIfCan();
                var request = (HttpWebRequest)WebRequest.Create(url);
                var source = new CancellationTokenSource();

                requestState.Request = request;
                requestState.CompleteByCancel = source;

                var result = request.BeginGetResponse(ResponseCallback, requestState);

                var cancelToken = source.Token;
                await Task.Delay(TimeSpan.FromMilliseconds(-1), cancelToken);
            }
            catch (TaskCanceledException)
            {
                if (requestState.Exception == null)
                {
                    var contentType = requestState.Response.ContentType;
                    if (contentType == "text/html")
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
                        using (var sr = new StreamReader(requestState.ResponseStream, enc))
                        {
                            var content = sr.ReadToEnd();
                            TargetNode = new SimplePageParser((owner, source, level, url, localDir, localFile) =>
                                new HttpDownloader
                                {
                                    Owner = owner,
                                    SourceNode = source,
                                    Level = level,
                                    Url = url,
                                    LocalDirectory = localDir,
                                    LocalFileName = localFile
                                })
                            {
                                Owner = Owner,
                                Url = Url,
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
                        using (var br = new BinaryReader(requestState.ResponseStream))
                        {
                            var len = requestState.Response.ContentLength;
                            const int maxBufLen = 16*1024;
                            var buflen = (int)Math.Min(len, maxBufLen);

                            await SaveDataAsync(() =>
                            {
                                var buf = br.ReadBytes(buflen);
                                return buf;
                            });
                        }
                    }
                }
                else
                {
                    // TODO log requestState.Exception or something
                }
            }
            catch (WebException)
            {
                // TODO log error or something
            }
            catch (Exception)
            {
                // TODO log error or something
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
            finally
            {
                requestState.CompleteByCancel.Cancel();
            }
        }
    }
}
