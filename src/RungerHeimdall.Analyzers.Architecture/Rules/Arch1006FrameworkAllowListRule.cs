using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1006FrameworkAllowListRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1006",
        "Layer must not reference disallowed frameworks",
        "'{0}' references '{1}' from '{2}'. Only allow-listed namespaces may be used in this layer.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private static readonly string[] DefaultAllowList =
    {
        "System",
        "System.*",
        "Microsoft",
        "Microsoft.*",
    };

    private readonly LayerIndex _layers;
    private readonly ScopeMatcher _scope;
    private readonly NamespacePattern[] _allowed;
    private readonly DiagnosticSeverity? _severityOverride;

    public Arch1006FrameworkAllowListRule(LayerIndex layers, RuleConfig config)
    {
        _layers = layers;
        _scope = new ScopeMatcher(layers, config.In);

        IReadOnlyList<string> values = config.Allow.Count > 0 ? config.Allow : DefaultAllowList;
        _allowed = new NamespacePattern[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            _allowed[index] = new NamespacePattern(values[index]);
        }

        _severityOverride = SeverityParser.Parse(config.Severity);
    }

    public string Id => "ARCH1006";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (_scope.IsEmpty)
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

            if (_layers.Match(referenced.ContainingNamespace) is not null)
            {
                continue;
            }

            if (IsAllowed(namespaceName))
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
                    referenced.ToDisplayString(),
                    namespaceName)
                : Diagnostic.Create(RuleDescriptor, location, type.Name, referenced.ToDisplayString(), namespaceName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool IsAllowed(string namespaceName)
    {
        foreach (var pattern in _allowed)
        {
            if (pattern.Matches(namespaceName))
            {
                return true;
            }
        }

        return false;
    }
}