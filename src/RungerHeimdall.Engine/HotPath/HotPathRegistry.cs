using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RungerHeimdall.Engine.HotPath;

public sealed class HotPathRegistry
{
    private const string HotPathAttributeName = "RungerHeimdall.HotPathAttribute";
    private const string ColdPathAttributeName = "RungerHeimdall.ColdPathAttribute";

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

    private static readonly HashSet<string> MinimalApiMethods =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "MapGet",
            "MapPost",
            "MapPut",
            "MapDelete",
            "MapPatch",
            "MapMethods",
            "MapFallback",
        };

    private static readonly string[] HeuristicPrefixes =
    {
        "Process",
        "Handle",
        "Execute",
        "Serialize",
        "Render",
    };

    private readonly ConcurrentDictionary<ISymbol, bool> _cache =
        new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);

    private readonly Compilation? _compilation;
    private readonly bool _heuristicEnabled;
    private readonly bool _propagationEnabled;

    public HotPathRegistry()
        : this(null, null)
    {
    }

    public HotPathRegistry(Compilation? compilation, AnalyzerConfigOptionsProvider? options)
    {
        _compilation = compilation;
        _heuristicEnabled = ReadBool(options, "rungerheimdall_hotpath_heuristic", defaultValue: false);
        _propagationEnabled = ReadBool(options, "rungerheimdall_hotpath_propagation", defaultValue: true);
    }

    public bool IsHot(ISymbol? symbol)
    {
        return symbol is not null && _cache.GetOrAdd(symbol, Evaluate);
    }

    public bool IsCold(ISymbol? symbol)
    {
        return symbol is not null && HasAttribute(symbol, ColdPathAttributeName);
    }

    private bool Evaluate(ISymbol symbol)
    {
        if (HasAttribute(symbol, ColdPathAttributeName))
        {
            return false;
        }

        if (IsSeed(symbol))
        {
            return true;
        }

        if (_propagationEnabled
            && symbol is IMethodSymbol method
            && CallsSeed(method))
        {
            return true;
        }

        return _heuristicEnabled
            && symbol is IMethodSymbol candidate
            && MatchesHeuristic(candidate);
    }

    private bool IsSeed(ISymbol symbol)
    {
        if (HasAttribute(symbol, HotPathAttributeName))
        {
            return true;
        }

        if (symbol is not IMethodSymbol method)
        {
            return false;
        }

        if (method.ContainingType is not null
            && HasAttribute(method.ContainingType, HotPathAttributeName))
        {
            return true;
        }

        if (IsIterator(method))
        {
            return true;
        }

        return IsFrameworkEntryPoint(method)
            || IsMinimalApiHandler(method)
            || IsMassTransitConsumer(method);
    }

    private static bool IsIterator(IMethodSymbol method)
    {
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax().DescendantNodes().OfType<YieldStatementSyntax>().Any())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsMinimalApiHandler(IMethodSymbol method)
    {
        if (method.MethodKind is not (MethodKind.AnonymousFunction or MethodKind.LocalFunction)
            || _compilation is null)
        {
            return false;
        }

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var node = reference.GetSyntax();
            var invocation = node
                .Ancestors()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocation is null)
            {
                continue;
            }

            var name = GetInvokedName(invocation);
            if (name is null || !MinimalApiMethods.Contains(name))
            {
                continue;
            }

            var model = _compilation.GetSemanticModel(invocation.SyntaxTree);
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol target)
            {
                continue;
            }

            var namespaceName = target.ContainingType?.ContainingNamespace?.ToDisplayString();
            if (namespaceName is not null
                && namespaceName.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetInvokedName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => null,
        };
    }

    private static bool IsMassTransitConsumer(IMethodSymbol method)
    {
        if (method.Name != "Consume")
        {
            return false;
        }

        var type = method.ContainingType;
        if (type is null)
        {
            return false;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (@interface.OriginalDefinition.ToDisplayString() == "MassTransit.IConsumer<T>")
            {
                return true;
            }
        }

        return false;
    }

    private bool CallsSeed(IMethodSymbol method)
    {
        if (_compilation is null || method.DeclaringSyntaxReferences.Length == 0)
        {
            return false;
        }

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var node = reference.GetSyntax();
            var model = _compilation.GetSemanticModel(node.SyntaxTree);

            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol target
                    && !SymbolEqualityComparer.Default.Equals(target, method)
                    && IsSeed(target))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesHeuristic(IMethodSymbol method)
    {
        foreach (var prefix in HeuristicPrefixes)
        {
            if (method.Name.StartsWith(prefix, StringComparison.Ordinal))
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

    private static bool ReadBool(AnalyzerConfigOptionsProvider? provider, string key, bool defaultValue)
    {
        if (provider is null
            || !provider.GlobalOptions.TryGetValue(key, out var value)
            || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }
}