using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2004BlockingTaskRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2004",
        "Avoid blocking on tasks",
        "'{0}' blocks the calling thread and can deadlock in a synchronization context. Await the task instead.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF2004";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        switch (context.Operation)
        {
            case IPropertyReferenceOperation property:
                if (property.Property.Name != "Result")
                {
                    return;
                }

                if (!IsTaskLike(property.Property.ContainingType))
                {
                    return;
                }

                Report(context, property.Syntax.GetLocation(), "Task.Result");
                return;

            case IInvocationOperation invocation:
                if (invocation.TargetMethod.Name != "Wait")
                {
                    return;
                }

                if (!IsTaskLike(invocation.TargetMethod.ContainingType))
                {
                    return;
                }

                Report(context, invocation.Syntax.GetLocation(), "Task.Wait");
                return;
        }
    }

    private static bool IsTaskLike(INamedTypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type.Name is not ("Task" or "ValueTask"))
        {
            return false;
        }

        return type.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";
    }

    private static void Report(OperationContext context, Location location, string member)
    {
        var diagnostic = Diagnostic.Create(RuleDescriptor, location, member);
        context.ReportDiagnostic(diagnostic);
    }
}