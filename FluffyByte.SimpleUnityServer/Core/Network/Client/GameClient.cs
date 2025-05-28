namespace FluffyByte.SimpleUnityServer.Core.Network.Client
{

    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class GameClient : IGameClient, IDisposable, ITickable
    {
        private static int _id = 0;
        public int Id { get; }

        public Guid Guid { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = "GameClient";

        public Socket Socket { get; private set; }

        public bool Disconnecting => _disconnecting;

        private bool _disconnecting = false;
        private bool _requestedDisconnect = false;
        private bool _disposed = false;

        public Messenger Messenger { get; private set; }
        public ConnectionTracker ConnectionTracker { get; private set; }

        public const int TIMEOUT = 5;
  
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
            if(_requestedDisconnect && !Disconnecting)
            {

            }
            
            if(!IsConnected)
            {
                await RequestDisconnect();
                return;
            }

            try
            {
                await Scribe.DebugAsync($"Ticking GameClient...");

                if(ConnectionTracker.PingClient())
                {
                    
                }

                await Messenger.FlushOutgoingMessages();
            }
            catch
            {
                await RequestDisconnect();
            }
        }

        public void UpdateResponseTime()
        {
            if (ConnectionTracker != null)
                ConnectionTracker.UpdateResponseTime();
        }

        public async Task RequestDisconnect()
        {
            if (_disconnecting)
                return;


            SystemOperator.Instance.HeartbeatManager.Unregister(this);

            _disconnecting = true;

            await CloseForDisposal();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsConnected
        {
            get
            {
                if (Socket == null || _disposed || _disconnecting)
                    return false;

                try
                {
                    // Poll for read with zero timeout to check connection
                    return !(Socket.Poll(1, SelectMode.SelectRead) && Socket.Available == 0);
                }
                catch
                {
                    return false;
                }
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                try { Socket?.Dispose(); } catch { }
            }
            _disposed = true;
        }

        private async Task CloseForDisposal()
        {
            try
            {
                Socket?.Close();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public void RaiseRequestToDisconnect()
        {
            if (_disconnecting || _requestedDisconnect)
                return;

            SystemOperator.Instance.HeartbeatManager.Unregister(this);

            _requestedDisconnect = true;
        }

        public async Task RaiseRequestToDisconnectAsync()
        {
            if (_disconnecting || _disposed)
                return;

            SystemOperator.Instance.HeartbeatManager.Unregister(this);

            _disconnecting = true;

            await CloseForDisposal();
        }
    }
}
