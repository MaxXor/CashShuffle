using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CashShuffle.Messages;

namespace CashShuffle
{
    public class Pool
    {
        private CancellationToken _shutdownRequested;
        private List<Guest> _guests;
        private Dictionary<string, int> _excludedKeys;

        public event NewRoundEventHandler NewRound;
        public delegate void NewRoundEventHandler(Pool sender);
        public int PoolCapacity { get; private set; }
        public int PoolSize { get { return _guests.Count; } }

        public Pool(CancellationToken shutdownRequested, int poolCapacity)
        {
            if (poolCapacity < 2) throw new ArgumentException($"{nameof(poolCapacity)} must be bigger than 1.");
            this._guests = new List<Guest>();
            this._excludedKeys = new Dictionary<string, int>();
            this._shutdownRequested = shutdownRequested;
            this.PoolCapacity = poolCapacity;
        }

        public void AddGuest(Guest sender)
        {
            if (IsGuestBanned(sender))
            {
                Console.WriteLine("Banned guest joined, disconnecting...");
                sender.Disconnect();
                return;
            }
            Console.WriteLine("New guest joined the pool!");

            _guests.Add(sender);
            sender.Disconnected += this.RemoveGuest;
            sender.Forward += this.Forward;
            sender.BanRequest += this.BanRequest;

            if (PoolSize == PoolCapacity)
                StartRound();
        }

        private void StartRound()
        {
            Packet p = new Packet();
            p.Phase = Phase.Announcement;
            p.Number = (uint)PoolSize;
            Forward(null, null, p);
        }

        public void RemoveGuest(Guest sender)
        {
            Console.WriteLine("Guest disconnected!");

            _guests.Remove(sender);
            sender.Disconnected -= this.RemoveGuest;
            sender.Forward -= this.Forward;
            sender.BanRequest -= this.BanRequest;

            if (_shutdownRequested.IsCancellationRequested)
                return;

            // if guest disconnects during suffling phase, announce new round
            for (int i = 0; i < _guests.Count; i++)
            {
                _guests[i].GuestNumber = (uint)i + 1;
            }
            OnNewRound(this);
        }

        public async void Forward(string toKey, Guest sender, Packet packet)
        {
            foreach (Guest g in _guests)
            {
                // handle broadcasts and unicasts
                if (string.IsNullOrEmpty(toKey) || toKey == g.VerificationKey)
                    await g.SendAsync(packet);
            }
        }

        public bool IsGuestBanned(Guest guest)
        {
            if (_excludedKeys.ContainsKey(guest.VerificationKey))
            {
                return (_excludedKeys[guest.VerificationKey] >= PoolCapacity - 1);
            }
            return false;
        }

        public void BanRequest(string verificationKey, Guest sender)
        {
            Console.WriteLine("Received ban request from guest.");

            if (!_excludedKeys.ContainsKey(verificationKey))
            {
                _excludedKeys.Add(verificationKey, 1);
                return;
            }
            _excludedKeys[verificationKey] += 1;

            if (_excludedKeys[verificationKey] >= PoolCapacity - 1)
            {
                BanGuest(verificationKey);
            }
        }

        private void BanGuest(string verificationKey)
        {
            Console.WriteLine($"All other guests agreed to ban guest with verification key: {verificationKey}");
            Guest guestToBan = _guests.First(x => x.VerificationKey == verificationKey);
            guestToBan?.Disconnect();
        }

        private void OnNewRound(Pool sender)
        {
            Console.WriteLine("Announcing new round!");

            var handler = NewRound;
            if (handler != null)
                handler(sender);
        }
    }
}
