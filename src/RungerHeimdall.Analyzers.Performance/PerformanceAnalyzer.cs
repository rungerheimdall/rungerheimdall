using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RungerHeimdall.Analyzers.Performance.Rules;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PerformanceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Perf1001ParamsArrayRule.RuleDescriptor,
            Perf1002CountRule.RuleDescriptor,
            Perf1007EnumHasFlagRule.RuleDescriptor,
            Perf1012StringComparisonRule.RuleDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var engine = new RuleEngine(new IRule[]
        {
            new Perf1001ParamsArrayRule(),
            new Perf1002CountRule(),
            new Perf1007EnumHasFlagRule(),
            new Perf1012StringComparisonRule(),
        });

        if (engine.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(operationContext =>
        {
            var engineContext = new OperationContext(
                operationContext.Operation,
                operationContext.ContainingSymbol,
                operationContext.Compilation,
                operationContext.ReportDiagnostic,
                operationContext.CancellationToken);

            engine.RunOperationRules(engineContext);
        }, OperationKind.Invocation, OperationKind.ObjectCreation);
    }
}