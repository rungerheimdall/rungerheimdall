using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace RungerHeimdall.Cli;

internal static class Program
{
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
                Console.WriteLine($"heimdall {GetVersion()}");
                return 0;

            case "--help":
            case "-h":
                return PrintHelp();

            case "init":
                return Init(args);

            case "baseline":
                if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
                {
                    Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
                }

                return BaselineCommand.CreateAsync(args).GetAwaiter().GetResult();

            case "doctor":
                return Doctor();

            default:
                Console.Error.WriteLine($"Unknown command: {args[0]}");
                return PrintHelp();
        }
    }

    private static int Init(string[] args)
    {
        var strict = false;
        foreach (var argument in args)
        {
            if (argument.Equals("--strict", StringComparison.OrdinalIgnoreCase))
            {
                strict = true;
            }
        }

        var root = DetectRootNamespace() ?? "MyApp";
        var target = Path.Combine(Environment.CurrentDirectory, "architecture.json");
        if (File.Exists(target))
        {
            target += ".new";
        }

        File.WriteAllText(target, BuildConfig(root, strict), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine($"Created {target}");
        Console.WriteLine($"Layer root: {root}");
        Console.WriteLine("Fill in ARCH1002 types if you want to ban specific framework types.");
        Console.WriteLine("Next: dotnet build");
        return 0;
    }

    private static string? DetectRootNamespace()
    {
        var files = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            return null;
        }

        return Path.GetFileNameWithoutExtension(files[0]);
    }

    private static string BuildConfig(string root, bool strict)
    {
        var severity = strict ? "error" : "warning";

        return $$"""
{
  "$schema": "https://rungerheimdall.github.io/rungerheimdall/schemas/architecture.v1.json",
  "version": 1,
  "layers": [
    { "name": "Domain", "namespaces": ["{{root}}.Domain", "{{root}}.Domain.*"], "rank": 0 },
    { "name": "Application", "namespaces": ["{{root}}.Application.*"], "rank": 1 },
    { "name": "Infrastructure", "namespaces": ["{{root}}.Infrastructure.*"], "rank": 2 },
    { "name": "Api", "namespaces": ["{{root}}.Api.*"], "rank": 3 }
  ],
  "rules": [
    { "id": "ARCH1001", "kind": "layerDependency", "from": "Domain", "deny": ["Infrastructure", "Api"], "severity": "{{severity}}" },
    { "id": "ARCH1002", "kind": "forbiddenType", "in": ["Api", "Application"], "types": [], "severity": "{{severity}}" },
    { "id": "ARCH1003", "kind": "forbiddenNamespace", "in": ["Domain"], "deny": ["{{root}}.Infrastructure.*"], "severity": "{{severity}}" },
    { "id": "ARCH1004", "kind": "namespaceMatchesFolder", "in": ["Domain", "Application"], "severity": "{{severity}}" },
    { "id": "ARCH1005", "kind": "requiredInterface", "in": ["Domain"], "suffix": ["Repository", "Service"], "mustBeInterface": true, "severity": "{{severity}}" },
    { "id": "ARCH1006", "kind": "frameworkAllowList", "in": ["Domain"], "allow": ["System", "System.*", "Microsoft", "Microsoft.*"], "severity": "{{severity}}" },
    { "id": "ARCH1007", "kind": "noStaticMutableState", "in": ["Domain", "Application"], "severity": "{{severity}}" },
    { "id": "ARCH1008", "kind": "endpointForbiddenCreation", "in": ["Api"], "deny": ["{{root}}.Infrastructure.*"], "severity": "{{severity}}" }
  ],
  "exemptions": []
}
""";
    }

    private static string GetVersion()
    {
        var informational = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrEmpty(informational))
        {
            var separator = informational!.IndexOf('+');
            return separator >= 0 ? informational.Substring(0, separator) : informational;
        }

        return typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
    }

    private static int PrintHelp()
    {
        Console.WriteLine($"heimdall {GetVersion()}");
        Console.WriteLine();
        Console.WriteLine("Usage: heimdall <command>");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  init       Create architecture.json from the current project name.");
        Console.WriteLine("             Use --strict to emit error-severity rules.");
        Console.WriteLine("  baseline   Create heimdall-baseline.json from the current solution or project.");
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