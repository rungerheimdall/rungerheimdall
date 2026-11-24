using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1007Tests
{
    private const string Config = """
        { "rules": [ { "id": "ARCH1007", "kind": "noStaticMutableState" } ] }
        """;

    [Fact]
    public async Task StaticMutableField_ReportsArch1007()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Cache
    {
        public static int Counter;
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1007");
    }

    [Fact]
    public async Task StaticReadOnlyField_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Cache
    {
        public static readonly object Sync = new object();
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1007");
    }

    [Fact]
    public async Task InstanceField_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Cache
    {
        private int _count;
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1007");
    }

    [Fact]
    public async Task ConstField_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Limits
    {
        public const int MaxItems = 100;
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1007");
    }
}