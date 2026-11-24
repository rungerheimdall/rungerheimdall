using System;
using System.Collections.Generic;
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
            Console.WriteLine(
                $"{finding.File}:{finding.Line}:{finding.Column} {finding.Severity.ToLowerInvariant()} {finding.RuleId} {finding.Message}");
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
            Console.WriteLine(
                $"| {finding.RuleId} | {finding.Severity} | {finding.File} | {finding.Line} | {Escape(finding.Message)} |");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {findings.Count}");
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