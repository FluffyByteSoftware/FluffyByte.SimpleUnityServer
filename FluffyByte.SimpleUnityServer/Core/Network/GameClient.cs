using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleUnityServer.Utilities;

namespace FluffyByte.SimpleUnityServer.Core.Network
{
    internal class GameClient
    {
        public Guid Guid { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = "GameClient";

        public Socket TcpSocket { get; private set; }
        public Socket UdpSocket { get; private set; }

        private bool _connected = false;

        public bool Connected => _connected;

        private readonly NetworkStream _udpStream;
        private readonly NetworkStream _tcpStream;

        private readonly StreamReader _tcpStreamReader;
        private readonly StreamWriter _tcpStreamWriter;

        public DateTime FirstConnectedTime;

        public GameClient(Socket udpSocket, Socket tcpSocket)
        {
            try
            {
                TcpSocket = tcpSocket;
                UdpSocket = udpSocket;



                Name = $"GameClient_{Guid}";

                _connected = true;

                FirstConnectedTime = DateTime.Now;

                Scribe.Write($"Successful construction of new GameClient: {Name}");
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task SendTextMessage(string message)
        {
            try
            {

            }
            catch(IOException)
            {
                await Scribe.ErrorAsync("Lost connection to client.");
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public async Task<string> ReceiveTextMessage()
        {
            string? response;

            try
            {
                response = await _tcpStreamReader.ReadLineAsync();

                return response ?? string.Empty;
            }
            catch(IOException)
            {

            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }

            return string.Empty;
        }
    }
}
