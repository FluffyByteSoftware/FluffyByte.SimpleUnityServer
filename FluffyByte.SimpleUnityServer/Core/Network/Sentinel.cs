using System.Net;
using System.Net.Sockets;
using FluffyByte.SimpleUnityServer.Core;
using FluffyByte.SimpleUnityServer.Core.Network.Client;
using FluffyByte.SimpleUnityServer.Enums;
using FluffyByte.SimpleUnityServer.Game.Managers;
using FluffyByte.SimpleUnityServer.Utilities;

internal class Sentinel : CoreServiceTemplate
{
    public override string Name { get; } = "Sentinel";
    public TcpListener Listener { get; private set; } = new(IPAddress.Parse("10.0.0.84"), 9998);

    public Sentinel() : base() { }   

    public override async Task StartAsync()
    {
        try
        {
            SetStatus(CoreServiceStatus.Starting);
            await Scribe.DebugAsync($"[{Name}] Starting TCP Listener...");
            Listener.Start();
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
            await Scribe.DebugAsync($"[{Name}] Stopping TCP listener...");
            ResetCancellationToken();
            Listener.Stop();
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
                if (Listener.Pending())
                {
                    Socket tcpSocket = await Listener.AcceptSocketAsync();
                    GameClient client = new(tcpSocket);

                    bool handshakeSuccessful = await client.Messenger.PerformHandshakeAsync();

                    client.Messenger.StartBackgroundLoops();

                    if (!handshakeSuccessful)
                    {
                        await Scribe.WarnAsync($"[{client.Name}] Handshake failed. Disconnecting client.");
                        continue;
                    }

                    SystemOperator.Instance.HeartbeatManager.Register(client);
                    SystemOperator.Instance.NetworkManager.ConnectedClients.Add(client);                   
                    
                    _ = HandleClientCommunication(client);
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
