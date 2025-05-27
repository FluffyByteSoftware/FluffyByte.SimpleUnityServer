using System.Net;
using System.Net.Sockets;
using FluffyByte.SimpleUnityServer.Core;
using FluffyByte.SimpleUnityServer.Core.Network;
using FluffyByte.SimpleUnityServer.Enums;
using FluffyByte.SimpleUnityServer.Game.Managers;
using FluffyByte.SimpleUnityServer.Utilities;

internal class Sentinel : CoreServiceTemplate
{
    public override string Name { get; } = "Sentinel";
    public NetworkManager Manager { get; private set; } = new();
    public TcpListener Listener { get; private set; } = new(IPAddress.Parse("10.0.0.84"), 9998);

    public Sentinel() : base() { }

    private const string SECRET = "YourSuperSecretKey12345";

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

                    bool handshakeSuccessful = await PerformHandshake(client.TcpStreamReader, client.TcpStreamWriter);
                    
                    if(!handshakeSuccessful)
                    {
                        await Scribe.WarnAsync($"[{client.Name}] Handshake failed. Disconnecting client.");
                        continue;
                    }

                    SystemOperator.Instance.HeartbeatManager.Register(client);

                    await Manager.AddConnectedClient(client);
                    
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
            await client.SendTextMessage("Welcome.");

            while (client.IsConnected)
            {
                string message = await client.ReceiveTextMessage();

                if (message[0] == '/')
                {
                    string[] parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    switch (parts[0].ToLowerInvariant())
                    {
                        case "/quit":
                            await client.SendTextMessage("Goodbye!");
                            await client.RequestDisconnect();
                            return;
                        case "/ping":
                            await client.SendTextMessage("Pong!");
                            break;
                        default:
                            await client.SendTextMessage($"Unknown command: {parts[0]}");
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Scribe.ErrorAsync(ex);
        }
    }


    private static async Task<bool> PerformHandshake(StreamReader reader, StreamWriter writer)
    {
        // Step 1: Send a nonce (random string) to the client
        string nonce = Guid.NewGuid().ToString();
        await writer.WriteLineAsync(nonce);

        // Step 2: Receive hash from client
        string? clientHash = await reader.ReadLineAsync();

        if (string.IsNullOrEmpty(clientHash))
        {
            await Scribe.DebugAsync("Client did not send a hash.");
            return false;
        }

        // Step 3: Compute expected hash
        string expectedHash = ComputeSha256Hash(nonce + SECRET);

        // Step 4: Validate
        if (!string.Equals(clientHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync("ERROR: Unauthorized client.");
            return false;
        }

        await writer.WriteLineAsync("OK");
        return true;
    }

    private static string ComputeSha256Hash(string rawData)
    {
        byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawData));
        return System.Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
