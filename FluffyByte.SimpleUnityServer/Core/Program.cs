namespace FluffyByte.SimpleUnityServer.Core
{
    using System;
    using FluffyByte.SimpleUnityServer.Utilities;

    internal class Program
    {

        public static async Task Main(string[] args)
        {
            

            if(args.Length == 0)
            {

            }

            await Scribe.WriteAsync("Welcome to SimpleUnityServer by FluffyByte");
            await Scribe.WriteAsync("Press any key to begin...");

            Console.ReadLine();

            await Scribe.ClearConsole();


        }
    }
}