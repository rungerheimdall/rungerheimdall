using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1011ValueTypeForeachRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1011",
        "Avoid foreach over a non-generic collection of value types",
        "foreach over the non-generic IEnumerable boxes every '{0}' element. Use a generic collection instead.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1011";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IForEachLoopOperation loop)
        {
            return;
        }

        var element = loop.LoopControlVariable?.Type;
        if (element is null && !loop.Locals.IsDefaultOrEmpty)
        {
            element = loop.Locals[0].Type;
        }

        if (element is null || !element.IsValueType)
        {
            return;
        }

        var collection = Unwrap(loop.Collection);
        if (collection?.Type is not INamedTypeSymbol { SpecialType: SpecialType.System_Collections_IEnumerable })
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            RuleDescriptor,
            loop.Syntax.GetLocation(),
            element.ToDisplayString()));
    }

    private static IOperation? Unwrap(IOperation? operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }
}