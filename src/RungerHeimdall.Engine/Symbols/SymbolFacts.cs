using System;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Symbols;

public static class SymbolFacts
{
    public static string? GetNamespaceName(ISymbol? symbol)
    {
        var ns = symbol?.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace)
        {
            return null;
        }

        return ns.ToDisplayString();
    }

    public static string? GetFullName(ISymbol? symbol)
    {
        return symbol?.ToDisplayString();
    }

    public static bool IsInNamespace(ISymbol? symbol, string namespaceName)
    {
        var current = symbol?.ContainingNamespace;
        while (current is not null && !current.IsGlobalNamespace)
        {
            if (current.ToDisplayString().Equals(namespaceName, StringComparison.Ordinal))
            {
                return true;
            }

            current = current.ContainingNamespace;
        }

        return false;
    }

    public static INamedTypeSymbol? GetDefinition(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named ? named.ConstructedFrom : null;
    }

    public static string? GetTypeFullName(ITypeSymbol? type)
    {
        var definition = GetDefinition(type);
        return definition?.ToDisplayString() ?? type?.ToDisplayString();
    }

    public static bool IsFrameworkType(ITypeSymbol? type)
    {
        var ns = GetNamespaceName(type);
        if (ns is null)
        {
            return false;
        }

        return ns.Equals("System", StringComparison.Ordinal)
            || ns.StartsWith("System.", StringComparison.Ordinal)
            || ns.Equals("Microsoft", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.", StringComparison.Ordinal);
    }
}