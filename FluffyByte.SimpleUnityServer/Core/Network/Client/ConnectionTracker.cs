namespace FluffyByte.SimpleUnityServer.Core.Network.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class ConnectionTracker : IGameClientComponent, IDisposable
    {
        public string Name => "Connection Tracker";

        public DateTime ConnectionStartTime;
        public DateTime LastResponseTime;
        public DateTime LastTickTime;

        private const double TIMEOUT = 2.0;

        private readonly Socket Socket;

        public GameClient Parent { get; }

        public ConnectionTracker(GameClient parent)
        {
            Scribe.Debug("New ConnectionTracker");

            Parent = parent;
            
            Socket = Parent.Socket;

            ConnectionStartTime = DateTime.Now;
            LastResponseTime = DateTime.Now;
            LastTickTime = DateTime.Now;
        }

        public void UpdateResponseTime()
        {
            LastResponseTime = DateTime.Now;
        }

        public void UpdateTickTime()
        {
            LastTickTime = DateTime.Now;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public double SecondsSinceLastResponse
        {
            get
            {
                return (DateTime.Now - LastResponseTime).TotalSeconds;
            }
        }

        public bool PingClient()
        {
            try
            {
                if (SecondsSinceLastResponse > TIMEOUT)
                {
                    byte[] buffer = new byte[1];
                    if (Socket.Available > 0)
                    {
                        int read = Socket.Receive(buffer, 0, 1, SocketFlags.Peek);
                    }
                    else
                    {
                        Socket.Receive(buffer, 0, 0, SocketFlags.None);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
