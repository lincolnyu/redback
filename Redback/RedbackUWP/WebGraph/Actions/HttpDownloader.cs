using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
            public WebException WebError;
        }

        public override async Task Perform()
        {
            var requestState = new RequestState();
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Url);
                var source = new CancellationTokenSource();

                requestState.Request = request;
                requestState.CompleteByCancel = source;

                var result = request.BeginGetResponse(ResponseCallback, requestState);

                var cancelToken = source.Token;
                await Task.Delay(TimeSpan.FromMilliseconds(-1), cancelToken);
            }
            catch (TaskCanceledException)
            {
                // complete
                if (requestState.WebError == null)
                {
                    // TODO ...
                }
                else
                {

                }
            }
            catch (WebException e)
            {

            }
            catch (Exception e)
            {

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
                requestState.WebError = e;
            }
            finally
            {
                requestState.CompleteByCancel.Cancel();
            }
        }
    }
}
