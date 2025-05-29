using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffyByte.SimpleUnityServer.Enums
{
    internal enum GameClientState
    {
        New,                 // Just connected, not greeted yet
        Greeted,             // GUID sent, challenge about to be issued
        AwaitingSignature,   // Challenge sent, waiting for signed response
        Authenticated,       // Passed handshake, ready for world/game
        InWorld,             // Fully joined game world
        Disconnecting,       // Cleanup in progress
        Disconnected         // Socket is closed/cleaned up
    }
}
