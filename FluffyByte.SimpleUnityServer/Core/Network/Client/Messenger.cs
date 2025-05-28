using System.Net.Sockets;
using FluffyByte.SimpleUnityServer.Game;
using FluffyByte.SimpleUnityServer.Game.Managers;
using FluffyByte.SimpleUnityServer.Interfaces;
using FluffyByte.SimpleUnityServer.Utilities;

namespace FluffyByte.SimpleUnityServer.Core.Network.Client
{
    internal class Messenger : IGameClientComponent
    {
        public string Name => "Messenger";
        public GameClient Parent { get; }

        private readonly NetworkStream _stream;

        private readonly StreamReader _reader;
        public StreamReader TcpReader => _reader;

        private readonly StreamWriter _writer;
        public StreamWriter TcpWriter => _writer;

        private readonly Queue<string> _outbox = new();

        public event Func<string, Task>? MessageReceived;

        private readonly string clientListPrefixText = "INCOMING_CLIENT_OBJECT_LIST";
        private readonly string clientRequestObjectListText = "REQUEST_SERVER_OBJECT_LIST";

        public Messenger(GameClient parent, Socket socket)
        {
            Parent = parent;
            _stream = new(socket, false);
            _reader = new(_stream);
            _writer = new(_stream) { AutoFlush = true };
        }

        public void QueueMessage(string message)
        {
            lock (_outbox)
            {
                _outbox.Enqueue(message);
            }
        }

        public async Task ReceiveTextMessage()
        {
            if (Parent.Disconnecting) 
                return;

            try
            {
                string? response = await _reader.ReadLineAsync();

                Parent.UpdateResponseTime();

                if(!string.IsNullOrWhiteSpace(response))
                {
                    lock(_outbox)
                    {
                        _outbox.Enqueue(response);
                    }
                }
            }
            catch (IOException)
            {
                Parent.RaiseRequestToDisconnect();
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        private async Task SendTextMessage(string message)
        {
            try
            {
                if (Parent.Disconnecting) return;

                await _writer.WriteLineAsync(message);
            }
            catch (IOException)
            {
                Parent.RaiseRequestToDisconnect();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public bool TryDequeueReceived(out string message)
        {
            lock (_outbox)
            {
                if (_outbox.Count > 0)
                {
                    message = _outbox.Dequeue();
                    return true;
                }
            }
            message = string.Empty;
            return false;
        }

        public async Task OnServerMessageReceived(string message)
        {
            string messageStart = message.Split('\n')[0].Trim();

            try
            {
                if (messageStart == clientListPrefixText)
                {
                    string payLoad = message[clientListPrefixText.Length..].Trim();

                    try
                    {
                        List<ServerGameObject> clientObjects = await GameObjectManager.ParseObjectList(payLoad);

                        foreach (ServerGameObject obj in clientObjects)
                        {
                            await Scribe.WriteAsync($"Serialized Client Object: {obj.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Scribe.ErrorAsync(ex);
                    }
                }
                else if (messageStart == clientRequestObjectListText)
                {
                    //awaiting implementation

                }
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public async Task FlushOutgoingMessages()
        {
            while (true)
            {
                string? msg = null;

                lock (_outbox)
                {
                    if (_outbox.Count == 0)
                        break;
                    msg = _outbox.Dequeue();
                }

                if (msg != null)
                {
                    await SendTextMessage(msg);
                }
            }
        }

        public void Dispose()
        {
            try { _reader?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
        }

        public async Task SendClientTheServerList()
        {
            string response = SystemOperator.Instance.GameObjectManager.SerializeAllObjects();

            QueueMessage(response);

            await Task.CompletedTask;
        }
    }

}
