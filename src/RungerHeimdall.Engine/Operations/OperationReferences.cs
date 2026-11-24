using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RungerHeimdall.Engine.Operations;

public static class OperationReferences
{
    public static IEnumerable<ISymbol> Enumerate(IOperation root)
    {
        foreach (var operation in root.DescendantsAndSelf())
        {
            var symbol = GetSymbol(operation);
            if (symbol is not null)
            {
                yield return symbol;
            }
        }
    }

    private static ISymbol? GetSymbol(IOperation operation)
    {
        switch (operation)
        {
            case IObjectCreationOperation creation:
                return creation.Constructor;

            case IInvocationOperation invocation:
                return invocation.TargetMethod;

            case IFieldReferenceOperation field:
                return field.Field;

            case IPropertyReferenceOperation property:
                return property.Property;

            case IMethodReferenceOperation method:
                return method.Method;

            case IEventReferenceOperation @event:
                return @event.Event;

            case ITypeOfOperation typeOf:
                return typeOf.TypeOperand;

            default:
                return null;
        }
    }
}