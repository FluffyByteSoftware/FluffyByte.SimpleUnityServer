namespace FluffyByte.SimpleUnityServer.Core.Network.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography;
    using FluffyByte.SimpleUnityServer.Utilities;
    using FluffyByte.SimpleUnityServer.Game.Managers;

    /// <summary>
    /// Responsible for reliable, thread-safe async message IO over a TCP connection for a single client.
    /// Protocol parsing/handling should be done at a higher level.
    /// </summary>
    internal sealed class Messenger : IDisposable
    {
        public string Name => nameof(Messenger);
        public GameClient Parent { get; }

        private readonly NetworkStream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly ConcurrentQueue<string> _incoming = new();
        private readonly ConcurrentQueue<string> _outgoing = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly int _readTimeoutMs;

        /// <summary>
        /// Raised when a new line of text is received from the network (threadpool context).
        /// </summary>
        public event Func<string, Task>? MessageReceived;

        public Messenger(GameClient parent, Socket socket, int readTimeoutMs = 1000)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _stream = new NetworkStream(socket, ownsSocket: false);
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
            _readTimeoutMs = readTimeoutMs;
        }

        /// <summary>
        /// Enqueues an outgoing message to be sent to the client.
        /// </summary>
        public void EnqueueOutgoing(string message)
        {
            ArgumentNullException.ThrowIfNull(message);
            _outgoing.Enqueue(message);
        }

        /// <summary>
        /// Attempts to dequeue a message received from the network (for processing by higher-level code).
        /// </summary>
        public bool TryDequeueIncoming(out string? message)
            => _incoming.TryDequeue(out message);

        /// <summary>
        /// Begins asynchronous background tasks to handle network IO.
        /// </summary>
        public void StartBackgroundLoops()
        {
            Task.Run(() => ReceiveLoopAsync(_cts.Token));
            Task.Run(() => SendLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Background: Reads lines from the network, enqueues them, and raises MessageReceived.
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    string? line = await TryReadLineAsync(_readTimeoutMs, ct);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _incoming.Enqueue(line);
                        if (MessageReceived != null)
                            await MessageReceived.Invoke(line);
                    }
                }
            }
            catch (IOException)
            {
                // Remote closed or network error; signal disconnect
                Parent?.RaiseRequestToDisconnect();
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Utilities.Scribe.Error(ex);
            }
        }

        /// <summary>
        /// Background: Dequeues outgoing messages and sends them to the client.
        /// </summary>
        private async Task SendLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    while (_outgoing.TryDequeue(out var msg))
                    {
                        await SendTextMessageAsync(msg, ct);
                    }
                    await Task.Delay(10, ct); // Avoid busy loop
                }
            }
            catch (IOException)
            {
                Parent?.RaiseRequestToDisconnect();
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Utilities.Scribe.Error(ex);
            }
        }

        /// <summary>
        /// Attempts to read a line from the network with timeout.
        /// </summary>
        private async Task<string?> TryReadLineAsync(int timeoutMs, CancellationToken ct)
        {
            Task<string?> readTask = _reader.ReadLineAsync(ct).AsTask();

            var delayTask = Task.Delay(timeoutMs, ct);
            
            Task completed = await Task.WhenAny(readTask, delayTask);
            
            if (completed == readTask)
                return await readTask;
            
            return null;
        }

        /// <summary>
        /// Sends a single message (with error handling).
        /// </summary>
        private async Task SendTextMessageAsync(string message, CancellationToken ct)
        {
            if (Parent?.DisconnectRequested == true) return;
            try
            {
                await _writer.WriteLineAsync(message.AsMemory(), ct);
            }
            catch (IOException)
            {
                Parent?.RaiseRequestToDisconnect();
            }
            catch (Exception ex)
            {
                await Utilities.Scribe.ErrorAsync(ex);
            }
        }

        /// <summary>
        /// Immediately stops all background network activity and closes underlying resources.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            try { _reader?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            _cts.Dispose();
        }

        public async Task<bool> PerformHandshakeAsync()
        {
            // Step 1: Send nonce to client
            string nonce = Guid.NewGuid().ToString();
            await _writer.WriteLineAsync(nonce);

            // Step 2: Receive hash from client
            string? clientHash = await _reader.ReadLineAsync();

            if (string.IsNullOrEmpty(clientHash))
            {
                await Utilities.Scribe.DebugAsync("Client did not send a hash.");
                return false;
            }

            // Step 3: Validate
            if (!AuthHashManager.VerifyClientHash(nonce, clientHash))
            {
                await _writer.WriteLineAsync("ERROR: Unauthorized client.");
                return false;
            }

            await _writer.WriteLineAsync("OK");
            return true;
        }
    }
}
