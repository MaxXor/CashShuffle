using System;
using System.Collections.Generic;
using System.Threading;
using CashShuffle.Messages;

namespace CashShuffle
{
    public class Pool
    {
        private CancellationToken _shutdownRequested;
        private List<Guest> _guests;

        public event NewRoundEventHandler NewRound;
        public delegate void NewRoundEventHandler(Pool sender);
        public int PoolCapacity { get; private set; }
        public int PoolSize { get { return _guests.Count; } }

        public Pool(CancellationToken shutdownRequested, int poolCapacity)
        {
            this._guests = new List<Guest>();
            this._shutdownRequested = shutdownRequested;
            this.PoolCapacity = poolCapacity;
        }

        public void AddGuest(Guest sender)
        {
            Console.WriteLine("New guest connected!");

            _guests.Add(sender);
            sender.Disconnected += this.RemoveGuest;
            sender.Forward += this.Forward;

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

        private void OnNewRound(Pool sender)
        {
            Console.WriteLine("Announcing new round!");

            var handler = NewRound;
            if (handler != null)
                handler(sender);
        }
    }
}
