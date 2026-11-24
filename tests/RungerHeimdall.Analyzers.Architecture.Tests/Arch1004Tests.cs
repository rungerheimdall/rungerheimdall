using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1004Tests
{
    private const string Config = """
        { "rules": [ { "id": "ARCH1004", "kind": "namespaceMatchesFolder" } ] }
        """;

    [Fact]
    public async Task NamespaceNotMatchingFolder_ReportsArch1004()
    {
        const string source = @"namespace Shop.Domain { public sealed class Order { } }";

        var diagnostics = await TestHarness.AnalyzeWithPathAsync(
            source,
            new ArchitectureAnalyzer(),
            path: "C:/repo/src/Shop.Domain/Entities/Order.cs",
            projectDirectory: "C:/repo/src/Shop.Domain",
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1004");
    }

    [Fact]
    public async Task NamespaceMatchingFolder_DoesNotReport()
    {
        const string source = @"namespace Shop.Domain.Entities { public sealed class Order { } }";

        var diagnostics = await TestHarness.AnalyzeWithPathAsync(
            source,
            new ArchitectureAnalyzer(),
            path: "C:/repo/src/Shop.Domain/Entities/Order.cs",
            projectDirectory: "C:/repo/src/Shop.Domain",
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1004");
    }

    [Fact]
    public async Task MultiLevelFolders_AreMatched()
    {
        const string source = @"namespace Shop.Domain.Entities { public sealed class Order { } }";

        var diagnostics = await TestHarness.AnalyzeWithPathAsync(
            source,
            new ArchitectureAnalyzer(),
            path: "C:/repo/src/Shop.Domain/Domain/Entities/Order.cs",
            projectDirectory: "C:/repo/src/Shop.Domain",
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1004");
    }

    [Fact]
    public async Task GeneratedFile_IsSkipped()
    {
        const string source = @"namespace Shop.Domain { public sealed class Order { } }";

        var diagnostics = await TestHarness.AnalyzeWithPathAsync(
            source,
            new ArchitectureAnalyzer(),
            path: "C:/repo/src/Shop.Domain/obj/Entities/Order.g.cs",
            projectDirectory: "C:/repo/src/Shop.Domain",
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1004");
    }

    [Fact]
    public async Task WithoutProjectDirectory_FallsBackToLastFolder()
    {
        const string source = @"namespace Shop.Domain { public sealed class Order { } }";

        var diagnostics = await TestHarness.AnalyzeWithPathAsync(
            source,
            new ArchitectureAnalyzer(),
            path: "C:/repo/Entities/Order.cs",
            projectDirectory: null,
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1004");
    }
}