using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RungerHeimdall.Cli;

internal static class ReportCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var target = WorkspaceAnalyzer.ResolveTarget(args);
        var output = ResolveOutput(args);

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

        var markdown = Build(findings);
        var methods = findings
            .Select(finding => finding.Method)
            .Distinct(StringComparer.Ordinal)
            .Count();

        if (output == "-")
        {
            Console.WriteLine(markdown);
            return 0;
        }

        File.WriteAllText(output, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Created {output}");
        Console.WriteLine($"Findings: {findings.Count}");
        Console.WriteLine($"Methods: {methods}");
        return 0;
    }

    private static string Build(List<AnalysisFinding> findings)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Hot path report");
        builder.AppendLine();

        if (findings.Count == 0)
        {
            builder.AppendLine("No findings. Nothing to optimise.");
            return builder.ToString();
        }

        var groups = findings
            .GroupBy(finding => string.IsNullOrEmpty(finding.Method) ? "<unknown>" : finding.Method)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToList();

        builder.AppendLine($"Total findings: {findings.Count}");
        builder.AppendLine();
        builder.AppendLine("| Method | Findings | Rules |");
        builder.AppendLine("|--------|----------|-------|");

        foreach (var group in groups)
        {
            var rules = group
                .Select(finding => finding.RuleId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal);

            builder.AppendLine($"| {Escape(group.Key)} | {group.Count()} | {string.Join(", ", rules)} |");
        }

        builder.AppendLine();

        foreach (var group in groups)
        {
            builder.AppendLine($"## {group.Key}");
            builder.AppendLine();
            builder.AppendLine("| Rule | Severity | File | Line | Message |");
            builder.AppendLine("|------|----------|------|------|---------|");

            foreach (var finding in group
                .OrderBy(item => item.File, StringComparer.Ordinal)
                .ThenBy(item => item.Line))
            {
                var file = string.IsNullOrEmpty(finding.File) ? "—" : finding.File;
                var line = finding.Line == 0 ? "—" : finding.Line.ToString();
                builder.AppendLine(
                    $"| {finding.RuleId} | {finding.Severity} | {file} | {line} | {Escape(finding.Message)} |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", string.Empty).Replace("\n", " ");
    }

    private static string ResolveOutput(string[] args)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (args[index] is "--output" or "-o")
            {
                return args[index + 1];
            }
        }

        return Path.Combine(Environment.CurrentDirectory, "hotpath-report.md");
    }
}