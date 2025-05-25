
namespace FluffyByte.SimpleUnityServer.Core.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class NetworkManager : IHelper
    {
        public string Name => "NetworkManager";

        public ConcurrentDictionary<GameClient, DateTime> ConnectedClients { get; private set; } = [];

        public async Task AddConnectedClient(GameClient gClient)
        {
            try
            {
                ConnectedClients.TryAdd(gClient, DateTime.Now);
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
                int totalConnectedClients = ConnectedClients.Keys.Count;                

                
                await Scribe.WriteCleanAsync($"Current Connected Clients: {totalConnectedClients}");

                
                Dictionary<GameClient, DateTime> temp = ConnectedClients.ToDictionary();

                foreach(GameClient gClient in temp.Keys)
                {
                    await Scribe.WriteCleanAsync($"ConnectedClient: {gClient.Name} first connected; {gClient.FirstConnectedTime}");

                }
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }


    }
}
