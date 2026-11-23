using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

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

    public Arch1001LayerDependencyRule(LayerIndex layers, DiagnosticSeverity? severityOverride = null)
    {
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _severityOverride = severityOverride;
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

        foreach (var referenced in EnumerateReferencedTypes(type))
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var target = _layers.Match(referenced.ContainingNamespace);
            if (target is null || target.Rank <= source.Rank)
            {
                continue;
            }

            var targetName = referenced.ToDisplayString();
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

    private static IEnumerable<ITypeSymbol> EnumerateReferencedTypes(INamedTypeSymbol type)
    {
        if (type.BaseType is not null)
        {
            foreach (var item in Flatten(type.BaseType))
            {
                yield return item;
            }
        }

        foreach (var @interface in type.Interfaces)
        {
            foreach (var item in Flatten(@interface))
            {
                yield return item;
            }
        }

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            switch (member)
            {
                case IFieldSymbol field:
                    foreach (var item in Flatten(field.Type))
                    {
                        yield return item;
                    }

                    break;

                case IPropertySymbol property:
                    foreach (var item in Flatten(property.Type))
                    {
                        yield return item;
                    }

                    break;

                case IEventSymbol @event:
                    foreach (var item in Flatten(@event.Type))
                    {
                        yield return item;
                    }

                    break;

                case IMethodSymbol method:
                    foreach (var item in Flatten(method.ReturnType))
                    {
                        yield return item;
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        foreach (var item in Flatten(parameter.Type))
                        {
                            yield return item;
                        }
                    }

                    break;
            }
        }
    }

    private static IEnumerable<ITypeSymbol> Flatten(ITypeSymbol type)
    {
        yield return type;

        if (type is INamedTypeSymbol named)
        {
            foreach (var argument in named.TypeArguments)
            {
                foreach (var item in Flatten(argument))
                {
                    yield return item;
                }
            }
        }
        else if (type is IArrayTypeSymbol array)
        {
            foreach (var item in Flatten(array.ElementType))
            {
                yield return item;
            }
        }
    }
}