namespace FluffyByte.SimpleUnityServer.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Enums;

    internal interface ICoreService
    {
        string Name { get; }

        CancellationToken CancelToken { get; }

        Task StartAsync();
        Task StopAsync();

        Task<CoreServiceStatus> Status();
        
    }
}
