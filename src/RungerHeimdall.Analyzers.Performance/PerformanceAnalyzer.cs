using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RungerHeimdall.Analyzers.Performance.Rules;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.HotPath;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PerformanceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Perf1001ParamsArrayRule.RuleDescriptor,
            Perf1002CountRule.RuleDescriptor,
            Perf1003ToListBeforeForeachRule.RuleDescriptor,
            Perf1004StringConcatInLoopRule.RuleDescriptor,
            Perf1007EnumHasFlagRule.RuleDescriptor,
            Perf1009SubstringSpanRule.RuleDescriptor,
            Perf1010RegexSourceGeneratedRule.RuleDescriptor,
            Perf1012StringComparisonRule.RuleDescriptor,
            Perf2001LinqInLoopRule.RuleDescriptor,
            Perf2002TaskRunInAsyncRule.RuleDescriptor,
            Perf2003AsyncVoidRule.RuleDescriptor,
            Perf2004BlockingTaskRule.RuleDescriptor,
            Perf2005MultipleEnumerationRule.RuleDescriptor,
            Perf2006ValueTaskReuseRule.RuleDescriptor,
            Perf2007TaskFromResultRule.RuleDescriptor,
            Perf2008ArrayResizeInLoopRule.RuleDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var hotPath = new HotPathRegistry();

        var engine = new RuleEngine(new IRule[]
        {
            new Perf1001ParamsArrayRule(),
            new Perf1002CountRule(),
            new Perf1003ToListBeforeForeachRule(),
            new Perf1004StringConcatInLoopRule(),
            new Perf1007EnumHasFlagRule(),
            new Perf1009SubstringSpanRule(),
            new Perf1010RegexSourceGeneratedRule(),
            new Perf1012StringComparisonRule(),
            new Perf2001LinqInLoopRule(hotPath),
            new Perf2002TaskRunInAsyncRule(),
            new Perf2003AsyncVoidRule(),
            new Perf2004BlockingTaskRule(),
            new Perf2005MultipleEnumerationRule(),
            new Perf2006ValueTaskReuseRule(),
            new Perf2007TaskFromResultRule(hotPath),
            new Perf2008ArrayResizeInLoopRule(),
        });

        if (engine.IsEmpty)
        {
            return;
        }

        var found = ConfigLoader.TryLoad(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            out var config,
            out _);

        var exemptions = found && config is not null
            ? ExemptionStore.Create(config.Exemptions, DateTime.UtcNow.Date)
            : ExemptionStore.Empty;

        var baseline = BaselineStore.Load(
            context.Options.AdditionalFiles,
            context.CancellationToken,
            ReadProjectDirectory(context.Options),
            out _);

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
        OperationKind.Invocation,
        OperationKind.ObjectCreation,
        OperationKind.Loop,
        OperationKind.Binary,
        OperationKind.CompoundAssignment,
        OperationKind.PropertyReference);
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