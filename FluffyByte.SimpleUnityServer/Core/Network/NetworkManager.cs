
namespace FluffyByte.SimpleUnityServer.Core.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Core.Network.Client;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class NetworkManager : CoreServiceTemplate
    {
        public override string Name => "NetworkManager";
        public Guid Guid { get; private set; } = Guid.NewGuid();

        public ThreadSafeList<GameClient> ConnectedClients { get; private set; } = [];

        public async Task AddConnectedClient(GameClient gClient)
        {
            try
            {
                ConnectedClients.Add(gClient);
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public async Task PollUsers()
        {
            try
            {
                int totalConnectedClients = ConnectedClients.Count;                

                
                await Scribe.WriteCleanAsync($"Current Connected Clients: {totalConnectedClients}");

                
                ThreadSafeList<GameClient> temp = ConnectedClients;

                foreach(GameClient gClient in temp)
                {
                    await Scribe.WriteCleanAsync($"ConnectedClient: {gClient.Name} first connected; {gClient.ConnectionTracker.ConnectionStartTime}");

                }
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }


    }
}
