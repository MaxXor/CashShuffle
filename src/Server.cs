using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace CashShuffle
{
    public class Server
    {
        private X509Certificate2 _serverCertificate;
        private TcpListener _listener;
        private List<Pool> _pools;
        private CancellationTokenSource _shutdownRequested;
        private int _poolCapacity;

        public Server(string certificatePath, int port = 8080, int poolCapacity = 5)
        {
            this._serverCertificate = new X509Certificate2(certificatePath);
            this._listener = new TcpListener(IPAddress.Any, port);
            this._pools = new List<Pool>();
            this._shutdownRequested = new CancellationTokenSource();
            this._poolCapacity = poolCapacity;
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Waiting for new guests to join...");

            while (!_shutdownRequested.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    SslStream sslStream = new SslStream(client.GetStream(), false);
                    await sslStream.AuthenticateAsServerAsync(_serverCertificate, false, SslProtocols.Tls12, false);

                    Guest guest = new Guest(client, sslStream, _shutdownRequested.Token);
                    guest.Verified += GuestVerified;
                }
                catch (Exception ex) when (!(ex is ObjectDisposedException || ex is AuthenticationException))
                {
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }
        }

        private void GuestVerified(string verificationKey, Guest sender)
        {
            Console.WriteLine("Received verification key from guest.");
            sender.Verified -= GuestVerified;
            Pool pool = GetPool();
            sender.AddPool(pool);
        }

        private Pool GetPool()
        {
            foreach (Pool p in _pools)
            {
                if (p.PoolSize < p.PoolCapacity)
                {
                    return p;
                }
            }

            Pool newPool = new Pool(_shutdownRequested.Token, _poolCapacity);
            AddPool(newPool);
            return newPool;
        }

        public void AddPool(Pool pool)
        {
            _pools.Add(pool);
        }

        public void RemovePool(Pool pool)
        {
            _pools.Remove(pool);
        }

        public void Stop()
        {
            _shutdownRequested.Cancel();
            _listener.Stop();
        }
    }
}
