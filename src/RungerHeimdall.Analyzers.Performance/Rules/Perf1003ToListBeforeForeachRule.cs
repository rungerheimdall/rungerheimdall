using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1003ToListBeforeForeachRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1003",
        "Avoid ToList before foreach",
        "'ToList()' materializes the sequence only to iterate it. Iterate the source directly.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1003";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IForEachLoopOperation loop)
        {
            return;
        }

        var collection = loop.Collection;
        while (collection is IConversionOperation conversion)
        {
            collection = conversion.Operand;
        }

        if (collection is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (!method.Name.Equals("ToList", StringComparison.Ordinal))
        {
            return;
        }

        if (method.ContainingType?.ToDisplayString() != "System.Linq.Enumerable")
        {
            return;
        }

        var definition = method.ReducedFrom ?? method;
        if (definition.Parameters.Length != 1)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            invocation.Syntax.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}