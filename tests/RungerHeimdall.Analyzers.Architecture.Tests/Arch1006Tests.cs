using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1006Tests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 }
          ],
          "rules": [
            { "id": "ARCH1006", "kind": "frameworkAllowList", "in": ["Domain"] }
          ]
        }
        """;

    [Fact]
    public async Task DomainReferencingVendor_ReportsArch1006()
    {
        const string source = @"
namespace Vendor.Library
{
    public sealed class Thing { }
}

namespace Shop.Domain
{
    public sealed class Order
    {
        private readonly Vendor.Library.Thing _thing;
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1006");
    }

    [Fact]
    public async Task DomainReferencingSystem_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Order
    {
        private readonly string _id;
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1006");
    }

    [Fact]
    public async Task DomainReferencingOwnLayer_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Aggregate { }

    public sealed class Order
    {
        private readonly Aggregate _aggregate;
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1006");
    }

    [Fact]
    public async Task AllowListedVendor_DoesNotReport()
    {
        const string source = @"
namespace Vendor.Library
{
    public sealed class Thing { }
}

namespace Shop.Domain
{
    public sealed class Order
    {
        private readonly Vendor.Library.Thing _thing;
    }
}";

        const string config = """
            {
              "layers": [
                { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 }
              ],
              "rules": [
                { "id": "ARCH1006", "kind": "frameworkAllowList", "in": ["Domain"], "allow": ["System", "System.*", "Vendor.*"] }
              ]
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1006");
    }
}