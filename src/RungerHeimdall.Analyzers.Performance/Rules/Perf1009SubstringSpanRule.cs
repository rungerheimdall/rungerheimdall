using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1009SubstringSpanRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1009",
        "Avoid Substring allocations for parsing and comparison",
        "'Substring' allocates a new string. Use a span-based overload or AsSpan instead.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1009";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (!method.Name.Equals("Substring", StringComparison.Ordinal))
        {
            return;
        }

        if (method.ContainingType?.SpecialType != SpecialType.System_String)
        {
            return;
        }

        if (!IsUsedForParseOrComparison(invocation))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsUsedForParseOrComparison(IOperation operation)
    {
        var parent = operation.Parent;

        if (parent is IArgumentOperation argument
            && argument.Parent is IInvocationOperation call
            && IsParseOrComparison(call.TargetMethod))
        {
            return true;
        }

        if (parent is IInvocationOperation receiverCall
            && receiverCall.Instance == operation
            && IsParseOrComparison(receiverCall.TargetMethod))
        {
            return true;
        }

        return false;
    }

    private static bool IsParseOrComparison(IMethodSymbol method)
    {
        if (method.ContainingType?.SpecialType == SpecialType.System_String)
        {
            switch (method.Name)
            {
                case "Equals":
                case "Contains":
                case "StartsWith":
                case "EndsWith":
                case "Compare":
                case "CompareTo":
                    return true;
            }

            return false;
        }

        switch (method.ContainingType?.SpecialType)
        {
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                switch (method.Name)
                {
                    case "Parse":
                    case "TryParse":
                        return true;
                }

                return false;

            default:
                return false;
        }
    }
}