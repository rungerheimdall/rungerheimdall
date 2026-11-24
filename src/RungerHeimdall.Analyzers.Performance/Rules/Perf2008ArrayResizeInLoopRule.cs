using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2008ArrayResizeInLoopRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2008",
        "Avoid Array.Resize inside loops",
        "'Array.Resize' copies the whole array on every call. Use a List<T> or ArrayPool<T> instead.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF2008";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var target = invocation.TargetMethod;
        if (target.Name != "Resize")
        {
            return;
        }

        if (target.ContainingType?.ToDisplayString() != "System.Array")
        {
            return;
        }

        if (!IsInsideLoop(invocation))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation());

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