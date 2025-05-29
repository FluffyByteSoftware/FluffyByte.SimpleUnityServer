using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FluffyByte.SimpleUnityServer.Interfaces
{
    internal interface IGameClient
    {
        byte[]? AuthChallenge { get; }

        string Name { get; }
    }
}
