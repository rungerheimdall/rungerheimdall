using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;

namespace RungerHeimdall.Analyzers.Architecture;

public static class ConventionLayerResolver
{
    private static readonly Dictionary<string, int> KnownLayers =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Domain"] = 0,
            ["Core"] = 0,
            ["Shared"] = 0,
            ["Application"] = 1,
            ["Modules"] = 1,
            ["Infrastructure"] = 2,
            ["Api"] = 3,
            ["Web"] = 3,
            ["Endpoints"] = 3,
            ["Host"] = 4,
        };

    public static LayerIndex Resolve(Compilation compilation)
    {
        var patterns = new Dictionary<string, LayerDefinition>(StringComparer.OrdinalIgnoreCase);

        AddAssembly(compilation.Assembly, patterns);

        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (IsFrameworkAssembly(reference))
            {
                continue;
            }

            AddAssembly(reference, patterns);
        }

        var layers = new List<LayerDefinition>(patterns.Count);
        foreach (var pair in patterns)
        {
            layers.Add(pair.Value);
        }

        return new LayerIndex(layers);
    }

    private static void AddAssembly(IAssemblySymbol assembly, Dictionary<string, LayerDefinition> patterns)
    {
        foreach (var ns in EnumerateNamespaces(assembly.GlobalNamespace))
        {
            var segments = ns.ToDisplayString().Split('.');
            for (var i = 0; i < segments.Length; i++)
            {
                if (!KnownLayers.TryGetValue(segments[i], out var rank))
                {
                    continue;
                }

                var prefix = string.Join(".", segments, 0, i + 1);
                if (patterns.ContainsKey(prefix))
                {
                    break;
                }

                var layerName = segments[i];
                layerName = char.ToUpperInvariant(layerName[0]) + layerName.Substring(1).ToLowerInvariant();
                patterns[prefix] = new LayerDefinition(
                    layerName,
                    rank,
                    new NamespacePattern(prefix),
                    new NamespacePattern(prefix + ".*"));
                break;
            }
        }
    }

    private static bool IsFrameworkAssembly(IAssemblySymbol assembly)
    {
        var name = assembly.Identity.Name;
        if (name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
            || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
            || name.Equals("System", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<INamespaceSymbol> EnumerateNamespaces(INamespaceSymbol root)
    {
        foreach (var child in root.GetNamespaceMembers())
        {
            yield return child;

            foreach (var nested in EnumerateNamespaces(child))
            {
                yield return nested;
            }
        }
    }
}