using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Symbols;

public static class TypeReferences
{
    public static IEnumerable<ITypeSymbol> Enumerate(INamedTypeSymbol type)
    {
        if (type.BaseType is not null)
        {
            foreach (var item in Flatten(type.BaseType))
            {
                yield return item;
            }
        }

        foreach (var @interface in type.Interfaces)
        {
            foreach (var item in Flatten(@interface))
            {
                yield return item;
            }
        }

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            switch (member)
            {
                case IFieldSymbol field:
                    foreach (var item in Flatten(field.Type))
                    {
                        yield return item;
                    }

                    break;

                case IPropertySymbol property:
                    foreach (var item in Flatten(property.Type))
                    {
                        yield return item;
                    }

                    break;

                case IEventSymbol @event:
                    foreach (var item in Flatten(@event.Type))
                    {
                        yield return item;
                    }

                    break;

                case IMethodSymbol method:
                    foreach (var item in Flatten(method.ReturnType))
                    {
                        yield return item;
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        foreach (var item in Flatten(parameter.Type))
                        {
                            yield return item;
                        }
                    }

                    break;
            }
        }
    }

    private static IEnumerable<ITypeSymbol> Flatten(ITypeSymbol type)
    {
        yield return type;

        if (type is INamedTypeSymbol named)
        {
            foreach (var argument in named.TypeArguments)
            {
                foreach (var item in Flatten(argument))
                {
                    yield return item;
                }
            }
        }
        else if (type is IArrayTypeSymbol array)
        {
            foreach (var item in Flatten(array.ElementType))
            {
                yield return item;
            }
        }
    }
}