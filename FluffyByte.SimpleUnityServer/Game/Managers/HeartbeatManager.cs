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
            Scribe.Debug($"Registering {tickable.Name} to HeartbeatManager.");
            _tickables.Add(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            _tickables.Remove(tickable);

            int tickCount = _tickables.Count;

            Scribe.Debug($"Unregistered {tickable} from HeartbeatManager. Current count: {tickCount}");
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
                foreach (ITickable tickable in _tickables.ToArray())
                    tickable.Tick();

                await Scribe.DebugAsync($"HeartbeatManager ticked {_tickables.Count} objects.");
                await Task.Delay(_tickIntervalMs, CancelToken);
            }
        }
    }
}
