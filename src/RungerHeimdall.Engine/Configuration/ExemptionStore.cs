using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Configuration;

public sealed class ExemptionStore
{
    private readonly List<Entry> _entries;

    private ExemptionStore(List<Entry> entries)
    {
        _entries = entries;
    }

    public static ExemptionStore Create(IEnumerable<ExemptionConfig> exemptions, DateTime today)
    {
        var entries = new List<Entry>();

        foreach (var exemption in exemptions)
        {
            var expired = false;
            if (!string.IsNullOrEmpty(exemption.ExpiresOn)
                && DateTime.TryParse(
                    exemption.ExpiresOn,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var expires))
            {
                expired = expires.Date < today.Date;
            }

            entries.Add(new Entry(exemption, expired));
        }

        return new ExemptionStore(entries);
    }

    public static ExemptionStore Empty { get; } =
        new ExemptionStore(new List<Entry>());

    public bool IsExempt(ISymbol? symbol)
    {
        if (symbol is null)
        {
            return false;
        }

        foreach (var entry in _entries)
        {
            if (!entry.Expired && Matches(entry.Config, symbol))
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<ExemptionConfig> GetExpired()
    {
        foreach (var entry in _entries)
        {
            if (entry.Expired)
            {
                yield return entry.Config;
            }
        }
    }

    private static bool Matches(ExemptionConfig exemption, ISymbol symbol)
    {
        switch (exemption.Scope.ToLowerInvariant())
        {
            case "type":
                return GetTypeName(symbol).Equals(exemption.Target, StringComparison.Ordinal);

            case "namespace":
                var namespaceName = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                return namespaceName.Equals(exemption.Target, StringComparison.Ordinal)
                    || namespaceName.StartsWith(exemption.Target + ".", StringComparison.Ordinal);

            case "member":
                return symbol.ToDisplayString().Equals(exemption.Target, StringComparison.Ordinal);

            case "assembly":
                return symbol.ContainingAssembly is { } assembly
                    && assembly.Identity.Name.Equals(exemption.Target, StringComparison.Ordinal);

            default:
                return false;
        }
    }

    private static string GetTypeName(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol type)
        {
            return type.ToDisplayString();
        }

        return symbol.ContainingType?.ToDisplayString() ?? symbol.ToDisplayString();
    }

    private sealed class Entry
    {
        public Entry(ExemptionConfig config, bool expired)
        {
            Config = config;
            Expired = expired;
        }

        public ExemptionConfig Config { get; }

        public bool Expired { get; }
    }
}