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
            Arch1002ForbiddenTypeRule.RuleDescriptor,
            Arch1003ForbiddenNamespaceRule.RuleDescriptor,
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
        ImmutableArray<IRule> rules;

        if (found && config is not null)
        {
            layers = config.BuildLayerIndex();
            rules = ArchitectureRuleFactory.Create(config, layers);
        }
        else
        {
            layers = ConventionLayerResolver.Resolve(context.Compilation);
            rules = ImmutableArray.Create<IRule>(new Arch1001LayerDependencyRule(layers));
        }

        if (layers.IsEmpty || rules.IsEmpty)
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
            var engineContext = new SymbolContext(
                symbolContext.Symbol,
                symbolContext.Compilation,
                symbolContext.ReportDiagnostic,
                symbolContext.CancellationToken);

            engine.RunSymbolRules(engineContext);
        }, SymbolKind.NamedType);

        context.RegisterOperationAction(operationContext =>
        {
            var engineContext = new OperationContext(
                operationContext.Operation,
                operationContext.ContainingSymbol,
                operationContext.Compilation,
                operationContext.ReportDiagnostic,
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
}