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

            bool exitRequested = false;

            while (!exitRequested)
            {
                await Scribe.ClearConsole();

                // Display menu
                await Scribe.WriteCleanAsync("==== Main Menu ====\n\n");
                await Scribe.WriteCleanAsync("1) Start/Stop the Server");
                await Scribe.WriteCleanAsync("2) Check Status of Services");
                await Scribe.WriteCleanAsync("0) Exit\n\n");
                
                await Scribe.WriteCleanAsync($"Current Server State: {SystemOperator.Instance.Status}");
                await Scribe.WriteCleanAsync("Select an option: ");

                string? input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        // Toggle server state
                        if (SystemOperator.Instance.Status == CoreServiceStatus.Default ||
                            SystemOperator.Instance.Status == CoreServiceStatus.Stopped ||
                            SystemOperator.Instance.Status == CoreServiceStatus.Errored)
                        {
                            await Scribe.WriteCleanAsync("Starting server...");
                            await SystemOperator.Instance.StartSystem();
                        }
                        else if (SystemOperator.Instance.Status == CoreServiceStatus.Running)
                        {
                            await Scribe.WriteCleanAsync("Stopping server...");
                            await SystemOperator.Instance.StopSystem();
                        }
                        else
                        {
                            await Scribe.WriteCleanAsync($"Server is in state: {SystemOperator.Instance.Status} and cannot be started/stopped at this time.");
                        }

                        await Scribe.WriteCleanAsync("Press any key to continue...");
                        Console.ReadLine();
                        break;

                    case "2":
                        await SystemOperator.Instance.SystemStatus();
                        await Scribe.WriteCleanAsync("Press any key to continue...");
                        Console.ReadLine();
                        break;

                    case "0":
                        if(SystemOperator.Instance.Status == CoreServiceStatus.Running)
                        {
                            await Scribe.WriteCleanAsync($"Server is running... shutting down.");

                            await SystemOperator.Instance.StopSystem();
                        }

                        exitRequested = true;
                        break;

                    default:
                        await Scribe.WriteCleanAsync("Invalid option. Please try again.");
                        await Task.Delay(1000);
                        break;
                }
            }

            await Scribe.WriteCleanAsync("Exiting SimpleUnityServer. Goodbye!");
        }
    }
}
