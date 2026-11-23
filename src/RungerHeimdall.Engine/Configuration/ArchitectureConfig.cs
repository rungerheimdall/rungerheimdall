using System.Collections.Generic;

namespace RungerHeimdall.Engine.Configuration;

public sealed class ArchitectureConfig
{
    public int Version { get; set; } = 1;

    public List<LayerConfig> Layers { get; } = new List<LayerConfig>();

    public List<RuleConfig> Rules { get; } = new List<RuleConfig>();

    public List<ExemptionConfig> Exemptions { get; } = new List<ExemptionConfig>();

    public LayerIndex BuildLayerIndex()
    {
        var definitions = new List<LayerDefinition>(Layers.Count);

        foreach (var layer in Layers)
        {
            var patterns = new NamespacePattern[layer.Namespaces.Count];
            for (var index = 0; index < layer.Namespaces.Count; index++)
            {
                patterns[index] = new NamespacePattern(layer.Namespaces[index]);
            }

            definitions.Add(new LayerDefinition(layer.Name, layer.Rank, patterns));
        }

        return new LayerIndex(definitions);
    }
}

public sealed class LayerConfig
{
    public string Name { get; set; } = string.Empty;

    public int Rank { get; set; }

    public List<string> Namespaces { get; } = new List<string>();
}

public sealed class RuleConfig
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string? From { get; set; }

    public string? Severity { get; set; }

    public List<string> Deny { get; } = new List<string>();

    public List<string> In { get; } = new List<string>();

    public List<string> Types { get; } = new List<string>();
}

public sealed class ExemptionConfig
{
    public string Scope { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string? ExpiresOn { get; set; }
}