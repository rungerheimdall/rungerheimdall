using RungerHeimdall.Engine.Json;
using Xunit;

namespace RungerHeimdall.Engine.Tests;

public sealed class JsonParserTests
{
    [Fact]
    public void ParsesObjectWithNestedArray()
    {
        const string json = """
            {
              "version": 1,
              "layers": [
                { "name": "Domain", "namespaces": ["MyApp.Domain", "MyApp.Domain.*"], "rank": 0 }
              ],
              "enabled": true,
              "note": null
            }
            """;

        var root = (JsonObject)JsonValue.Parse(json);

        Assert.Equal(1, root.GetInt("version"));
        Assert.True(root.GetBool("enabled"));
        Assert.Null(root.Get("note"));

        var layers = root.GetArray("layers");
        Assert.NotNull(layers);
        Assert.Single(layers!.Items);

        var layer = (JsonObject)layers.Items[0];
        Assert.Equal("Domain", layer.GetString("name"));
        Assert.Equal(0, layer.GetInt("rank"));

        var namespaces = layer.GetArray("namespaces");
        Assert.NotNull(namespaces);
        Assert.Equal(2, namespaces!.Items.Count);
    }

    [Fact]
    public void ParsesEscapedStrings()
    {
        const string json = """{"path":"C:\\temp\\\"quoted\\\"","unicode":"\u4E2D"}""";

        var root = (JsonObject)JsonValue.Parse(json);

        Assert.Equal("C:\\temp\\\"quoted\\\"", root.GetString("path"));
        Assert.Equal("中", root.GetString("unicode"));
    }

    [Fact]
    public void ParsesNumbers()
    {
        var root = (JsonObject)JsonValue.Parse("""{"a":-1,"b":2.5,"c":1e3}""");

        Assert.Equal(-1, root.GetInt("a"));
        Assert.Equal(2.5, ((JsonNumber)root.Get("b")!).Value);
        Assert.Equal(1000, ((JsonNumber)root.Get("c")!).Value);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("""{"a":}""")]
    [InlineData("""{"a":1,}""")]
    [InlineData("nul")]
    [InlineData("\"unterminated")]
    [InlineData("""{"a":1}extra""")]
    public void InvalidJson_Throws(string json)
    {
        Assert.Throws<FormatException>(() => JsonValue.Parse(json));
    }
}