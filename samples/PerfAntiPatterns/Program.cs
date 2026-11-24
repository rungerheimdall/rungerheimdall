using System;

namespace PerfAntiPatterns;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("PerfAntiPatterns is a compile-time sample.");
        Console.WriteLine("Build it to see the diagnostics:");
        Console.WriteLine("  dotnet build samples/PerfAntiPatterns");
        Console.WriteLine("Then aggregate them by method:");
        Console.WriteLine("  heimdall report samples/PerfAntiPatterns");
    }
}