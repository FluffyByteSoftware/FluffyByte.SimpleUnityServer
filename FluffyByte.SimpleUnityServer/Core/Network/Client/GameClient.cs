namespace FluffyByte.SimpleUnityServer.Core.Network.Client
{

    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Game.Managers;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class GameClient : IGameClient, IDisposable, ITickable
    {
        private static int _id = 0;
        public int Id { get; }

        public const int TIMEOUT = 5;

        public Guid Guid { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = "Game Client";
        public Socket Socket { get; private set; }

        public bool PingClient 
        { 
            get
            {
                return ConnectionTracker.PingClient();
            } 
        }

        public bool DisconnectRequested => _requestedDisconnect;

        private bool _requestedDisconnect = false;
        private bool _disconnected = false;

        public Messenger Messenger { get; private set; }
        public ConnectionTracker ConnectionTracker { get; private set; }

        public GameClient(Socket tcpSocket)
        {
            try
            {
                Socket = tcpSocket;
                Name = $"GameClient_{Id}";
                
                ConnectionTracker = new(this);
                Messenger = new(this, tcpSocket);

                Scribe.Write($"Successful construction of new GameClient: {Name}");

                Id = _id++;
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                throw; // Rethrow to indicate construction failed
            }
        }

        // Called every server tick (HeartbeatManager), sends all queued messages this tick
        public async Task Tick()
        {
            if(DisconnectRequested && !_disconnected)
            {
                SystemOperator.Instance.HeartbeatManager.Unregister(this);

                await Disconnect();

                return;
            }


        }

        public void UpdateResponseTime()
        {
            ConnectionTracker.UpdateResponseTime();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void RaiseRequestToDisconnect()
        {
            if (_requestedDisconnect)
                return;

            SystemOperator.Instance.HeartbeatManager.Unregister(this);

            _requestedDisconnect = true;
        }

        private async Task Disconnect()
        {
            if (_disconnected) return;
            
            if (!DisconnectRequested) return;

            _disconnected = true;

            await Socket.DisconnectAsync(true);
        }
    }
}
