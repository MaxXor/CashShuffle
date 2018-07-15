using System;
using Mono.Options;

// https://github.com/cashshuffle/cashshuffle-electron-cash-plugin/wiki/Protocol-Description
// https://github.com/cashshuffle/cashshuffle-electron-cash-plugin/wiki/Server-Specification
//ex: { "fromKey": { "key": "12345" }, "registration": { "amount": "1337" } }⏎

namespace CashShuffle
{
    public class Program
    {
        private static string certPath = null;
        private static int serverPort = 8080;
        private static int poolCapacity = 5;
        private static bool showHelp = false;

        private static OptionSet options = new OptionSet {
            { "c|certificate=", "Path to certificate file in PFX format.", (string c) => certPath = c },
            { "p|port=", "Server port (default 8080).", (int p) => serverPort = p },
            { "s|size=", "Pool size (default 5).", (int s) => poolCapacity = s },
            { "h|help", "Show help information.", h => { if (h != null) ShowHelp(); } }
        };

        public static void Main(string[] args)
        {
            try
            {
                // parse command line args
                options.Parse(args);
                if (certPath == null) throw new OptionException("Path to certificate is required.", "certificate");

                Console.WriteLine("Starting CashShuffle server...");
                Server s = new Server(certPath, serverPort, poolCapacity);
                s.StartAsync();
                Console.ReadLine();
                s.Stop();
            }
            catch (OptionException e)
            {
                // output some error message
                Console.Write("CashShuffle: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `dotnet run -- --help' for more information.");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: dotnet run -- [OPTIONS]");
            options.WriteOptionDescriptions(Console.Out);
            Environment.Exit(0);
        }
    }
}
