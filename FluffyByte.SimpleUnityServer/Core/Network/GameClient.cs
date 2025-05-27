// GameClient.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleUnityServer.Utilities;

namespace FluffyByte.SimpleUnityServer.Core.Network
{
    internal class GameClient : IDisposable
    {
        public Guid Guid { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = "GameClient";

        public Socket TcpSocket { get; private set; }
        public IPEndPoint? UdpEndpoint { get; private set; }

        private bool _disconnecting = false;
        private bool _disposed = false;

        private readonly NetworkStream _tcpStream;
        private readonly StreamReader _tcpStreamReader;
        private readonly StreamWriter _tcpStreamWriter;

        public DateTime FirstConnectedTime { get; private set; }
        public DateTime LastResponseTime { get; private set; }

        public event EventHandler? OnDisconnect;

        public GameClient(Socket tcpSocket)
        {
            try
            {
                TcpSocket = tcpSocket;
                Name = $"GameClient_{Guid}";
                FirstConnectedTime = DateTime.Now;
                LastResponseTime = DateTime.Now;

                _tcpStream = new NetworkStream(TcpSocket, ownsSocket: false);
                _tcpStreamReader = new StreamReader(_tcpStream, Encoding.UTF8);
                _tcpStreamWriter = new StreamWriter(_tcpStream, Encoding.UTF8) { AutoFlush = true };

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
                if (_tcpStreamWriter == null)
                {
                    await Scribe.WarnAsync($"[{Name}] Attempted to send message on closed stream.");
                    return;
                }
                await _tcpStreamWriter.WriteLineAsync(message);
                await _tcpStreamWriter.FlushAsync();
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
            if (_tcpStreamReader == null)
            {
                await RequestDisconnect();
                return string.Empty;
            }

            try
            {
                string? response = await _tcpStreamReader.ReadLineAsync();

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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                try { _tcpStreamReader?.Dispose(); } catch { }
                try { _tcpStreamWriter?.Dispose(); } catch { }
                try { _tcpStream?.Dispose(); } catch { }
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
    }
}
