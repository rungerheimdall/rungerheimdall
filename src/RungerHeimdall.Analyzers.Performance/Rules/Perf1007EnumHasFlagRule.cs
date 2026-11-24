using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1007EnumHasFlagRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1007",
        "Avoid Enum.HasFlag boxing",
        "Enum.HasFlag boxes its argument on older runtimes. Use a bitwise comparison instead.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF1007";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (!method.Name.Equals("HasFlag", StringComparison.Ordinal))
        {
            return;
        }

        if (method.ContainingType?.SpecialType != SpecialType.System_Enum)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}