namespace FluffyByte.SimpleUnityServer.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Interfaces;


    internal class SystemOperator
    {
        private static readonly Lazy<SystemOperator> _instance = new(() => new());
        public static SystemOperator Instance => _instance.Value;

        public List<ICoreService> ListOfCoreServices { get; private set; } = [];


    }
}
