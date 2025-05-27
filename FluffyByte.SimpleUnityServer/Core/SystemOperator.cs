namespace FluffyByte.SimpleUnityServer.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Enums;
    using FluffyByte.SimpleUnityServer.Game.Managers;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class SystemOperator
    {
        private static readonly Lazy<SystemOperator> _instance = new(() => new());
        public static SystemOperator Instance => _instance.Value;

        public CoreServiceStatus Status { get; private set; } = CoreServiceStatus.Default;

        // Main list of all services to start/stop
        public ThreadSafeList<ICoreService> ListOfCoreServices { get; private set; } = [];

        // Tracks running services for stop/status
        private readonly ThreadSafeList<ICoreService> _listOfRunningServices = [];

        public readonly Sentinel Sentinel = new();
        public readonly HeartbeatManager HeartbeatManager = new();
        public readonly GameObjectManager GameObjectManager = new();

        public event Action? ServiceStarted;
        public event Action? ServiceStopped;
        public event Action? ServiceErrored;

        public async Task StartSystem()
        {
            if (Status != CoreServiceStatus.Default && Status != CoreServiceStatus.Errored)
            {
                Status = CoreServiceStatus.Errored;
                await Scribe.ErrorAsync("SystemOperator asked to start system, but was not in a valid state to start from.");
                await Scribe.DebugAsync($"SystemOperator Status: {Status}");

                return;
            }

            try
            {
                ListOfCoreServices.Add(Sentinel);
                ListOfCoreServices.Add(HeartbeatManager);
                ListOfCoreServices.Add(GameObjectManager);

                Status = CoreServiceStatus.Starting;
                // Take a snapshot to avoid concurrency issues during iteration
                var coreServicesSnapshot = ListOfCoreServices.ToArray();
                foreach (ICoreService service in coreServicesSnapshot)
                {
                    try
                    {
                        await Scribe.DebugAsync($"ICoreService: {service.Name} - Starting...");
                        await service.StartAsync();
                        _listOfRunningServices.Add(service);
                        await Scribe.DebugAsync($"ICoreService: {service.Name} - Started.");
                        ServiceStarted?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        await Scribe.ErrorAsync(ex);
                        ServiceErrored?.Invoke();
                        continue;
                    }
                    finally
                    {
                        await Task.Delay(10);
                    }
                }
                if (ListOfCoreServices.Count > _listOfRunningServices.Count)
                {
                    await Scribe.WarnAsync("Shutting down... a number of core services are not running.");
                    await Scribe.WarnAsync("Please check the log for more information.");
                    await StopSystem();
                }
                else
                {
                    Status = CoreServiceStatus.Running;
                }
            }
            catch (Exception ex)
            {
                Status = CoreServiceStatus.Errored;
                await Scribe.ErrorAsync(ex);
                ServiceErrored?.Invoke();
            }
        }

        public async Task StopSystem()
        {
            // Stop in reverse order if dependency is a concern
            var runningServicesSnapshot = _listOfRunningServices.ToArray();
            foreach (ICoreService service in runningServicesSnapshot)
            {
                try
                {
                    await Scribe.WriteAsync($"ICoreService: {service.Name} - Stopping...");
                    await service.StopAsync();
                    _listOfRunningServices.Remove(service);
                    await Scribe.WriteAsync($"ICoreService: {service.Name} - Stopped.");
                    ServiceStopped?.Invoke();
                }
                catch (Exception ex)
                {
                    await Scribe.ErrorAsync(ex);
                    ServiceErrored?.Invoke();
                    continue;
                }
                finally
                {
                    await Task.Delay(10);
                }
            }
            Status = CoreServiceStatus.Stopped;
        }

        public async Task SystemStatus()
        {
            await Scribe.WriteAsync("Current status of ICoreServices:");
            var runningServicesSnapshot = _listOfRunningServices.ToArray();
            foreach (ICoreService service in runningServicesSnapshot)
            {
                try
                {
                    CoreServiceStatus status = service.Status;
                    await Scribe.WriteAsync($"{service.Name} - {status}");
                }
                catch (Exception ex)
                {
                    await Scribe.ErrorAsync(ex);
                    ServiceErrored?.Invoke();
                }
            }
            await Scribe.WriteAsync($"Services Running: {_listOfRunningServices.Count} / {ListOfCoreServices.Count}");
        }
    }
}
