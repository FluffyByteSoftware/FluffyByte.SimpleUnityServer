namespace FluffyByte.SimpleUnityServer.Game.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Core;
    using FluffyByte.SimpleUnityServer.Enums;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class HeartbeatManager : CoreServiceTemplate
    {
        public override string Name => "Heartbeat Manager";

        private readonly List<ITickable> _tickables = [];

        private readonly int _tickIntervalMs = 250; // 1/4 second

        public void Register(ITickable tickable)
        {
            _tickables.Add(tickable);
        }

        protected override async Task OnStartAsync()
        {
            _ = TickLoop();

            await Task.CompletedTask;
        }

        protected override async Task OnStopAsync()
        {
            await Task.CompletedTask;
        }

        private async Task TickLoop()
        {
            while (!CancelToken.IsCancellationRequested)
            {
                await Scribe.WriteAsync("Ticking all children...");

                foreach (ITickable tickable in _tickables.ToArray())
                    tickable.Tick();

                await Task.Delay(_tickIntervalMs, CancelToken);
            }
        }

    }

}
