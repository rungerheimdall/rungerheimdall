using System;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1005RequiredInterfaceRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1005",
        "Repositories and services must be interfaces",
        "'{0}' must be an interface. Names ending with '{1}' are reserved for abstractions in this layer.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly ScopeMatcher _scope;
    private readonly string[] _suffixes;
    private readonly bool _mustBeInterface;
    private readonly DiagnosticSeverity? _severityOverride;

    public Arch1005RequiredInterfaceRule(LayerIndex layers, RuleConfig config)
    {
        _scope = new ScopeMatcher(layers, config.In);
        _suffixes = config.Suffix.ToArray();
        _mustBeInterface = config.MustBeInterface;
        _severityOverride = SeverityParser.Parse(config.Severity);
    }

    public string Id => "ARCH1005";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (_scope.IsEmpty || _suffixes.Length == 0 || !_mustBeInterface)
        {
            return;
        }

        if (context.Symbol is not INamedTypeSymbol type || !_scope.Matches(type))
        {
            return;
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            return;
        }

        var suffix = MatchSuffix(type.Name);
        if (suffix is null)
        {
            return;
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
                suffix)
            : Diagnostic.Create(RuleDescriptor, location, type.Name, suffix);

        context.ReportDiagnostic(diagnostic);
    }

    private string? MatchSuffix(string name)
    {
        foreach (var suffix in _suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return suffix;
            }
        }

        return null;
    }
}