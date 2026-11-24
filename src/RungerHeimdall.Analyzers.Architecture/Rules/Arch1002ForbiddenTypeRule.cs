using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1002ForbiddenTypeRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1002",
        "Forbidden type reference",
        "'{0}' must not reference '{1}'. This type is banned inside this layer.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly ScopeMatcher _scope;
    private readonly string[] _forbiddenTypes;
    private readonly DiagnosticSeverity? _severityOverride;

    public Arch1002ForbiddenTypeRule(LayerIndex layers, RuleConfig config)
    {
        _scope = new ScopeMatcher(layers, config.In);
        _forbiddenTypes = config.Types.ToArray();
        _severityOverride = SeverityParser.Parse(config.Severity);
    }

    public string Id => "ARCH1002";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (_scope.IsEmpty || _forbiddenTypes.Length == 0)
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

            if (!IsForbidden(referenced))
            {
                continue;
            }

            var name = referenced.ToDisplayString();
            if (!reported.Add(name))
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
                    name)
                : Diagnostic.Create(RuleDescriptor, location, type.Name, name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool IsForbidden(ITypeSymbol type)
    {
        var definition = SymbolFacts.GetDefinition(type) ?? type;
        var name = definition.Name;
        var fullName = definition.ToDisplayString();
        var metadataName = definition is INamedTypeSymbol named ? named.MetadataName : name;
        var namespaceName = SymbolFacts.GetNamespaceName(definition);

        foreach (var configured in _forbiddenTypes)
        {
            if (configured.Equals(name, StringComparison.Ordinal)
                || configured.Equals(fullName, StringComparison.Ordinal)
                || configured.Equals(metadataName, StringComparison.Ordinal))
            {
                return true;
            }

            if (namespaceName is not null
                && (configured.Equals(namespaceName + "." + name, StringComparison.Ordinal)
                    || configured.Equals(namespaceName + "." + metadataName, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }
}