// GameClient.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleUnityServer.Game;
using FluffyByte.SimpleUnityServer.Game.Managers;
using FluffyByte.SimpleUnityServer.Interfaces;
using FluffyByte.SimpleUnityServer.Utilities;
using Microsoft.VisualBasic;

namespace FluffyByte.SimpleUnityServer.Core.Network
{
    internal class GameClient : IDisposable, ITickable
    {
        public Guid Guid { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = "GameClient";

        public Socket TcpSocket { get; private set; }
        public IPEndPoint? UdpEndpoint { get; private set; }

        private bool _disconnecting = false;
        private bool _disposed = false;

        public readonly NetworkStream TcpStream;
        public readonly StreamReader TcpStreamReader;
        public readonly StreamWriter TcpStreamWriter;

        public DateTime FirstConnectedTime { get; private set; }
        public DateTime LastResponseTime { get; private set; }
        public DateTime LastHeartbeat { get; private set; }

        private readonly string clientListPrefixText = "INCOMING_CLIENT_OBJECT_LIST";
        private readonly string serverListPrefixText = "INCOMING_SERVER_OBJECT_LIST";
        private readonly string clientRequestObjectListText = "REQUEST_CLIENT_OBJECT_LIST";
        private readonly string serverRequestObjectListText = "REQUEST_SERVER_OBJECT_LIST";

        public event EventHandler? OnDisconnect;

        public GameClient(Socket tcpSocket)
        {
            try
            {
                TcpSocket = tcpSocket;
                Name = $"GameClient_{Guid}";
                FirstConnectedTime = DateTime.Now;
                LastResponseTime = DateTime.Now;

                TcpStream = new NetworkStream(TcpSocket, ownsSocket: false);
                TcpStreamReader = new StreamReader(TcpStream, Encoding.UTF8);
                TcpStreamWriter = new StreamWriter(TcpStream, Encoding.UTF8) { AutoFlush = true };

                Scribe.Write($"Successful construction of new GameClient: {Name}");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                throw; // Rethrow to indicate construction failed
            }
        }

        public void AttachUdpEndpoint(IPEndPoint endpoint)
        {
            UdpEndpoint = endpoint;
        }

        public async Task SendTextMessage(string message)
        {
            try
            {
                if (TcpStreamWriter == null)
                {
                    await Scribe.WarnAsync($"[{Name}] Attempted to send message on closed stream.");
                    return;
                }
                await TcpStreamWriter.WriteLineAsync(message);
                await TcpStreamWriter.FlushAsync();
            }
            catch (IOException)
            {
                await Scribe.ErrorAsync($"[{Name}] Lost connection to client.");
                await RequestDisconnect();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
                await RequestDisconnect();
            }
        }

        public async Task<string> ReceiveTextMessage()
        {
            if (TcpStreamReader == null)
            {
                await RequestDisconnect();
                return string.Empty;
            }

            try
            {
                string? response = await TcpStreamReader.ReadLineAsync();

                response ??= string.Empty;

                LastResponseTime = DateTime.Now;

                return response;
            }
            catch (IOException)
            {
                await Scribe.WarnAsync($"[{Name}] Client IO exception (likely disconnected).");
                await RequestDisconnect();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
                await RequestDisconnect();
            }
            return string.Empty;
        }

        public async Task RequestDisconnect()
        {
            if (_disconnecting)
                return;

            _disconnecting = true;
            OnDisconnect?.Invoke(this, EventArgs.Empty);
            await CloseForDisposal();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsConnected => TcpSocket?.Connected ?? false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                try { TcpStreamReader?.Dispose(); } catch { }
                try { TcpStreamWriter?.Dispose(); } catch { }
                try { TcpStream?.Dispose(); } catch { }
                try { TcpSocket?.Dispose(); } catch { }
            }
            _disposed = true;
        }

        private async Task CloseForDisposal()
        {
            try
            {
                if (TcpSocket.Connected)
                {
                    try { await TcpSocket.DisconnectAsync(false); } catch { }
                }
                TcpSocket?.Close();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        // Called every server tick (HeartbeatManager)
        public async void Tick()
        {
            try
            {
                await Scribe.DebugAsync($"Ticking GameClient...");
                LastHeartbeat = DateTime.Now;

                if (TcpSocket.Connected)
                {
                    await RequestClientObjectList();
                }
            }
            catch
            {
                await RequestDisconnect();
            }
        }

        // Requests the client to send its object list
        private async Task RequestClientObjectList()
        {
            await SendTextMessage(clientRequestObjectListText);
        }

        // Handles incoming messages
        public async Task OnMessageReceived(string message)
        {
            string messageStart = message.Split('\n')[0].Trim();

            if (messageStart == clientListPrefixText)
            {
                string payload = message[clientListPrefixText.Length..].Trim();

                try
                {
                    var clientObjects = GameObjectManager.ParseObjectList(payload);
                    LastResponseTime = DateTime.Now;
                    // Compare or process as needed
                }
                catch (Exception ex)
                {
                    await Scribe.ErrorAsync(ex);
                }
            }
            else if (messageStart == serverListPrefixText)
            {
                string payload = message[serverListPrefixText.Length..].Trim();

                try
                {
                    var serverObjects = GameObjectManager.ParseObjectList(payload);
                    // Replace or reconcile client-side state with this list.
                    // For a full overwrite:
                    SystemOperator.Instance.GameObjectManager.DeSerializeAllObjects(payload);
                    
                    LastResponseTime = DateTime.Now;

                    await Scribe.DebugAsync("Received and applied authoritative server object list.");
                }
                catch (Exception ex)
                {
                    await Scribe.ErrorAsync(ex);
                }
            }

        }


        // Not yet used: for pushing server objects to client
        public async Task SendServerObjectList()
        {
            StringBuilder sb = new();
            sb.AppendLine(serverListPrefixText);
            
            foreach (ServerGameObject obj in SystemOperator.Instance.GameObjectManager.AllObjects)
                sb.AppendLine(obj.SerializationString());
            
            await SendTextMessage(sb.ToString().TrimEnd());
        }
    }
}
