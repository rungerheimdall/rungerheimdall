using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.HotPath;

public sealed class HotPathRegistry
{
    private static readonly HashSet<string> EntryPointAttributes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "HttpGetAttribute",
            "HttpPostAttribute",
            "HttpPutAttribute",
            "HttpDeleteAttribute",
            "HttpPatchAttribute",
            "HttpHeadAttribute",
            "HttpOptionsAttribute",
            "FunctionNameAttribute",
        };

    private static readonly HashSet<string> EntryPointBaseTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "ControllerBase",
            "Controller",
            "BackgroundService",
            "Hub",
        };

    private readonly ConcurrentDictionary<ISymbol, bool> _cache =
        new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);

    public bool IsHot(ISymbol? symbol)
    {
        return symbol is not null && _cache.GetOrAdd(symbol, Evaluate);
    }

    public bool IsCold(ISymbol? symbol)
    {
        return symbol is not null && HasAttribute(symbol, "RungerHeimdall.ColdPathAttribute");
    }

    private static bool Evaluate(ISymbol symbol)
    {
        if (HasAttribute(symbol, "RungerHeimdall.ColdPathAttribute"))
        {
            return false;
        }

        if (HasAttribute(symbol, "RungerHeimdall.HotPathAttribute"))
        {
            return true;
        }

        if (symbol is IMethodSymbol method)
        {
            if (method.ContainingType is not null
                && HasAttribute(method.ContainingType, "RungerHeimdall.HotPathAttribute"))
            {
                return true;
            }

            if (IsFrameworkEntryPoint(method))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFrameworkEntryPoint(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;
            if (name is not null && EntryPointAttributes.Contains(name))
            {
                return true;
            }
        }

        var type = method.ContainingType;
        if (type is null)
        {
            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (!EntryPointBaseTypes.Contains(current.Name))
            {
                continue;
            }

            var namespaceName = current.ContainingNamespace?.ToDisplayString();
            if (namespaceName is "Microsoft.AspNetCore.Mvc"
                or "Microsoft.Extensions.Hosting"
                or "Microsoft.AspNetCore.SignalR")
            {
                return true;
            }
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (@interface.Name == "IHostedService"
                && @interface.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Hosting")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string fullName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == fullName)
            {
                return true;
            }
        }

        return false;
    }
}