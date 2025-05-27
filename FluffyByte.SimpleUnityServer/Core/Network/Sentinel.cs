using System.Net;
using System.Net.Sockets;
using FluffyByte.SimpleUnityServer.Core;
using FluffyByte.SimpleUnityServer.Core.Network;
using FluffyByte.SimpleUnityServer.Enums;
using FluffyByte.SimpleUnityServer.Utilities;

internal class Sentinel : CoreServiceTemplate
{
    public override string Name { get; } = "Sentinel";
    public NetworkManager Manager { get; private set; } = new();
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
                    
                    await Manager.AddConnectedClient(client);
                    // Transition to Warden, e.g.:
                    // Warden.HandleClient(client);

                    await Scribe.DebugAsync($"[{Name}] Accepted new TCP client: {client.Guid}");
                }
                await Task.Delay(10, CancelToken);
            }
        }
        catch (OperationCanceledException)
        {
            _ = Scribe.DebugAsync("WelcomeNewClientLoop canceled due to shutdown?");
        }
        catch (Exception ex)
        {
            await Scribe.ErrorAsync(ex);
        }
    }
}
