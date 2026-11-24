using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RungerHeimdall.Cli;

internal static class BaselineCommand
{
    private const string BaselineFileName = "heimdall-baseline.json";

    public static async Task<int> CreateAsync(string[] args)
    {
        var target = WorkspaceAnalyzer.ResolveTarget(args);
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

        var entries = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var finding in findings)
        {
            entries.Add(finding.RuleId + "|" + finding.File);
        }

        var output = Path.Combine(Environment.CurrentDirectory, BaselineFileName);
        File.WriteAllText(output, BuildBaseline(entries), new UTF8Encoding(false));

        Console.WriteLine($"Created {output}");
        Console.WriteLine($"Entries: {entries.Count}");
        return 0;
    }

    private static string BuildBaseline(SortedSet<string> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"$schema\": \"https://rungerheimdall.github.io/rungerheimdall/schemas/heimdall-baseline.v1.json\",");
        builder.AppendLine("  \"version\": 1,");
        builder.AppendLine("  \"entries\": [");

        var first = true;
        foreach (var entry in entries)
        {
            var separator = entry.IndexOf('|');
            var ruleId = entry.Substring(0, separator);
            var file = entry.Substring(separator + 1);

            if (!first)
            {
                builder.AppendLine(",");
            }

            first = false;
            builder.Append($"    {{ \"ruleId\": \"{ruleId}\", \"file\": \"{Escape(file)}\" }}");
        }

        builder.AppendLine();
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}