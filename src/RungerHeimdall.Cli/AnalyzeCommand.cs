using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RungerHeimdall.Cli;

internal static class AnalyzeCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var target = WorkspaceAnalyzer.ResolveTarget(args);
        var format = ResolveFormat(args);

        List<AnalysisFinding> findings;

        try
        {
            findings = await WorkspaceAnalyzer.RunAsync(target);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        switch (format)
        {
            case "json":
                PrintJson(findings);
                break;

            case "markdown":
                PrintMarkdown(findings);
                break;

            case "sarif":
                PrintSarif(findings);
                break;

            default:
                PrintConsole(findings);
                break;
        }

        return 0;
    }

    private static string ResolveFormat(string[] args)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (args[index] is "--format" or "-f")
            {
                return args[index + 1].ToLowerInvariant();
            }
        }

        return "console";
    }

    private static void PrintConsole(List<AnalysisFinding> findings)
    {
        foreach (var finding in findings)
        {
            var location = string.IsNullOrEmpty(finding.File)
                ? "<project>"
                : $"{finding.File}:{finding.Line}:{finding.Column}";

            Console.WriteLine(
                $"{location} {finding.Severity.ToLowerInvariant()} {finding.RuleId} {finding.Message}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {findings.Count}");
    }

    private static void PrintJson(List<AnalysisFinding> findings)
    {
        var payload = JsonSerializer.Serialize(
            new { findings, total = findings.Count },
            new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine(payload);
    }

    private static void PrintMarkdown(List<AnalysisFinding> findings)
    {
        Console.WriteLine("| Rule | Severity | File | Line | Message |");
        Console.WriteLine("|------|----------|------|------|---------|");

        foreach (var finding in findings)
        {
            var file = string.IsNullOrEmpty(finding.File) ? "—" : finding.File;
            var line = finding.Line == 0 ? "—" : finding.Line.ToString();

            Console.WriteLine(
                $"| {finding.RuleId} | {finding.Severity} | {file} | {line} | {Escape(finding.Message)} |");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {findings.Count}");
    }

    private static void PrintSarif(List<AnalysisFinding> findings)
    {
        var rules = findings
            .Select(finding => finding.RuleId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => (object)new Dictionary<string, object?>
            {
                ["id"] = id,
                ["shortDescription"] = new Dictionary<string, object?> { ["text"] = id },
            })
            .ToList();

        var results = findings.Select(finding =>
        {
            var result = new Dictionary<string, object?>
            {
                ["ruleId"] = finding.RuleId,
                ["level"] = MapSarifLevel(finding.Severity),
                ["message"] = new Dictionary<string, object?> { ["text"] = finding.Message },
            };

            if (!string.IsNullOrEmpty(finding.File))
            {
                result["locations"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["physicalLocation"] = new Dictionary<string, object?>
                        {
                            ["artifactLocation"] = new Dictionary<string, object?>
                            {
                                ["uri"] = SarifUri(finding),
                                ["uriBaseId"] = "%SRCROOT%",
                            },
                            ["region"] = new Dictionary<string, object?>
                            {
                                ["startLine"] = finding.Line,
                                ["startColumn"] = finding.Column,
                            },
                        },
                    },
                };
            }

            return (object)result;
        }).ToList();

        var payload = new Dictionary<string, object?>
        {
            ["$schema"] = "https://json.schemastore.org/sarif-2.1.0.json",
            ["version"] = "2.1.0",
            ["runs"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["tool"] = new Dictionary<string, object?>
                    {
                        ["driver"] = new Dictionary<string, object?>
                        {
                            ["name"] = "RungerHeimdall",
                            ["informationUri"] = "https://github.com/rungerheimdall/rungerheimdall",
                            ["version"] = GetVersion(),
                            ["rules"] = rules,
                        },
                    },
                    ["results"] = results,
                },
            },
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string SarifUri(AnalysisFinding finding)
    {
        if (string.IsNullOrEmpty(finding.AbsoluteFile))
        {
            return finding.File;
        }

        try
        {
            return Path.GetRelativePath(Environment.CurrentDirectory, finding.AbsoluteFile)
                .Replace('\\', '/');
        }
        catch (ArgumentException)
        {
            return finding.File;
        }
    }

    private static string MapSarifLevel(string severity)
    {
        switch (severity.ToLowerInvariant())
        {
            case "error":
                return "error";

            case "warning":
                return "warning";

            case "hidden":
                return "none";

            default:
                return "note";
        }
    }

    private static string GetVersion()
    {
        var informational = typeof(AnalyzeCommand).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrEmpty(informational))
        {
            var separator = informational!.IndexOf('+');
            return separator >= 0 ? informational.Substring(0, separator) : informational;
        }

        return typeof(AnalyzeCommand).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '|' => "\\|",
                '\r' => string.Empty,
                '\n' => " ",
                _ => character.ToString(),
            });
        }

        return builder.ToString();
    }
}