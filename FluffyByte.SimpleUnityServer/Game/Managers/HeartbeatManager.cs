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
            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    if (_tickables.Count == 0)
                    {
                        await Scribe.WriteAsync("Nothing to tick.");
                        await Task.Delay(_tickIntervalMs, CancelToken);
                        continue; // skip to next iteration, don't return!
                    }


                    foreach (ITickable tickable in _tickables.ToArray())
                    {
                        try
                        {
                            Scribe.Debug($"SEnding tick to {tickable.Name}");

                            await tickable.Tick();
                        }
                        catch (Exception ex)
                        {
                            await Scribe.ErrorAsync($"Tick exception: {ex}");
                        }
                    }
                    await Task.Delay(_tickIntervalMs, CancelToken);
                }
            }
            catch(Exception ex)
            {
                await Scribe.ErrorAsync(ex);
            }
        }

    }
}
