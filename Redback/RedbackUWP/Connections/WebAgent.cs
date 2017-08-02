using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Redback.Helpers;
using Windows.Storage.Streams;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace Redback.Connections
{
    public class WebAgent : IWebAgent
    {
        #region Delegates

        public delegate Task GetDataCallback(DataReader reader, uint acutalLength);

        #endregion

        #region Nested types

        public class HttpResponse
        {
            #region Properties

            public uint ContentLength { get; set; }

            public string ContentType { get; set; }

            public string PageContent { get; set; }

            public byte[] DataContent { get; set; }

            public string Location { get; set; }

            public string SetCookie { get; set; }

            public string CacheControl { get; set; }

            public string Expires { get; set; }

            public string Pragma { get; set; }

            public string XWhom { get; set; }

            public string P3P { get; set; }

            public bool IsPage
            {
                get
                {
                    return PageContent != null;
                }
            }

            public bool IsSession
            {
                get
                {
                    return Location != null;
                }
            }

            #endregion
        }

        #endregion

        #region Fields

        private StreamSocket _socket;

        private DataWriter _writer;

        private DataReader _reader;

        #endregion

        #region Constructors

        public WebAgent(string hostDisplayName)
        {
            HostDisplayName = hostDisplayName;
        }

        #endregion

        #region Properties

        public string HostDisplayName
        {
            get; private set;
        }
        
        #endregion

        #region Methods

        public async Task<bool> SocketConnect()
        {
            HostName hostName;
            try
            {
                hostName = new HostName(HostDisplayName);
            }
            catch (ArgumentException)
            {
                return false; // TODO log the error message
            }

            _socket = new StreamSocket();

            var localHostNames = NetworkInformation.GetHostNames();
            var successful = false;
            foreach (var localHostName in localHostNames)
            {
                if (localHostName.IPInformation != null)
                {
                    var adapter = localHostName.IPInformation.NetworkAdapter;

                    try
                    {
                        await _socket.ConnectAsync(hostName, "80", SocketProtectionLevel.PlainSocket, adapter);
                        // TODO another version works on a binding to a specific adapter, we should not need that for now
                        successful = true;
                        break;
                    }
                    catch (Exception)
                    {
                        // If this is an unknown status it means that the error is fatal and retry will likely fail.
                        //if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                        //{
                        //    throw;
                        //}
                        _socket = new StreamSocket();
                    }
                }
            }

            _writer = null;
            _reader = null;

            return successful;
        }

        public async Task<bool> SendRequest(string request)
        {
            var successful = false;
            for (var i = 0; i < 2 && !successful; i++)
            {
                if (_writer == null)
                {
                    _writer = new DataWriter(_socket.OutputStream);
                }

                // writes the request directly (without leading string size)
                _writer.WriteString(request);

                // Write the locally buffered data to the network.
                try
                {
                    await _writer.StoreAsync();
                    successful = true;
                }
                catch (Exception exception)
                {
                    // If this is an unknown status it means that the error if fatal and retry will likely fail.
                    if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                    {
                        throw;
                    }
                    successful = false;
                }
                if (!successful)
                {
                    await SocketConnect(); // reconnect
                }
            }
            return successful;
        }

        public async Task<HttpResponse> GetResponse()
        {
            if (_reader == null)
            {
                _reader = new DataReader(_socket.InputStream);
            }

            try
            {
                 // parse the http response
                var sbHeader = new StringBuilder();
                while (true)
                {
                    await _reader.LoadAsync(1);
                    var s = _reader.ReadString(1);
                    sbHeader.Append(s);

                    if (sbHeader.Length > 4 && sbHeader.ToString().EndsWith("\r\n\r\n"))
                    {
                        break;
                    }
                }

                var header = sbHeader.ToString();

                var transferEncoding = header.IndexOf("Transfer-Encoding", StringComparison.Ordinal);

                // TODO make sure 'Accept-Ranges' is byte?

                var contentType = header.GetParameter("Content-Type:");
                var contentEncoding = header.GetParameter("Content-Encoding:");

                byte[] data;
                uint contentLength;
                if (transferEncoding >= 0)
                {
                    var bufs = new LinkedList<byte[]>();
                    contentLength = 0;
                    while (true)
                    {
                        var buf = await ReadChunk();
                        if (buf.Length > 0)
                        {
                            bufs.AddLast(buf);
                            contentLength += (uint)buf.Length;
                        }
                        else
                        {
                            break;
                        }
                    }
                    data = new byte[contentLength];
                    var i = 0;
                    foreach (var buf in bufs)
                    {
                        buf.CopyTo(data, i);
                        i += buf.Length;
                    }
                }
                else
                {
                    var sContentLength = header.GetParameter("Content-Length:");
                    uint.TryParse(sContentLength, out contentLength);

                    data = new byte[contentLength];

                    if (contentLength > 0)
                    {
                    await _reader.LoadAsync(contentLength);
                    _reader.ReadBytes(data);
                    }
                    else
                    {
                        // session
                        var cookie = header.GetParameter("Set-Cookie:");
                        var location = header.GetParameter("Location:");
                        var cache = header.GetParameter("Cache-Control:");
                        var expires = header.GetParameter("Expires:");
                        var pragma = header.GetParameter("Pragma:");
                        var xwhom = header.GetParameter("X-Whom:");
                        var p3p = header.GetParameter("P3P:");

                        var sessionResponse = new HttpResponse
                        {
                            ContentType = contentType,
                            ContentLength = 0,
                            SetCookie = cookie,
                            Location = location,
                            CacheControl = cache,
                            Expires = expires,
                            Pragma = pragma,
                            XWhom = xwhom,
                            P3P = p3p
                        };
                        return sessionResponse;
                    }
                }

                var response = new HttpResponse
                {
                    ContentType = contentType,
                    ContentLength = contentLength,
                };

                if (contentEncoding.Contains("gzip"))
                {
                    const int decompBufferSize = 4096;
                    var decompBuffer = new byte[decompBufferSize];

                    using (var dataStream = new MemoryStream(data))
                    {
                        using (var gzip = new GZipStream(dataStream, CompressionMode.Decompress))
                        {
                            using (var deflated = new MemoryStream())
                            {
                                int size;
                                do
                                {
                                    size = gzip.Read(decompBuffer, 0, decompBufferSize);
                                    deflated.Write(decompBuffer, 0, size);
                                } while (size > 0);
                                data = deflated.ToArray();  // update data with the deflated data
                            }
                        }
                    }
                }

                if (contentType.Contains("text/html"))
                {
                    var sbPayload = new StringBuilder();
                    if (contentType.Contains("utf8"))
                    {
                        var enc = new UTF8Encoding();
                        var dec = enc.GetDecoder();
                        var charCount = dec.GetCharCount(data, 0, data.Length);
                        var chars = new char[charCount];
                        dec.GetChars(data, 0, data.Length, chars, 0);
                        foreach (var c in chars)
                        {
                            sbPayload.Append(c);
                        }
                    }
                    else
                    {
                        // use trivial decoding
                        // TODO to support other decoding methods?
                        foreach (var b in data)
                        {
                            var c = (char) b;
                            sbPayload.Append(c);
                        }
                    }
                    response.PageContent = sbPayload.ToString();
                    response.DataContent = null;
                }
                else
                {
                    response.DataContent = data;
                    response.PageContent = null;
                }
                return response;
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }
            await SocketConnect(); // reconnect
            return null;
        }


        private async Task<byte[]> ReadChunk()
        {
            var sb = new StringBuilder();
            while (true)
            {
                await _reader.LoadAsync(1);
                var s = _reader.ReadString(1);
                sb.Append(s);

                if (sb.Length > 4 && sb.ToString().EndsWith("\r\n"))
                {
                    break;
                }
            }

            var slen = sb.ToString();
            uint len;
            if (!uint.TryParse(slen, NumberStyles.HexNumber, null, out len))
            {
                // fatal error
                throw new Exception("Bad chunk datsa");
            }

            if (len == 0)
            {
                return new byte[0];
            }

            var bytes = new byte[len];

            await _reader.LoadAsync(len);
            _reader.ReadBytes(bytes);

            // there is still a "\r\n" to consume?
            await _reader.LoadAsync(2);
            var sTail = _reader.ReadString(2);
            if (sTail != "\r\n")
            {
                // log the error
            }

            return bytes;
        }

        #endregion
    }
}
