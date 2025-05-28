using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleUnityServer.Core.Network.Client;

namespace FluffyByte.SimpleUnityServer.Interfaces
{
    internal interface IGameClientComponent : IDisposable
    {
        string Name { get; }
        GameClient Parent { get; }

    }
}
