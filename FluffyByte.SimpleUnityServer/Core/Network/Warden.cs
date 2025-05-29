namespace FluffyByte.SimpleUnityServer.Core.Network
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Core.Network.Client;
    using FluffyByte.SimpleUnityServer.Utilities;
    using FluffyByte.SimpleUnityServer.Enums;
    using System.Security.Cryptography.X509Certificates;
    using FluffyByte.SimpleUnityServer.Game.Managers;
    using System.Reflection.Metadata.Ecma335;

    internal static class Warden
    {
        public static async Task GreetNewClient(GameClient client)
        {
            bool handshakeSuccess = await client.Messenger.PerformHandshakeAsync();

            client.Messenger.StartBackgroundLoops();
            
            if(!handshakeSuccess)
            {
                await Scribe.WarnAsync($"[{client.Name}] Handshake Failed.");
                client.RaiseRequestToDisconnect();

                return;
            }

            SystemOperator.Instance.HeartbeatManager.Register(client);
            SystemOperator.Instance.NetworkManager.ConnectedClients.Add(client);

            // Pass to Shroud at this point
        }
    }
}