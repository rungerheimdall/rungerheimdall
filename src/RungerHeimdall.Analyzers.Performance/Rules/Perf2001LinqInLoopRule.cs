using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.HotPath;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2001LinqInLoopRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2001",
        "Avoid LINQ inside loops",
        "LINQ inside a loop allocates enumerators and delegates on every iteration. Use a manual loop or move the query out.",
        "Performance",
        DiagnosticSeverity.Info);

    private readonly HotPathRegistry _hotPath;

    public Perf2001LinqInLoopRule(HotPathRegistry hotPath)
    {
        _hotPath = hotPath;
    }

    public string Id => "PERF2001";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        if (_hotPath.IsCold(context.InnermostSymbol))
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (method.ContainingType?.ToDisplayString() != "System.Linq.Enumerable")
        {
            return;
        }

        if (!IsInsideLoop(invocation))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation(),
            method.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInsideLoop(IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is IForEachLoopOperation
                || parent is IForLoopOperation
                || parent is IWhileLoopOperation)
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }
}