using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RungerHeimdall.Cli;

internal static class Program
{
    private const string Version = "0.1.0-preview.1";

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return PrintHelp();
        }

        switch (args[0])
        {
            case "--version":
            case "-v":
                Console.WriteLine($"heimdall {Version}");
                return 0;

            case "--help":
            case "-h":
                return PrintHelp();

            case "doctor":
                return Doctor();

            default:
                Console.Error.WriteLine($"Unknown command: {args[0]}");
                return PrintHelp();
        }
    }

    private static int PrintHelp()
    {
        Console.WriteLine($"heimdall {Version}");
        Console.WriteLine();
        Console.WriteLine("Usage: heimdall <command>");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  doctor     Check the local environment and configuration.");
        Console.WriteLine("  --version  Print the version.");
        Console.WriteLine("  --help     Print this help.");
        return 0;
    }

    private static int Doctor()
    {
        Console.WriteLine("RungerHeimdall doctor");
        Console.WriteLine($"  .NET          : {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  OS            : {RuntimeInformation.OSDescription}");
        Console.WriteLine($"  Architecture  : {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"  Working dir   : {Environment.CurrentDirectory}");

        var config = Path.Combine(Environment.CurrentDirectory, "architecture.json");
        Console.WriteLine($"  architecture.json : {(File.Exists(config) ? config : "not found (convention mode)")}");

        return 0;
    }
}