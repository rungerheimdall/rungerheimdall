using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Operations;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1002ForbiddenTypeRule : ISymbolRule, IOperationRule
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
    private readonly ConcurrentDictionary<string, byte> _reported =
        new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

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

        var containingKey = type.ToDisplayString();

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

            var targetName = SymbolFacts.DisplayType(referenced);
            if (!MarkReported(containingKey, targetName))
            {
                continue;
            }

            Report(
                context.ReportDiagnostic,
                type.Locations.Length > 0 ? type.Locations[0] : Location.None,
                type.Name,
                targetName);
        }
    }

    public void Analyze(OperationContext context)
    {
        if (_scope.IsEmpty || _forbiddenTypes.Length == 0)
        {
            return;
        }

        var containingType = context.OwningSymbol?.ContainingType;
        if (containingType is null || !_scope.Matches(containingType))
        {
            return;
        }

        var containingKey = containingType.ToDisplayString();

        foreach (var symbol in OperationReferences.Enumerate(context.Operation))
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var targetType = GetReferencedType(symbol);
            if (targetType is null || !IsForbidden(targetType))
            {
                continue;
            }

            var targetName = SymbolFacts.DisplayType(targetType);
            if (!MarkReported(containingKey, targetName))
            {
                continue;
            }

            Report(
                context.ReportDiagnostic,
                context.Operation.Syntax.GetLocation(),
                containingType.Name,
                targetName);
        }
    }

    private bool MarkReported(string containingType, string targetType)
    {
        return _reported.TryAdd(containingType + "|" + targetType, 0);
    }

    private static ITypeSymbol? GetReferencedType(ISymbol symbol)
    {
        switch (symbol)
        {
            case INamedTypeSymbol type:
                return type;

            case IMethodSymbol method:
                return method.ContainingType;

            case IFieldSymbol field:
                return field.ContainingType;

            case IPropertySymbol property:
                return property.ContainingType;

            case IEventSymbol @event:
                return @event.ContainingType;

            default:
                return symbol.ContainingType;
        }
    }

    private void Report(
        Action<Diagnostic> report,
        Location location,
        string containingName,
        string targetName)
    {
        var diagnostic = _severityOverride.HasValue
            ? Diagnostic.Create(
                RuleDescriptor,
                location,
                _severityOverride.Value,
                additionalLocations: null,
                properties: null,
                containingName,
                targetName)
            : Diagnostic.Create(RuleDescriptor, location, containingName, targetName);

        report(diagnostic);
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