using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1003ForbiddenNamespaceRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1003",
        "Forbidden namespace reference",
        "'{0}' must not reference namespace '{1}'. This dependency is banned inside this layer.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly ScopeMatcher _scope;
    private readonly NamespacePattern[] _deniedNamespaces;
    private readonly DiagnosticSeverity? _severityOverride;

    public Arch1003ForbiddenNamespaceRule(LayerIndex layers, RuleConfig config)
    {
        _scope = new ScopeMatcher(layers, config.In);

        var patterns = new List<NamespacePattern>();
        foreach (var value in config.Deny)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                patterns.Add(new NamespacePattern(value));
            }
        }

        _deniedNamespaces = patterns.ToArray();
        _severityOverride = SeverityParser.Parse(config.Severity);
    }

    public string Id => "ARCH1003";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (_scope.IsEmpty || _deniedNamespaces.Length == 0)
        {
            return;
        }

        if (context.Symbol is not INamedTypeSymbol type || !_scope.Matches(type))
        {
            return;
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var referenced in TypeReferences.Enumerate(type))
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var namespaceName = SymbolFacts.GetNamespaceName(referenced);
            if (namespaceName is null)
            {
                continue;
            }

            if (!IsDenied(namespaceName))
            {
                continue;
            }

            if (!reported.Add(namespaceName))
            {
                continue;
            }

            var location = type.Locations.Length > 0 ? type.Locations[0] : Location.None;
            var diagnostic = _severityOverride.HasValue
                ? Diagnostic.Create(
                    RuleDescriptor,
                    location,
                    _severityOverride.Value,
                    additionalLocations: null,
                    properties: null,
                    type.Name,
                    namespaceName)
                : Diagnostic.Create(RuleDescriptor, location, type.Name, namespaceName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool IsDenied(string namespaceName)
    {
        foreach (var pattern in _deniedNamespaces)
        {
            if (pattern.Matches(namespaceName))
            {
                return true;
            }
        }

        return false;
    }
}