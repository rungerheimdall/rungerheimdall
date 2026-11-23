using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RungerHeimdall.Engine.Configuration;
using Xunit;

namespace RungerHeimdall.Engine.Tests;

public sealed class ConfigLoaderTests
{
    [Fact]
    public void TryLoad_WithoutConfig_ReturnsFalse()
    {
        var found = ConfigLoader.TryLoad(Array.Empty<AdditionalText>(), default, out var config, out var error);

        Assert.False(found);
        Assert.Null(config);
        Assert.Null(error);
    }

    [Fact]
    public void TryLoad_ValidConfig_ReturnsConfig()
    {
        const string json = """
            {
              "version": 1,
              "layers": [
                { "name": "Domain", "namespaces": ["MyApp.Domain", "MyApp.Domain.*"], "rank": 0 },
                { "name": "Infrastructure", "namespaces": ["MyApp.Infrastructure.*"], "rank": 2 }
              ],
              "rules": [
                { "id": "ARCH1001", "kind": "layerDependency", "from": "Domain", "deny": ["Infrastructure"], "severity": "error" }
              ]
            }
            """;

        var found = ConfigLoader.TryLoad(
            new[] { new FakeAdditionalText("/repo/architecture.json", json) },
            default,
            out var config,
            out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(config);
        Assert.Equal(2, config!.Layers.Count);
        Assert.Single(config.Rules);
        Assert.Equal("error", config.Rules[0].Severity);
    }

    [Fact]
    public void TryLoad_InvalidJson_ReturnsError()
    {
        var found = ConfigLoader.TryLoad(
            new[] { new FakeAdditionalText("/repo/architecture.json", "{") },
            default,
            out var config,
            out var error);

        Assert.True(found);
        Assert.Null(config);
        Assert.NotNull(error);
        Assert.Contains("invalid", error!.ToLowerInvariant());
    }

    [Fact]
    public void TryLoad_ExemptionWithoutReason_ReturnsError()
    {
        const string json = """{ "exemptions": [ { "scope": "type", "target": "A.B" } ] }""";

        var found = ConfigLoader.TryLoad(
            new[] { new FakeAdditionalText("/repo/architecture.json", json) },
            default,
            out var config,
            out var error);

        Assert.True(found);
        Assert.Null(config);
        Assert.NotNull(error);
        Assert.Contains("reason", error!.ToLowerInvariant());
    }

    [Fact]
    public void BuildLayerIndex_MapsConfiguredNamespaces()
    {
        const string json = """
            {
              "layers": [
                { "name": "Domain", "namespaces": ["MyApp.Domain.*"], "rank": 0 },
                { "name": "Infrastructure", "namespaces": ["MyApp.Infrastructure.*"], "rank": 2 }
              ]
            }
            """;

        ConfigLoader.TryLoad(
            new[] { new FakeAdditionalText("/repo/architecture.json", json) },
            default,
            out var config,
            out _);

        var index = config!.BuildLayerIndex();

        Assert.Equal("Domain", index.Match("MyApp.Domain.Entities")!.Name);
        Assert.Equal("Infrastructure", index.Match("MyApp.Infrastructure.Persistence")!.Name);
        Assert.Null(index.Match("MyApp.Other"));
    }
}

internal sealed class FakeAdditionalText : AdditionalText
{
    private readonly SourceText _text;

    public FakeAdditionalText(string path, string text)
    {
        Path = path;
        _text = SourceText.From(text, Encoding.UTF8);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return _text;
    }
}