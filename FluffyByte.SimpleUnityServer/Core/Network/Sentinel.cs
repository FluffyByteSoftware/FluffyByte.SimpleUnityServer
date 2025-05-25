using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleUnityServer.Enums;
using FluffyByte.SimpleUnityServer.Interfaces;
using FluffyByte.SimpleUnityServer.Utilities;

namespace FluffyByte.SimpleUnityServer.Core.Network
{
    internal class Sentinel : ICoreService
    {
        public string Name { get; } = "Sentinel";

        private readonly CancellationTokenSource _cts = new();

        public NetworkManager Manager { get; private set; } = new();

        public TcpListener Listener { get; private set; } = new(IPAddress.Parse("10.0.0.84"), 9998);

        public CancellationToken CancelToken => _cts.Token;

        public CoreServiceStatus Status { get; private set; } = CoreServiceStatus.Default;

        public async Task StartAsync()
        {
            try
            {
                Status = CoreServiceStatus.Starting;

                Listener.Start();
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
            try
            {
                while (!CancelToken.IsCancellationRequested)
                {

                }
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

        public async Task StopAsync()
        {

        }

    }
}
