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

    internal static readonly DiagnosticDescriptor BaselineErrorDescriptor = DiagnosticFactory.Create(
        "HDL9003",
        "Invalid baseline file",
        "{0}",
        "Architecture",
        DiagnosticSeverity.Info);

    internal static readonly DiagnosticDescriptor ExpiredExemptionDescriptor = DiagnosticFactory.Create(
        "HDL9001",
        "Expired architecture exemption",
        "The exemption for '{0}' expired on {1}. Remove it or extend the expiry.",
        "Architecture",
        DiagnosticSeverity.Info);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Arch1001LayerDependencyRule.RuleDescriptor,
            Arch1002ForbiddenTypeRule.RuleDescriptor,
            Arch1003ForbiddenNamespaceRule.RuleDescriptor,
            Arch1004NamespaceMatchesFolderRule.RuleDescriptor,
            Arch1005RequiredInterfaceRule.RuleDescriptor,
            Arch1006FrameworkAllowListRule.RuleDescriptor,
            Arch1007NoStaticMutableStateRule.RuleDescriptor,
            Arch1008EndpointForbiddenCreationRule.RuleDescriptor,
            ConfigurationErrorDescriptor,
            BaselineErrorDescriptor,
            ExpiredExemptionDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(ReportConfigurationDiagnostics);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void ReportConfigurationDiagnostics(CompilationAnalysisContext context)
    {
        var found = ConfigLoader.TryLoad(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            out var config,
            out var error);

        if (error is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(ConfigurationErrorDescriptor, Location.None, error));
        }

        var baseline = BaselineStore.Load(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            ReadProjectDirectory(context.Options),
            out var baselineError);

        if (baselineError is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(BaselineErrorDescriptor, Location.None, baselineError));
        }

        if (!found || config is null)
        {
            return;
        }

        var exemptions = ExemptionStore.Create(config.Exemptions, DateTime.UtcNow.Date);
        foreach (var exemption in exemptions.GetExpired())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ExpiredExemptionDescriptor,
                Location.None,
                exemption.Target,
                exemption.ExpiresOn));
        }

        _ = baseline;
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var found = ConfigLoader.TryLoad(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            out var config,
            out _);

        var exemptions = found && config is not null
            ? ExemptionStore.Create(config.Exemptions, DateTime.UtcNow.Date)
            : ExemptionStore.Empty;

        var projectDirectory = ReadProjectDirectory(context.Options);
        var baseline = BaselineStore.Load(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            projectDirectory,
            out _);

        LayerIndex layers;
        ImmutableArray<IRule> rules;

        if (found && config is not null)
        {
            layers = config.BuildLayerIndex();
            rules = ArchitectureRuleFactory.Create(config, layers, projectDirectory);
        }
        else
        {
            layers = ConventionLayerResolver.Resolve(context.Compilation);
            rules = ImmutableArray.Create<IRule>(new Arch1001LayerDependencyRule(layers));
        }

        if (rules.IsEmpty)
        {
            return;
        }

        var engine = new RuleEngine(rules);
        if (engine.IsEmpty)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext =>
        {
            if (exemptions.IsExempt(symbolContext.Symbol))
            {
                return;
            }

            var report = new Action<Diagnostic>(diagnostic =>
            {
                if (baseline.IsSuppressed(diagnostic.Id, symbolContext.Symbol, diagnostic.Location))
                {
                    return;
                }

                symbolContext.ReportDiagnostic(diagnostic);
            });

            var engineContext = new SymbolContext(
                symbolContext.Symbol,
                symbolContext.Compilation,
                report,
                symbolContext.CancellationToken);

            engine.RunSymbolRules(engineContext);
        }, SymbolKind.NamedType);

        context.RegisterOperationAction(operationContext =>
        {
            if (exemptions.IsExempt(operationContext.ContainingSymbol))
            {
                return;
            }

            var report = new Action<Diagnostic>(diagnostic =>
            {
                if (baseline.IsSuppressed(diagnostic.Id, operationContext.ContainingSymbol, diagnostic.Location))
                {
                    return;
                }

                operationContext.ReportDiagnostic(diagnostic);
            });

            var engineContext = new OperationContext(
                operationContext.Operation,
                operationContext.ContainingSymbol,
                operationContext.Compilation,
                report,
                operationContext.CancellationToken);

            engine.RunOperationRules(engineContext);
        },
        OperationKind.ObjectCreation,
        OperationKind.Invocation,
        OperationKind.FieldReference,
        OperationKind.PropertyReference,
        OperationKind.MethodReference,
        OperationKind.EventReference,
        OperationKind.TypeOf);
    }

    private static string? ReadProjectDirectory(AnalyzerOptions options)
    {
        var provider = options.AnalyzerConfigOptionsProvider;
        if (provider is null)
        {
            return null;
        }

        return provider.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var value)
            ? value
            : null;
    }
}