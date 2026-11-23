using System;
using System.Collections.Generic;
using RungerHeimdall.Engine.Json;

namespace RungerHeimdall.Engine.Configuration;

internal static class ArchitectureConfigReader
{
    public static ArchitectureConfig Read(JsonObject root)
    {
        var config = new ArchitectureConfig();

        var version = root.GetInt("version");
        if (version.HasValue && version.Value != 1)
        {
            throw new FormatException($"Unsupported configuration version '{version.Value}'.");
        }

        config.Version = version ?? 1;

        ReadLayers(root, config);
        ReadRules(root, config);
        ReadExemptions(root, config);

        return config;
    }

    private static void ReadLayers(JsonObject root, ArchitectureConfig config)
    {
        var layers = root.GetArray("layers");
        if (layers is null)
        {
            return;
        }

        foreach (var item in layers.Items)
        {
            if (item is not JsonObject layerObject)
            {
                throw new FormatException("Each layer must be a JSON object.");
            }

            var name = layerObject.GetString("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new FormatException("Layer name is required.");
            }

            var layer = new LayerConfig
            {
                Name = name!,
                Rank = layerObject.GetInt("rank") ?? 0,
            };

            AddStrings(layer.Namespaces, layerObject.GetArray("namespaces"));
            if (layer.Namespaces.Count == 0)
            {
                throw new FormatException($"Layer '{layer.Name}' must declare at least one namespace.");
            }

            config.Layers.Add(layer);
        }
    }

    private static void ReadRules(JsonObject root, ArchitectureConfig config)
    {
        var rules = root.GetArray("rules");
        if (rules is null)
        {
            return;
        }

        foreach (var item in rules.Items)
        {
            if (item is not JsonObject ruleObject)
            {
                throw new FormatException("Each rule must be a JSON object.");
            }

            var id = ruleObject.GetString("id");
            var kind = ruleObject.GetString("kind");

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new FormatException("Rule id is required.");
            }

            if (string.IsNullOrWhiteSpace(kind))
            {
                throw new FormatException($"Rule '{id}' must declare a kind.");
            }

            var rule = new RuleConfig
            {
                Id = id!,
                Kind = kind!,
                From = ruleObject.GetString("from"),
                Severity = ruleObject.GetString("severity"),
            };

            AddStrings(rule.Deny, ruleObject.GetArray("deny"));
            AddStrings(rule.In, ruleObject.GetArray("in"));
            AddStrings(rule.Types, ruleObject.GetArray("types"));

            config.Rules.Add(rule);
        }
    }

    private static void ReadExemptions(JsonObject root, ArchitectureConfig config)
    {
        var exemptions = root.GetArray("exemptions");
        if (exemptions is null)
        {
            return;
        }

        foreach (var item in exemptions.Items)
        {
            if (item is not JsonObject exemptionObject)
            {
                throw new FormatException("Each exemption must be a JSON object.");
            }

            var scope = exemptionObject.GetString("scope");
            var target = exemptionObject.GetString("target");
            var reason = exemptionObject.GetString("reason");

            if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(target))
            {
                throw new FormatException("Exemption scope and target are required.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new FormatException($"Exemption for '{target}' must declare a reason.");
            }

            config.Exemptions.Add(new ExemptionConfig
            {
                Scope = scope!,
                Target = target!,
                Reason = reason!,
                ExpiresOn = exemptionObject.GetString("expiresOn"),
            });
        }
    }

    private static void AddStrings(List<string> target, JsonArray? array)
    {
        if (array is null)
        {
            return;
        }

        foreach (var item in array.Items)
        {
            if (item is JsonString text && text.Value.Length > 0)
            {
                target.Add(text.Value);
            }
        }
    }
}