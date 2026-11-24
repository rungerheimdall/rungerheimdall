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
    public static ImmutableArray<IRule> Create(
        ArchitectureConfig config,
        LayerIndex layers,
        string? projectDirectory = null)
    {
        var builder = ImmutableArray.CreateBuilder<IRule>();
        var layerRule = FindRule(config, "ARCH1001", "layerDependency");
        builder.Add(new Arch1001LayerDependencyRule(
            layers,
            SeverityParser.Parse(layerRule?.Severity),
            layerRule));

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

                case "namespacematchesfolder":
                    builder.Add(new Arch1004NamespaceMatchesFolderRule(layers, rule, projectDirectory));
                    break;

                case "requiredinterface":
                    builder.Add(new Arch1005RequiredInterfaceRule(layers, rule));
                    break;

                case "frameworkallowlist":
                    builder.Add(new Arch1006FrameworkAllowListRule(layers, rule));
                    break;

                case "nostaticmutablestate":
                    builder.Add(new Arch1007NoStaticMutableStateRule(layers, rule));
                    break;

                case "endpointforbiddencreation":
                    builder.Add(new Arch1008EndpointForbiddenCreationRule(layers, rule));
                    break;
            }
        }

        return builder.ToImmutable();
    }

    private static RuleConfig? FindRule(ArchitectureConfig config, string ruleId, string kind)
    {
        foreach (var rule in config.Rules)
        {
            if (rule.Id.Equals(ruleId, StringComparison.OrdinalIgnoreCase)
                || rule.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }

        return null;
    }
}