using System;
using Mono.Options;

// https://github.com/cashshuffle/cashshuffle-electron-cash-plugin/wiki/Protocol-Description
// https://github.com/cashshuffle/cashshuffle-electron-cash-plugin/wiki/Server-Specification
//ex: { "fromKey": { "key": "12345" }, "registration": { "amount": "1337" } }⏎

namespace CashShuffle
{
    public class Program
    {
        static private string certPath = null;
        static private bool showHelp = false;
        static private int poolSize = 5;

        static OptionSet options = new OptionSet {
            { "c|certificate=", "Path to certificate file in PFX format.", (string c) => certPath = c },
            { "s|size=", "Pool size (default 5).", (int s) => poolSize = s },
            { "h|help", "Show help information.", h => { if (h != null) ShowHelp(); } }
        };

        static void Main(string[] args)
        {
            try
            {
                // parse command line args
                options.Parse(args);
                if (certPath == null) throw new OptionException("Path to certificate is required.", "certificate");

                Console.WriteLine("Starting CashShuffle server...");
                Server s = new Server(certPath);
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

        static void ShowHelp()
        {
            Console.WriteLine("Usage: dotnet run -- [OPTIONS]");
            options.WriteOptionDescriptions(Console.Out);
            Environment.Exit(0);
        }
    }
}
