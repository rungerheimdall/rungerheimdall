using RungerHeimdall.Engine.Configuration;
using Xunit;

namespace RungerHeimdall.Engine.Tests;

public sealed class LayerIndexTests
{
    [Fact]
    public void ExactPattern_MatchesOnlyItself()
    {
        var pattern = new NamespacePattern("MyApp.Domain");

        Assert.True(pattern.Matches("MyApp.Domain"));
        Assert.False(pattern.Matches("MyApp.Domain.Entities"));
        Assert.False(pattern.Matches("MyApp.Domains"));
    }

    [Fact]
    public void PrefixPattern_MatchesChildren()
    {
        var pattern = new NamespacePattern("MyApp.Domain.*");

        Assert.True(pattern.Matches("MyApp.Domain"));
        Assert.True(pattern.Matches("MyApp.Domain.Entities"));
        Assert.False(pattern.Matches("MyApp.Domains"));
    }

    [Fact]
    public void LayerIndex_PicksMostSpecificPrefix()
    {
        var index = new LayerIndex(new[]
        {
            new LayerDefinition("Outer", 2, new NamespacePattern("MyApp.*")),
            new LayerDefinition("Domain", 0, new NamespacePattern("MyApp.Domain.*")),
        });

        var match = index.Match("MyApp.Domain.Entities");

        Assert.NotNull(match);
        Assert.Equal("Domain", match!.Name);
        Assert.Equal(0, match.Rank);
    }

    [Fact]
    public void LayerIndex_Empty_ReturnsNull()
    {
        var index = new LayerIndex(Array.Empty<LayerDefinition>());

        Assert.Null(index.Match("MyApp.Domain"));
    }
}