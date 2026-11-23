using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1002CountRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1002",
        "Avoid Enumerable.Count() on a countable collection",
        "'{0}.Count()' enumerates the sequence. Use the Count or Length property instead.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1002";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (!method.Name.Equals("Count", System.StringComparison.Ordinal))
        {
            return;
        }

        if (method.ContainingType is null
            || !method.ContainingType.ToDisplayString().Equals("System.Linq.Enumerable", System.StringComparison.Ordinal))
        {
            return;
        }

        var definition = method.ReducedFrom ?? method;
        if (definition.Parameters.Length != 1 || invocation.Arguments.Length != 1)
        {
            return;
        }

        var receiver = invocation.Arguments[0].Value;
        while (receiver is Microsoft.CodeAnalysis.Operations.IConversionOperation conversion)
        {
            receiver = conversion.Operand;
        }

        var receiverType = receiver?.Type;
        if (receiverType is null || !HasCheapCount(receiverType))
        {
            return;
        }

        var location = invocation.Syntax.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(
            RuleDescriptor,
            location,
            SymbolFacts.GetTypeFullName(receiverType) ?? receiverType.Name));
    }

    private static bool HasCheapCount(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.Name.Equals("ICollection", System.StringComparison.Ordinal)
            || named.Name.Equals("IReadOnlyCollection", System.StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var @interface in named.AllInterfaces)
        {
            var definition = @interface.OriginalDefinition.ToDisplayString();
            if (definition.Equals("System.Collections.Generic.ICollection<T>", System.StringComparison.Ordinal)
                || definition.Equals("System.Collections.Generic.IReadOnlyCollection<T>", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}