using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1008StructDefaultComparerRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1008",
        "Avoid the default comparer for a struct without a generic comparison interface",
        "'{0}.Default' boxes every comparison of struct '{1}' because it does not implement {2}. Implement the generic interface or provide a custom comparer.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1008";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation reference)
        {
            return;
        }

        var property = reference.Property;
        if (property.Name != "Default" || !property.IsStatic)
        {
            return;
        }

        var containingType = property.ContainingType;
        if (containingType is null || containingType.TypeArguments.Length != 1)
        {
            return;
        }

        var definition = containingType.OriginalDefinition;
        var namespaceName = definition.ContainingNamespace?.ToDisplayString();
        if (namespaceName != "System.Collections.Generic")
        {
            return;
        }

        string interfaceName;
        switch (definition.MetadataName)
        {
            case "EqualityComparer`1":
                interfaceName = "IEquatable<T>";
                break;

            case "Comparer`1":
                interfaceName = "IComparable<T>";
                break;

            default:
                return;
        }

        var argument = containingType.TypeArguments[0];
        if (!argument.IsValueType)
        {
            return;
        }

        var genericInterface = definition.MetadataName == "EqualityComparer`1"
            ? "IEquatable`1"
            : "IComparable`1";

        foreach (var @interface in argument.AllInterfaces)
        {
            if (@interface.OriginalDefinition.MetadataName == genericInterface
                && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], argument))
            {
                return;
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(
            RuleDescriptor,
            reference.Syntax.GetLocation(),
            definition.Name + "<T>",
            argument.ToDisplayString(),
            interfaceName));
    }
}