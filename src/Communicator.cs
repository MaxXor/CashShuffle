using System;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashShuffle.Messages;
using Google.Protobuf;

namespace CashShuffle
{
    public class Communicator
    {
        private static readonly byte[] StopSymbol = new byte[] { 226, 143, 142 }; // this represents the character ⏎
        private JsonFormatter _jsonFormatter;
        private JsonParser _jsonParser;
        private SslStream _stream;
        private byte[] _buffer;
        private CancellationToken _shutdownRequested;

        public event ReceivedEventHandler Received;
        public delegate void ReceivedEventHandler(Communicator sender, Packet packet);
        public event ClosedEventHandler Closed;
        public delegate void ClosedEventHandler(Communicator sender);

        public Communicator(SslStream stream, CancellationToken shutdownRequested)
        {
            this._stream = stream;
            this._buffer = new byte[1024 * 32];
            this._jsonFormatter = new JsonFormatter(new JsonFormatter.Settings(false));
            this._jsonParser = new JsonParser(new JsonParser.Settings(3));
            this._shutdownRequested = shutdownRequested;
            this._shutdownRequested.Register(() => Close());
        }

        public async Task SendPacketAsync(Packet packet)
        {
            string packetStr = _jsonFormatter.Format(packet);
            byte[] payload = new byte[packetStr.Length + StopSymbol.Length];
            Encoding.ASCII.GetBytes(packetStr).CopyTo(payload, 0);
            payload[payload.Length - 3] = StopSymbol[0];
            payload[payload.Length - 2] = StopSymbol[1];
            payload[payload.Length - 1] = StopSymbol[2];
            await _stream.WriteAsync(payload, 0, payload.Length, _shutdownRequested);
        }

        public async Task ReceivePacketsAsync()
        {
            while (!_shutdownRequested.IsCancellationRequested)
            {
                int read;
                try
                {
                    read = await _stream.ReadAsync(_buffer, 0, _buffer.Length, _shutdownRequested);
                    if (read == 0)
                        throw new Exception("end of stream");
                }
                catch (Exception)
                {
                    Close();
                    return;
                }

                try
                {
                    int endOfPacket;
                    int offset = 0;
                    while ((endOfPacket = FindStopSymbol(_buffer, offset, read)) > 0)
                    {
                        string packet = Encoding.ASCII.GetString(_buffer, offset, endOfPacket - offset);
                        Packet p = _jsonParser.Parse<Packet>(packet);
                        OnReceived(this, p);
                        offset += endOfPacket + StopSymbol.Length;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Close();
                    return;
                }
            }
        }

        public void Close()
        {
            _stream.Dispose();
            OnClosed(this);
        }

        private void OnReceived(Communicator sender, Packet packet)
        {
            var handler = Received;
            if (handler != null)
                handler(sender, packet);
        }

        private void OnClosed(Communicator sender)
        {
            var handler = Closed;
            if (handler != null)
                handler(sender);
        }

        private int FindStopSymbol(byte[] buffer, int offset, int readCount)
        {
            int endOfPacket = -1;
            for (int i = offset; i < readCount; i++)
            {
                if (_buffer[i] == StopSymbol[0] && i + 1 < readCount && _buffer[i + 1] == StopSymbol[1] && i + 2 < readCount && _buffer[i + 2] == StopSymbol[2])
                {
                    // found stop symbol
                    endOfPacket = i;
                    break;
                }
            }
            return endOfPacket;
        }
    }
}
