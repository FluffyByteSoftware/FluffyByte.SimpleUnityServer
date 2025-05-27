namespace FluffyByte.SimpleUnityServer.Core.Network
{

    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using FluffyByte.SimpleUnityServer.Game;
    using FluffyByte.SimpleUnityServer.Game.Managers;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;
    using System.Diagnostics;

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
        //private readonly string serverRequestObjectListText = "REQUEST_SERVER_OBJECT_LIST";

        private readonly Queue<string> _messagesToSend = new();
        private const int MaxQueueSize = 10;
        private const bool V = false;

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

        // Queue a message to send during the next tick
        public bool QueueTextMessage(string message)
        {
            lock (_messagesToSend)
            {
                if (_messagesToSend.Count >= MaxQueueSize)
                {
                    // Optionally: log or handle overflow
                    return false;
                }
                _messagesToSend.Enqueue(message);
                
                return true;
            }
        }

        // Called every server tick (HeartbeatManager), sends all queued messages this tick
        public async void Tick()
        {
            try
            {
                await Scribe.DebugAsync($"Ticking GameClient...");

                LastHeartbeat = DateTime.Now;

                if((DateTime.Now - LastResponseTime).TotalSeconds > 2)
                {
                    await Scribe.WarnAsync($"{Name}: No response from client in over 2 seconds.");
                    await RequestDisconnect();

                    return;
                }

                if(!IsSocketConnected())
                {
                    await RequestDisconnect();
                    return;
                }

                await FlushOutgoingMessages();
                QueueTextMessage(clientRequestObjectListText);
            }
            catch
            {
                await RequestDisconnect();
            }
        }

        public bool IsSocketConnected()
        {
            try
            {
                bool check = TcpSocket.Poll(1000, SelectMode.SelectRead) && (TcpSocket.Available == 0);

                return !check;
            }
            catch
            {
                return false;
            }
        }

        // Send all queued messages
        public async Task FlushOutgoingMessages()
        {
            while (true)
            {
                string? message = null;
                lock (_messagesToSend)
                {
                    if (_messagesToSend.Count == 0)
                        break;
                    message = _messagesToSend.Dequeue();
                }
                if (message != null)
                {
                    await SendTextMessage(message);
                }
            }
        }

        // Actually sends to the TCP stream
        private async Task SendTextMessage(string message)
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
                await Scribe.WarnAsync($"[{Name}] Lost connection to client.");
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


            SystemOperator.Instance.HeartbeatManager.Unregister(this);

            _disconnecting = true;
            OnDisconnect?.Invoke(this, EventArgs.Empty);

            await CloseForDisposal();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsConnected => TcpSocket != null && IsSocketConnected();

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
                TcpSocket?.Close();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
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
        public void SendServerObjectList()
        {
            StringBuilder sb = new();
            sb.AppendLine(serverListPrefixText);

            foreach (ServerGameObject obj in SystemOperator.Instance.GameObjectManager.AllObjects)
                sb.AppendLine(obj.SerializationString());

            QueueTextMessage(sb.ToString().TrimEnd());
        }
    }
}
