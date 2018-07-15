using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashShuffle.Messages;
using Google.Protobuf;

namespace CashShuffle
{
    public class Guest
    {
        private static readonly byte[] StopSymbol = new byte[] { 226, 143, 142 }; // this represents the character ⏎
        private TcpClient _client;
        private Communicator _communicator;
        private Pool _pool;

        public event DisconnectedEventHandler Disconnected;
        public delegate void DisconnectedEventHandler(Guest sender);
        public event ForwardEventHandler Forward;
        public delegate void ForwardEventHandler(string receiverKey, Guest sender, Packet packet);
        public string VerificationKey { get; set; }
        public ulong Amount { get; set; }
        public Guid SessionGuid { get; private set; }
        public uint GuestNumber { get; set; }

        public Guest(Pool pool, TcpClient client, SslStream stream, CancellationToken shutdownRequested, uint guestNumber)
        {
            this.SessionGuid = Guid.NewGuid();
            this.GuestNumber = guestNumber;
            this._pool = pool;
            this._pool.NewRound += StartNewRoundAsync;
            this._client = client;
            this._communicator = new Communicator(stream, shutdownRequested);
            this._communicator.Received += ProcessPacketAsync;
            this._communicator.Closed += CommunicatorClosed;
            this._communicator.ReceivePacketsAsync();
        }

        private void CommunicatorClosed(Communicator Communicator)
        {
            _pool.NewRound -= StartNewRoundAsync;
            OnDisconnected(this);
            Disconnect();
        }

        public async Task SendAsync(Packet p)
        {
            await _communicator.SendPacketAsync(p);
        }

        public void Disconnect()
        {
            _client.Close();
        }

        private async void StartNewRoundAsync(Pool sender)
        {
            if (string.IsNullOrEmpty(VerificationKey)) return;
            Packet p = new Packet();
            p.Session = ByteString.CopyFrom(SessionGuid.ToString(), Encoding.ASCII);
            p.Number = GuestNumber;
            p.Message = new Message();
            p.Message.Str = "New round";
            await _communicator.SendPacketAsync(p);
        }

        private async Task SendBlameAsync(Reason reason)
        {
            Packet p = new Packet();
            p.Message = new Message();
            p.Message.Blame = new Blame();
            p.Message.Blame.Reason = reason;
            await _communicator.SendPacketAsync(p);
        }

        private void OnDisconnected(Guest sender)
        {
            var handler = Disconnected;
            if (handler != null)
                handler(sender);
        }

        private void OnForward(string receiverKey, Guest sender, Packet packet)
        {
            var handler = Forward;
            if (handler != null)
                handler(receiverKey, sender, packet);
        }

        private async void ProcessPacketAsync(Communicator sender, Packet packet)
        {
            try
            {
                if (string.IsNullOrEmpty(VerificationKey))
                {
                    VerificationKey = packet.FromKey.Key;
                    Amount = packet.Registration.Amount;

                    Packet p = new Packet();
                    p.Session = ByteString.CopyFrom(SessionGuid.ToString(), Encoding.ASCII);
                    p.Number = GuestNumber;
                    await _communicator.SendPacketAsync(p);
                    return;
                }

                if (!IsPacketValid(packet))
                {
                    await SendBlameAsync(Reason.Invalidformat);
                    Disconnect();
                    return;
                }

                OnForward(packet.ToKey.Key, this, packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private bool IsPacketValid(Packet packet)
        {
            return (packet.Session != ByteString.CopyFrom(SessionGuid.ToString(), Encoding.ASCII) &&
                    packet.FromKey.Key != VerificationKey &&
                    packet.ToKey != null);
        }
    }
}
