using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RungerHeimdall.Analyzers.Architecture.Rules;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Architecture;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArchitectureAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor ConfigurationErrorDescriptor = DiagnosticFactory.Create(
        "HDL9002",
        "Invalid architecture configuration",
        "{0}",
        "Architecture",
        DiagnosticSeverity.Info);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Arch1001LayerDependencyRule.RuleDescriptor,
            ConfigurationErrorDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var found = ConfigLoader.TryLoad(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            out var config,
            out var error);

        if (error is not null)
        {
            context.RegisterCompilationEndAction(endContext =>
            {
                endContext.ReportDiagnostic(Diagnostic.Create(ConfigurationErrorDescriptor, Location.None, error));
            });
        }

        LayerIndex layers;
        DiagnosticSeverity? severityOverride = null;

        if (found && config is not null)
        {
            layers = config.BuildLayerIndex();
            severityOverride = ReadSeverity(config, "ARCH1001", "layerDependency");
        }
        else
        {
            layers = ConventionLayerResolver.Resolve(context.Compilation);
        }

        if (layers.IsEmpty)
        {
            return;
        }

        var engine = new RuleEngine(new IRule[]
        {
            new Arch1001LayerDependencyRule(layers, severityOverride),
        });

        if (engine.IsEmpty)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext =>
        {
            var engineContext = new SymbolContext(
                symbolContext.Symbol,
                symbolContext.Compilation,
                symbolContext.ReportDiagnostic,
                symbolContext.CancellationToken);

            engine.RunSymbolRules(engineContext);
        }, SymbolKind.NamedType);
    }

    private static DiagnosticSeverity? ReadSeverity(ArchitectureConfig config, string ruleId, string kind)
    {
        foreach (var rule in config.Rules)
        {
            var matchesId = rule.Id.Equals(ruleId, StringComparison.OrdinalIgnoreCase);
            var matchesKind = rule.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase);

            if (matchesId || matchesKind)
            {
                return ParseSeverity(rule.Severity);
            }
        }

        return null;
    }

    private static DiagnosticSeverity? ParseSeverity(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        switch (value!.ToLowerInvariant())
        {
            case "error":
                return DiagnosticSeverity.Error;
            case "warning":
                return DiagnosticSeverity.Warning;
            case "suggestion":
            case "info":
                return DiagnosticSeverity.Info;
            case "hidden":
                return DiagnosticSeverity.Hidden;
            default:
                return null;
        }
    }
}