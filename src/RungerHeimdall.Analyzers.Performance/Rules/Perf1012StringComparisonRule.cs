using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1012StringComparisonRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1012",
        "Avoid ToLower/ToUpper for comparison",
        "'{0}' allocates a new string. Use StringComparison.OrdinalIgnoreCase instead.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1012";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (method.ContainingType?.SpecialType != SpecialType.System_String)
        {
            return;
        }

        if (!method.Name.Equals("ToLower", StringComparison.Ordinal)
            && !method.Name.Equals("ToUpper", StringComparison.Ordinal))
        {
            return;
        }

        if (method.Parameters.Length != 0)
        {
            return;
        }

        if (!IsUsedInComparison(invocation))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation(),
            method.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsUsedInComparison(IOperation operation)
    {
        var parent = operation.Parent;

        if (parent is IBinaryOperation binary
            && (binary.OperatorKind == BinaryOperatorKind.Equals
                || binary.OperatorKind == BinaryOperatorKind.NotEquals))
        {
            return true;
        }

        if (parent is IArgumentOperation argument
            && argument.Parent is IInvocationOperation call
            && IsComparisonMethod(call.TargetMethod))
        {
            return true;
        }

        if (parent is IInvocationOperation receiverCall
            && receiverCall.Instance == operation
            && IsComparisonMethod(receiverCall.TargetMethod))
        {
            return true;
        }

        return false;
    }

    private static bool IsComparisonMethod(IMethodSymbol method)
    {
        if (method.ContainingType?.SpecialType != SpecialType.System_String)
        {
            return false;
        }

        switch (method.Name)
        {
            case "Equals":
            case "Contains":
            case "StartsWith":
            case "EndsWith":
            case "Compare":
            case "CompareTo":
                return true;

            default:
                return false;
        }
    }
}