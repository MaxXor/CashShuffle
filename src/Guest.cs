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
        public event VerifiedEventHandler Verified;
        public delegate void VerifiedEventHandler(string verificationKey, Guest sender);
        public event BanRequestEventHandler BanRequest;
        public delegate void BanRequestEventHandler(string verificationKey, Guest sender);
        public string VerificationKey { get; set; }
        public ulong Amount { get; set; }
        public Guid SessionGuid { get; private set; }
        public uint GuestNumber { get; set; }

        public Guest(TcpClient client, SslStream stream, CancellationToken shutdownRequested)
        {
            this.SessionGuid = Guid.NewGuid();
            this._client = client;
            this._communicator = new Communicator(stream, shutdownRequested);
            this._communicator.Received += ProcessPacketAsync;
            this._communicator.Closed += CommunicatorClosed;
            this._communicator.ReceivePacketsAsync();
        }

        private void CommunicatorClosed(Communicator Communicator)
        {
            if (_pool != null)
                _pool.NewRound -= StartNewRoundAsync;
            OnDisconnected(this);
            Disconnect();
        }

        public void AddPool(Pool pool)
        {
            _pool = pool;
            _pool.NewRound += StartNewRoundAsync;
            GuestNumber = (uint)_pool.PoolSize + 1;
            _pool.AddGuest(this);
        }

        public async Task SendAsync(Packet p)
        {
            await _communicator.SendPacketAsync(p);
        }

        public void Disconnect()
        {
            _client.Dispose();
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
            Console.WriteLine("Guest behaved badly, sending blame message.");
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

        private void OnVerified(string verificationKey, Guest sender)
        {
            var handler = Verified;
            if (handler != null)
                handler(verificationKey, sender);
        }

        private void OnBanRequest(string verificationKey, Guest sender)
        {
            var handler = BanRequest;
            if (handler != null)
                handler(verificationKey, sender);
        }

        private async void ProcessPacketAsync(Communicator sender, Packet packet)
        {
            try
            {
                if (string.IsNullOrEmpty(VerificationKey))
                {
                    VerificationKey = packet.FromKey.Key;
                    Amount = packet.Registration.Amount;
                    OnVerified(VerificationKey, this);

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

                if (packet.Phase == Phase.Blame && packet.Message?.Blame?.Reason == Reason.Liar)
                {
                    string verificationKey = packet.Message?.Blame?.Accused?.Key;
                    if (!string.IsNullOrEmpty(verificationKey))
                    {
                        OnBanRequest(verificationKey, this);
                    }
                }

                OnForward(packet.ToKey?.Key, this, packet);
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
