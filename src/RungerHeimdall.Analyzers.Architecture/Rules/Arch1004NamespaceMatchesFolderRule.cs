using System;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Sources;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1004NamespaceMatchesFolderRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1004",
        "Namespace must match folder",
        "'{0}' is declared in namespace '{1}'. The folder implies namespace '{2}'.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly ScopeMatcher _scope;
    private readonly string? _projectDirectory;
    private readonly DiagnosticSeverity? _severityOverride;

    public Arch1004NamespaceMatchesFolderRule(LayerIndex layers, RuleConfig config, string? projectDirectory)
    {
        _scope = new ScopeMatcher(layers, config.In);
        _projectDirectory = projectDirectory;
        _severityOverride = SeverityParser.Parse(config.Severity);
    }

    public string Id => "ARCH1004";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type)
        {
            return;
        }

        if (!_scope.IsEmpty && !_scope.Matches(type))
        {
            return;
        }

        var location = type.Locations.Length > 0 ? type.Locations[0] : null;
        if (location is null || !location.IsInSource)
        {
            return;
        }

        var tree = location.SourceTree;
        if (tree is null || SourceGuard.IsGenerated(tree))
        {
            return;
        }

        var namespaceSymbol = type.ContainingNamespace;
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return;
        }

        var actual = namespaceSymbol.ToDisplayString();
        var expected = GetExpectedNamespace(tree.FilePath);
        if (expected is null || Matches(actual, expected))
        {
            return;
        }

        var diagnostic = _severityOverride.HasValue
            ? Diagnostic.Create(
                RuleDescriptor,
                location,
                _severityOverride.Value,
                additionalLocations: null,
                properties: null,
                type.Name,
                actual,
                expected)
            : Diagnostic.Create(RuleDescriptor, location, type.Name, actual, expected);

        context.ReportDiagnostic(diagnostic);
    }

    private string? GetExpectedNamespace(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var normalized = filePath!.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return null;
        }

        var directory = normalized.Substring(0, lastSlash);
        if (directory.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(_projectDirectory))
        {
            var root = _projectDirectory!.Replace('\\', '/').TrimEnd('/');
            if (directory.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                var relative = directory.Substring(root.Length + 1);
                if (relative.Length == 0)
                {
                    return null;
                }

                if (IsBuildOutput(relative))
                {
                    return null;
                }

                return relative.Replace('/', '.');
            }
        }

        var segment = directory.Substring(directory.LastIndexOf('/') + 1);
        if (segment.Length == 0 || IsBuildOutput(segment))
        {
            return null;
        }

        return segment;
    }

    private static bool IsBuildOutput(string path)
    {
        return path.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || path.Equals("bin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(string actual, string expected)
    {
        if (actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return actual.EndsWith("." + expected, StringComparison.OrdinalIgnoreCase);
    }
}