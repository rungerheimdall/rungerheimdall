using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1004StringConcatInLoopRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1004",
        "Avoid string concatenation inside loops",
        "String concatenation inside a loop allocates a new string per iteration. Use StringBuilder.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1004";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        switch (context.Operation)
        {
            case IBinaryOperation binary:
                if (binary.OperatorKind != BinaryOperatorKind.Add
                    || binary.Type?.SpecialType != SpecialType.System_String)
                {
                    return;
                }

                ReportIfInsideLoop(context, binary);
                return;

            case ICompoundAssignmentOperation compound:
                if (compound.OperatorKind != BinaryOperatorKind.Add
                    || compound.Type?.SpecialType != SpecialType.System_String)
                {
                    return;
                }

                ReportIfInsideLoop(context, compound);
                return;
        }
    }

    private static void ReportIfInsideLoop(OperationContext context, IOperation operation)
    {
        if (!IsInsideLoop(operation))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            operation.Syntax.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInsideLoop(IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ILoopOperation)
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }
}