using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.HotPath;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2007TaskFromResultRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2007",
        "Avoid Task.FromResult on hot paths",
        "'Task.FromResult' allocates a new task on every call. Cache the completed task or return a value directly.",
        "Performance",
        DiagnosticSeverity.Info);

    private readonly HotPathRegistry _hotPath;

    public Perf2007TaskFromResultRule(HotPathRegistry hotPath)
    {
        _hotPath = hotPath;
    }

    public string Id => "PERF2007";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (method.Name != "FromResult")
        {
            return;
        }

        if (method.ContainingType?.ToDisplayString() != "System.Threading.Tasks.Task")
        {
            return;
        }

        if (!IsInsideLoop(invocation) && !_hotPath.IsHot(context.OwningSymbol))
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