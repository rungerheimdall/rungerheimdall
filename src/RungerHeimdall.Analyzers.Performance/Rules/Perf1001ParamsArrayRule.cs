using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1001ParamsArrayRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1001",
        "Avoid empty params array allocation",
        "'{0}' is a params method called without arguments. The compiler emits an empty array allocation; the JIT may remove it.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF1001";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        IMethodSymbol? method;
        ImmutableArray<IArgumentOperation> arguments;

        switch (context.Operation)
        {
            case IInvocationOperation invocation:
                method = invocation.TargetMethod;
                arguments = invocation.Arguments;
                break;

            case IObjectCreationOperation creation:
                method = creation.Constructor;
                arguments = creation.Arguments;
                break;

            default:
                return;
        }

        if (method is null)
        {
            return;
        }

        var definition = method.ReducedFrom ?? method;
        var parameters = definition.Parameters;
        if (parameters.Length == 0)
        {
            return;
        }

        if (!parameters[parameters.Length - 1].IsParams)
        {
            return;
        }

        if (arguments.Length == 0)
        {
            return;
        }

        var lastArgument = arguments[arguments.Length - 1];
        if (!IsEmptyParamsArray(lastArgument))
        {
            return;
        }

        var displayName = method.MethodKind == MethodKind.Constructor
            ? method.ContainingType?.Name ?? method.Name
            : method.Name;

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            context.Operation.Syntax.GetLocation(),
            displayName);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsEmptyParamsArray(IArgumentOperation argument)
    {
        if (argument.Value is not IArrayCreationOperation array)
        {
            return false;
        }

        if (array.DimensionSizes.Length != 1)
        {
            return false;
        }

        var size = array.DimensionSizes[0].ConstantValue;
        return size.HasValue && size.Value is int value && value == 0;
    }
}