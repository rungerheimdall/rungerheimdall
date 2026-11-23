using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Configuration;

public sealed class LayerDefinition
{
    public LayerDefinition(string name, int rank, params NamespacePattern[] patterns)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Layer name is required.", nameof(name));
        }

        Name = name;
        Rank = rank;
        Patterns = patterns ?? Array.Empty<NamespacePattern>();
    }

    public string Name { get; }

    public int Rank { get; }

    public NamespacePattern[] Patterns { get; }
}

public sealed class LayerMatch
{
    public LayerMatch(string name, int rank)
    {
        Name = name;
        Rank = rank;
    }

    public string Name { get; }

    public int Rank { get; }
}

public sealed class LayerIndex
{
    private readonly LayerDefinition[] _layers;

    public LayerIndex(IEnumerable<LayerDefinition> layers)
    {
        if (layers is null)
        {
            throw new ArgumentNullException(nameof(layers));
        }

        var list = new List<LayerDefinition>();
        foreach (var layer in layers)
        {
            list.Add(layer);
        }

        _layers = list.ToArray();
    }

    public bool IsEmpty => _layers.Length == 0;

    public LayerMatch? Match(string? namespaceName)
    {
        if (_layers.Length == 0 || string.IsNullOrEmpty(namespaceName))
        {
            return null;
        }

        LayerDefinition? best = null;
        var bestLength = -1;

        foreach (var layer in _layers)
        {
            foreach (var pattern in layer.Patterns)
            {
                var length = pattern.GetMatchLength(namespaceName!);
                if (length > bestLength)
                {
                    bestLength = length;
                    best = layer;
                }
            }
        }

        return best is null ? null : new LayerMatch(best.Name, best.Rank);
    }

    public LayerMatch? Match(INamespaceSymbol? namespaceSymbol)
    {
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return null;
        }

        return Match(namespaceSymbol.ToDisplayString());
    }
}