using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using RungerHeimdall.Analyzers.Architecture;
using RungerHeimdall.Analyzers.Performance;

namespace RungerHeimdall.Cli;

internal static class BaselineCommand
{
    private const string BaselineFileName = "heimdall-baseline.json";

    public static async Task<int> CreateAsync(string[] args)
    {
        var target = ResolveTarget(args);
        var fullTarget = Path.GetFullPath(target);
        var root = File.Exists(fullTarget) ? Path.GetDirectoryName(fullTarget)! : fullTarget;

        var solutionPath = FindSolution(fullTarget);
        var projectPaths = solutionPath is null ? FindProjects(fullTarget) : Array.Empty<string>();

        if (solutionPath is null && projectPaths.Length == 0)
        {
            Console.Error.WriteLine($"No .sln or .csproj found at '{target}'.");
            return 1;
        }

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new ArchitectureAnalyzer(),
            new PerformanceAnalyzer());

        var entries = new SortedSet<string>(StringComparer.Ordinal);

        if (solutionPath is not null)
        {
            using var workspace = MSBuildWorkspace.Create();
            workspace.LoadMetadataForReferencedProjects = true;

            var solution = await workspace.OpenSolutionAsync(solutionPath);
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation is not null)
                {
                    await AnalyzeAsync(compilation, analyzers, entries, root);
                }
            }
        }
        else
        {
            foreach (var projectPath in projectPaths)
            {
                using var workspace = MSBuildWorkspace.Create();
                workspace.LoadMetadataForReferencedProjects = true;

                var project = await workspace.OpenProjectAsync(projectPath);
                var compilation = await project.GetCompilationAsync();
                if (compilation is not null)
                {
                    await AnalyzeAsync(compilation, analyzers, entries, root);
                }
            }
        }

        var output = Path.Combine(Environment.CurrentDirectory, BaselineFileName);
        File.WriteAllText(output, BuildBaseline(entries), new UTF8Encoding(false));

        Console.WriteLine($"Created {output}");
        Console.WriteLine($"Entries: {entries.Count}");
        return 0;
    }

    private static async Task AnalyzeAsync(
        Compilation compilation,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        SortedSet<string> entries,
        string root)
    {
        var withAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        foreach (var diagnostic in diagnostics)
        {
            if (!IsRule(diagnostic.Id))
            {
                continue;
            }

            var path = diagnostic.Location.GetLineSpan().Path;
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            entries.Add(diagnostic.Id + "|" + relative);
        }
    }

    private static string ResolveTarget(string[] args)
    {
        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument.Equals("create", StringComparison.OrdinalIgnoreCase)
                || argument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            return argument;
        }

        return Environment.CurrentDirectory;
    }

    private static string? FindSolution(string path)
    {
        if (File.Exists(path))
        {
            return path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        if (!Directory.Exists(path))
        {
            return null;
        }

        var solutions = Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly);
        return solutions.Length > 0 ? solutions[0] : null;
    }

    private static string[] FindProjects(string path)
    {
        if (File.Exists(path))
        {
            return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                ? new[] { path }
                : Array.Empty<string>();
        }

        if (!Directory.Exists(path))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
            .Where(IsNotBuildOutput)
            .ToArray();
    }

    private static bool IsNotBuildOutput(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) < 0
            && normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static bool IsRule(string id)
    {
        return id.StartsWith("ARCH", StringComparison.Ordinal)
            || id.StartsWith("PERF", StringComparison.Ordinal);
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