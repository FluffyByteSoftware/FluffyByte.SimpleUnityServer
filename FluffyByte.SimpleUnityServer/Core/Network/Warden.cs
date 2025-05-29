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

    internal static class Warden
    {
        public static void GreetNewClient(GameClient client)
        {
            // Generate and store a nonce for this session
            string nonce = GenerateNonce();
            client.AuthChallenge = nonce; // Store as string now!
            client.Messenger.EnqueueOutgoing(nonce);

            client.State = GameClientState.Greeted;
        }

        public static void TickAuth(GameClient client)
        {
            if (client.State != GameClientState.Greeted)
                return;

            if (!client.Messenger.TryDequeueIncoming(out string? input) || string.IsNullOrWhiteSpace(input))
                return;

            // Use AuthHashManager to verify hash
            bool valid = AuthHashManager.VerifyClientHash(client.SetAuthChallenge(), input.Trim());

            if (valid)
            {
                client.Messenger.EnqueueOutgoing("OK");
                client.State = GameClientState.Authenticated;
                //Shroud.WelcomeToWorld(client); // to implement
            }
            else
            {
                client.Messenger.EnqueueOutgoing("AUTH_FAILED: Invalid handshake.");
                client.State = GameClientState.Disconnecting;
                client.RaiseRequestToDisconnect();
            }
        }

        private static string GenerateNonce()
        {
            byte[] nonceBytes = new byte[16];
            RandomNumberGenerator.Fill(nonceBytes);
            return Convert.ToBase64String(nonceBytes);
        }
    }
}