using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2002TaskRunInAsyncRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2002",
        "Avoid Task.Run inside async methods",
        "'Task.Run' inside an async method adds a thread-pool hop without doing CPU-bound work. Await the operation directly.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF2002";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        if (context.OwningSymbol is not IMethodSymbol method || !method.IsAsync)
        {
            return;
        }

        var target = invocation.TargetMethod;
        if (target.Name != "Run")
        {
            return;
        }

        if (target.ContainingType?.ToDisplayString() != "System.Threading.Tasks.Task")
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}