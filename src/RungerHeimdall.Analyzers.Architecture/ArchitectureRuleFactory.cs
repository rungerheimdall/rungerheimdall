using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Analyzers.Architecture.Rules;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Architecture;

public static class ArchitectureRuleFactory
{
    public static ImmutableArray<IRule> Create(ArchitectureConfig config, LayerIndex layers)
    {
        var builder = ImmutableArray.CreateBuilder<IRule>();
        builder.Add(new Arch1001LayerDependencyRule(
            layers,
            ReadSeverity(config, "ARCH1001", "layerDependency")));

        foreach (var rule in config.Rules)
        {
            switch (rule.Kind.ToLowerInvariant())
            {
                case "forbiddentype":
                    builder.Add(new Arch1002ForbiddenTypeRule(layers, rule));
                    break;

                case "forbiddennamespace":
                    builder.Add(new Arch1003ForbiddenNamespaceRule(layers, rule));
                    break;
            }
        }

        return builder.ToImmutable();
    }

    public static DiagnosticSeverity? ReadSeverity(ArchitectureConfig config, string ruleId, string kind)
    {
        foreach (var rule in config.Rules)
        {
            if (rule.Id.Equals(ruleId, StringComparison.OrdinalIgnoreCase)
                || rule.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))
            {
                return SeverityParser.Parse(rule.Severity);
            }
        }

        return null;
    }
}