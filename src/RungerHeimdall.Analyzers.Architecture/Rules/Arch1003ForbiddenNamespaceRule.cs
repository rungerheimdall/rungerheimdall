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

public sealed class Arch1003ForbiddenNamespaceRule : ISymbolRule, IOperationRule
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
    private readonly ConcurrentDictionary<string, byte> _reported =
        new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

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

        var containingKey = type.ToDisplayString();

        foreach (var referenced in TypeReferences.Enumerate(type))
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var namespaceName = SymbolFacts.GetNamespaceName(referenced);
            if (namespaceName is null || !IsDenied(namespaceName))
            {
                continue;
            }

            if (!MarkReported(containingKey, namespaceName))
            {
                continue;
            }

            Report(
                context.ReportDiagnostic,
                type.Locations.Length > 0 ? type.Locations[0] : Location.None,
                type.Name,
                namespaceName);
        }
    }

    public void Analyze(OperationContext context)
    {
        if (_scope.IsEmpty || _deniedNamespaces.Length == 0)
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

            var namespaceName = SymbolFacts.GetNamespaceName(symbol);
            if (namespaceName is null || !IsDenied(namespaceName))
            {
                continue;
            }

            if (!MarkReported(containingKey, namespaceName))
            {
                continue;
            }

            Report(
                context.ReportDiagnostic,
                context.Operation.Syntax.GetLocation(),
                containingType.Name,
                namespaceName);
        }
    }

    private bool MarkReported(string containingType, string target)
    {
        return _reported.TryAdd(containingType + "|" + target, 0);
    }

    private void Report(
        Action<Diagnostic> report,
        Location location,
        string containingName,
        string namespaceName)
    {
        var diagnostic = _severityOverride.HasValue
            ? Diagnostic.Create(
                RuleDescriptor,
                location,
                _severityOverride.Value,
                additionalLocations: null,
                properties: null,
                containingName,
                namespaceName)
            : Diagnostic.Create(RuleDescriptor, location, containingName, namespaceName);

        report(diagnostic);
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