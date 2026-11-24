using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using RungerHeimdall.Analyzers.Architecture;
using RungerHeimdall.Analyzers.Performance;

namespace RungerHeimdall.Cli;

internal sealed class AnalysisFinding
{
    public string RuleId { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string File { get; init; } = string.Empty;

    public string AbsoluteFile { get; init; } = string.Empty;

    public int Line { get; init; }

    public int Column { get; init; }

    public string Severity { get; init; } = string.Empty;

    public string Method { get; init; } = string.Empty;
}

internal static class WorkspaceAnalyzer
{
    public static async Task<List<AnalysisFinding>> RunAsync(string target)
    {
        var fullTarget = Path.GetFullPath(target);
        var root = File.Exists(fullTarget) ? Path.GetDirectoryName(fullTarget)! : fullTarget;

        var solutionPath = FindSolution(fullTarget);
        var projectPaths = solutionPath is null ? FindProjects(fullTarget) : Array.Empty<string>();

        if (solutionPath is null && projectPaths.Length == 0)
        {
            throw new InvalidOperationException($"No .sln or .csproj found at '{target}'.");
        }

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new ArchitectureAnalyzer(),
            new PerformanceAnalyzer());

        var findings = new List<AnalysisFinding>();

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
                    await CollectAsync(compilation, analyzers, findings, root, ProjectDirectory(project));
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
                    await CollectAsync(compilation, analyzers, findings, root, ProjectDirectory(project));
                }
            }
        }

        findings.Sort((left, right) =>
        {
            var byFile = string.Compare(left.File, right.File, StringComparison.Ordinal);
            if (byFile != 0)
            {
                return byFile;
            }

            var byLine = left.Line.CompareTo(right.Line);
            return byLine != 0 ? byLine : string.Compare(left.RuleId, right.RuleId, StringComparison.Ordinal);
        });

        return findings;
    }

    public static string ResolveTarget(string[] args)
    {
        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];

            if (argument.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (argument is "--format" or "-f" or "--output" or "-o")
            {
                index++;
                continue;
            }

            if (argument.StartsWith("--", StringComparison.Ordinal)
                || argument.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            return argument;
        }

        return Environment.CurrentDirectory;
    }

    private static async Task CollectAsync(
        Compilation compilation,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        List<AnalysisFinding> findings,
        string root,
        string? projectDirectory)
    {
        var options = CliAnalyzerOptions.Create(projectDirectory);
        var withAnalyzers = compilation.WithAnalyzers(analyzers, options);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        foreach (var diagnostic in diagnostics)
        {
            if (!IsRule(diagnostic.Id))
            {
                continue;
            }

            var span = diagnostic.Location.GetLineSpan();
            var hasLocation = !string.IsNullOrEmpty(span.Path);

            findings.Add(new AnalysisFinding
            {
                RuleId = diagnostic.Id,
                Message = diagnostic.GetMessage(),
                File = hasLocation ? Path.GetRelativePath(root, span.Path).Replace('\\', '/') : string.Empty,
                AbsoluteFile = hasLocation ? span.Path : string.Empty,
                Line = hasLocation ? span.StartLinePosition.Line + 1 : 0,
                Column = hasLocation ? span.StartLinePosition.Character + 1 : 0,
                Severity = diagnostic.Severity.ToString(),
                Method = hasLocation ? ResolveMethod(compilation, diagnostic) : "<project>",
            });
        }
    }

    private static string ResolveMethod(Compilation compilation, Diagnostic diagnostic)
    {
        var tree = diagnostic.Location.SourceTree;
        if (tree is null)
        {
            return "<project>";
        }

        var model = compilation.GetSemanticModel(tree);
        var symbol = model.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start);

        switch (symbol)
        {
            case null:
                return "<unknown>";

            case IMethodSymbol method:
                return method.ToDisplayString();

            default:
                return symbol.ContainingType?.ToDisplayString()
                    ?? symbol.ContainingSymbol?.ToDisplayString()
                    ?? symbol.ToDisplayString();
        }
    }

    private static string? ProjectDirectory(Project project)
    {
        return project.FilePath is null ? null : Path.GetDirectoryName(project.FilePath);
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
            || id.StartsWith("PERF", StringComparison.Ordinal)
            || id.StartsWith("HDL9", StringComparison.Ordinal);
    }
}