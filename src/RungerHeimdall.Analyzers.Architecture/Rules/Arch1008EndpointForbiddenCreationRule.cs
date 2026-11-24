using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1008EndpointForbiddenCreationRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1008",
        "Endpoint layer must not create infrastructure types",
        "'{0}' must not create '{1}'. Resolve infrastructure through an abstraction instead.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly ScopeMatcher _scope;
    private readonly NamespacePattern[] _deniedNamespaces;
    private readonly DiagnosticSeverity? _severityOverride;
    private readonly ConcurrentDictionary<string, byte> _reported =
        new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

    public Arch1008EndpointForbiddenCreationRule(LayerIndex layers, RuleConfig config)
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

    public string Id => "ARCH1008";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (_scope.IsEmpty || _deniedNamespaces.Length == 0)
        {
            return;
        }

        if (context.Operation is not IObjectCreationOperation creation)
        {
            return;
        }

        var containingType = context.OwningSymbol?.ContainingType;
        if (containingType is null || !_scope.Matches(containingType))
        {
            return;
        }

        var targetType = creation.Constructor?.ContainingType;
        if (targetType is null)
        {
            return;
        }

        var namespaceName = SymbolFacts.GetNamespaceName(targetType);
        if (namespaceName is null || !IsDenied(namespaceName))
        {
            return;
        }

        var key = containingType.ToDisplayString() + "|" + targetType.ToDisplayString();
        if (!_reported.TryAdd(key, 0))
        {
            return;
        }

        var targetName = SymbolFacts.DisplayType(targetType);
        var location = creation.Syntax.GetLocation();
        var diagnostic = _severityOverride.HasValue
            ? Diagnostic.Create(
                RuleDescriptor,
                location,
                _severityOverride.Value,
                additionalLocations: null,
                properties: null,
                containingType.Name,
                targetName)
            : Diagnostic.Create(RuleDescriptor, location, containingType.Name, targetName);

        context.ReportDiagnostic(diagnostic);
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