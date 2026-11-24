using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Symbols;

namespace RungerHeimdall.Engine.Configuration;

public sealed class ScopeMatcher
{
    private readonly LayerIndex _layers;
    private readonly HashSet<string> _layerNames;
    private readonly NamespacePattern[] _patterns;

    public ScopeMatcher(LayerIndex layers, IEnumerable<string> values)
    {
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _layerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var patterns = new List<NamespacePattern>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.IndexOf('.') >= 0 || value.IndexOf('*') >= 0)
            {
                patterns.Add(new NamespacePattern(value));
            }
            else
            {
                _layerNames.Add(value);
            }
        }

        _patterns = patterns.ToArray();
    }

    public bool IsEmpty => _layerNames.Count == 0 && _patterns.Length == 0;

    public bool Matches(ISymbol? symbol)
    {
        if (symbol is null)
        {
            return false;
        }

        if (_layerNames.Count > 0)
        {
            var layer = _layers.Match(symbol.ContainingNamespace);
            if (layer is not null && _layerNames.Contains(layer.Name))
            {
                return true;
            }
        }

        if (_patterns.Length == 0)
        {
            return false;
        }

        var namespaceName = SymbolFacts.GetNamespaceName(symbol);
        if (namespaceName is null)
        {
            return false;
        }

        foreach (var pattern in _patterns)
        {
            if (pattern.Matches(namespaceName))
            {
                return true;
            }
        }

        return false;
    }
}