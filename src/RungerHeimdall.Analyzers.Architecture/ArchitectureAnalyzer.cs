using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RungerHeimdall.Analyzers.Architecture.Rules;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Architecture;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArchitectureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Arch1001LayerDependencyRule.RuleDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var layers = ConventionLayerResolver.Resolve(context.Compilation);
        if (layers.IsEmpty)
        {
            return;
        }

        var engine = new RuleEngine(new IRule[]
        {
            new Arch1001LayerDependencyRule(layers),
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
}