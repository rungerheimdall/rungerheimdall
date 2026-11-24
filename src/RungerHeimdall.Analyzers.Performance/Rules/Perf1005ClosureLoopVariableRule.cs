using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Operations;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1005ClosureLoopVariableRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1005",
        "Avoid capturing the loop variable in a closure",
        "The lambda captures the loop variable '{0}', allocating a closure on every iteration. Copy the value into a local before creating the delegate.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1005";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IAnonymousFunctionOperation anonymous)
        {
            return;
        }

        if (!OperationFacts.TryGetEnclosingLoop(anonymous, out var loop) || loop is null)
        {
            return;
        }

        if (loop.Locals.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var operation in anonymous.Body.Descendants())
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (operation is not ILocalReferenceOperation reference)
            {
                continue;
            }

            foreach (var local in loop.Locals)
            {
                if (!SymbolEqualityComparer.Default.Equals(reference.Local, local))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    RuleDescriptor,
                    anonymous.Syntax.GetLocation(),
                    local.Name));

                return;
            }
        }
    }
}