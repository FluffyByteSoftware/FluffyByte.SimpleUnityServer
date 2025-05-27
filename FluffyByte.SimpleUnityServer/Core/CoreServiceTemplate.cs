namespace FluffyByte.SimpleUnityServer.Core
{

    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Enums;
    using FluffyByte.SimpleUnityServer.Interfaces;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal abstract class CoreServiceTemplate : ICoreService
    {
        public abstract string Name { get; }
        private CancellationTokenSource _cts = new();
        public CancellationToken CancelToken => _cts.Token;
        private CoreServiceStatus _status = CoreServiceStatus.Default;
        public CoreServiceStatus Status => _status;

        protected void SetStatus(CoreServiceStatus status) => _status = status;

        protected void ResetCancellationToken()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                _cts.Cancel();

            _cts = new CancellationTokenSource();
        }

        protected virtual async Task RequestCancelAsync()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                await _cts.CancelAsync();
        }


        public virtual async Task StartAsync()
        {
            if (_cts.IsCancellationRequested) return;
            
            SetStatus(CoreServiceStatus.Starting);
            
            ResetCancellationToken();
            
            await OnStartAsync();

            SetStatus(CoreServiceStatus.Running);
        }

        public virtual async Task StopAsync()
        {
            if(_cts.IsCancellationRequested)
            {
                await Scribe.WarnAsync("Stop called but cancellation already requested!");
                
                return;
            }

            SetStatus(CoreServiceStatus.Stopping);

            await RequestCancelAsync();
            
            await OnStopAsync();

            SetStatus(CoreServiceStatus.Stopped);
        }

        protected virtual Task OnStartAsync() => Task.CompletedTask;
        protected virtual Task OnStopAsync() => Task.CompletedTask;
    }
}
