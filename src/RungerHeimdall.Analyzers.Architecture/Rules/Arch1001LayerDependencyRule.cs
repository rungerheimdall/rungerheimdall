using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1001LayerDependencyRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1001",
        "Layer dependency direction",
        "'{0}' is in layer '{1}' but depends on '{2}' in layer '{3}'. Dependencies must flow inward.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly LayerIndex _layers;
    private readonly DiagnosticSeverity? _severityOverride;
    private readonly string? _fromLayer;
    private readonly string[] _deniedLayers;

    public Arch1001LayerDependencyRule(
        LayerIndex layers,
        DiagnosticSeverity? severityOverride = null,
        RuleConfig? config = null)
    {
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _severityOverride = severityOverride;
        _fromLayer = config?.From;
        _deniedLayers = config is null ? Array.Empty<string>() : config.Deny.ToArray();
    }

    public string Id => "ARCH1001";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type)
        {
            return;
        }

        var source = _layers.Match(type.ContainingNamespace);
        if (source is null)
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

            var target = _layers.Match(referenced.ContainingNamespace);
            if (target is null || !Violates(source, target))
            {
                continue;
            }

            var targetName = SymbolFacts.DisplayType(referenced);
            if (!reported.Add(targetName))
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
                    source.Name,
                    targetName,
                    target.Name)
                : Diagnostic.Create(
                    RuleDescriptor,
                    location,
                    type.Name,
                    source.Name,
                    targetName,
                    target.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool Violates(LayerMatch source, LayerMatch target)
    {
        if (_deniedLayers.Length > 0)
        {
            if (_fromLayer is not null
                && !source.Name.Equals(_fromLayer, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var denied in _deniedLayers)
            {
                if (denied.Equals(target.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        return target.Rank > source.Rank;
    }
}