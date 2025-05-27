namespace FluffyByte.SimpleUnityServer.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal interface IHelper
    {
        public string Name { get; }
        public Guid Guid { get; }

    }
}
