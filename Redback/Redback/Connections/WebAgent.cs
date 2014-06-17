using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Redback.Connections
{
    public class WebAgent
    {
        #region Delegates

        public delegate Task GetDataCallback(DataReader reader, uint acutalLength);

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
                    catch (Exception exception)
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

            return successful;
        }

        public async Task<bool> SendRequest(string request)
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
                return true;
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
                return false;
            }
        }

        public async Task<string> GetStringResponse()
        {
            if (_reader == null)
            {
                _reader = new DataReader(_socket.InputStream);
            }

            var sbResponse = new StringBuilder();

            try
            {
                while (true)
                {
                    // Read first 4 bytes (length of the subsequent string).
                    var sizeFieldCount = await _reader.LoadAsync(sizeof (uint));
                    if (sizeFieldCount != sizeof (uint))
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        break;
                    }

                    // Read the string.
                    var stringLength = _reader.ReadUInt32();
                    var actualStringLength = await _reader.LoadAsync(stringLength);
                    if (stringLength != actualStringLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        break;
                    }

                    var s = _reader.ReadString(actualStringLength);
                    sbResponse.Append(s);
                }
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }
            return sbResponse.ToString();
        }

        public async Task GetResponse(GetDataCallback getData)
        {
            if (_reader == null)
            {
                _reader = new DataReader(_socket.InputStream);
            }

            try
            {
                while (true)
                {
                    // Read first 4 bytes (length of the subsequent data).
                    var sizeFieldCount = await _reader.LoadAsync(sizeof (uint));
                    if (sizeFieldCount != sizeof (uint))
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        break;
                    }

                    // Read the data.
                    var dataLength = _reader.ReadUInt32();
                    var actualLength = await _reader.LoadAsync(dataLength);

                    await getData(_reader, actualLength);

                    if (dataLength != actualLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }
        }

        #endregion
    }
}
