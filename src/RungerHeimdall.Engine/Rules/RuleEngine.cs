using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Rules;

public sealed class RuleEngine
{
    private readonly ImmutableArray<ISymbolRule> _symbolRules;
    private readonly ImmutableArray<IOperationRule> _operationRules;

    public RuleEngine(IEnumerable<IRule> rules)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        var symbolRules = ImmutableArray.CreateBuilder<ISymbolRule>();
        var operationRules = ImmutableArray.CreateBuilder<IOperationRule>();

        foreach (var rule in rules)
        {
            if (rule is ISymbolRule symbolRule)
            {
                symbolRules.Add(symbolRule);
            }

            if (rule is IOperationRule operationRule)
            {
                operationRules.Add(operationRule);
            }
        }

        _symbolRules = symbolRules.ToImmutable();
        _operationRules = operationRules.ToImmutable();
        SupportedDiagnostics = CollectDescriptors(_symbolRules, _operationRules);
    }

    public bool IsEmpty => _symbolRules.IsEmpty && _operationRules.IsEmpty;

    public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public void RunSymbolRules(SymbolContext context)
    {
        foreach (var rule in _symbolRules)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            rule.Analyze(context);
        }
    }

    public void RunOperationRules(OperationContext context)
    {
        foreach (var rule in _operationRules)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            rule.Analyze(context);
        }
    }

    private static ImmutableArray<DiagnosticDescriptor> CollectDescriptors(
        ImmutableArray<ISymbolRule> symbolRules,
        ImmutableArray<IOperationRule> operationRules)
    {
        var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in symbolRules)
        {
            if (seen.Add(rule.Descriptor.Id))
            {
                builder.Add(rule.Descriptor);
            }
        }

        foreach (var rule in operationRules)
        {
            if (seen.Add(rule.Descriptor.Id))
            {
                builder.Add(rule.Descriptor);
            }
        }

        return builder.ToImmutable();
    }
}