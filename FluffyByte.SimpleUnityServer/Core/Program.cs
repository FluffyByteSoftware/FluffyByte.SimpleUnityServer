namespace FluffyByte.SimpleUnityServer.Core
{
    using System;
    using System.Threading.Tasks;
    using FluffyByte.SimpleUnityServer.Enums;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                await Scribe.WriteCleanAsync("Using Args...");
            }

            await Scribe.WriteCleanAsync("Welcome to SimpleUnityServer by FluffyByte");
            await Scribe.WriteCleanAsync("Press any key to begin...");
            Console.ReadLine();

            await SystemOperator.Instance.StartSystem();

            Console.ReadLine();

            await SystemOperator.Instance.StopSystem();

            await Scribe.WriteCleanAsync("Exiting SimpleUnityServer. Goodbye!");
        }
    }
}
