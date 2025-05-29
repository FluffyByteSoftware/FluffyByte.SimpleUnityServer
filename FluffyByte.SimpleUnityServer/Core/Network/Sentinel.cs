using System.Net;
using System.Net.Sockets;
using System.Text;
using FluffyByte.SimpleUnityServer.Core;
using FluffyByte.SimpleUnityServer.Core.Network.Client;
using FluffyByte.SimpleUnityServer.Enums;
using FluffyByte.SimpleUnityServer.Game.Managers;
using FluffyByte.SimpleUnityServer.Utilities;

namespace FluffyByte.SimpleUnityServer.Core.Network
{
    internal class Sentinel : CoreServiceTemplate
    {
        public const int MAXSIMULATNEOUSCONNECTIONS = 10;

        public override string Name { get; } = "Sentinel";
        public TcpListener TcpListener { get; private set; } = new(IPAddress.Parse("10.0.0.84"), 9998);

        public Sentinel() : base() { }

        public override async Task StartAsync()
        {
            try
            {
                SetStatus(CoreServiceStatus.Starting);
                
                TcpListener.Start();

                _ = WelcomeNewClientLoop();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public override async Task StopAsync()
        {
            try
            {
                SetStatus(CoreServiceStatus.Stopping);
                
                ResetCancellationToken();
                TcpListener.Stop();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
            finally
            {
                SetStatus(CoreServiceStatus.Stopped);
            }
        }

        private async Task WelcomeNewClientLoop()
        {
            try
            {
                SetStatus(CoreServiceStatus.Running);
                while (!CancelToken.IsCancellationRequested)
                {
                    if (TcpListener.Pending())
                    {
                        
                        Socket tcpSocket = await TcpListener.AcceptSocketAsync();
                        GameClient client = new(tcpSocket);

                        await Warden.GreetNewClient(client);
                    }

                    await Task.Delay(10, CancelToken);
                }
            }
            catch (OperationCanceledException)
            {
                await Scribe.DebugAsync("WelcomeNewClientLoop canceled due to shutdown.");
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        private static async Task HandleClientCommunication(GameClient client)
        {
            try
            {
                client.Messenger.EnqueueOutgoing("Welcome");

                await Task.Delay(10000);

                client.RaiseRequestToDisconnect();
            }
            catch (Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }
    }
}