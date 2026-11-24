using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1005Tests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
            { "name": "Application", "namespaces": ["Shop.Application.*"], "rank": 1 }
          ],
          "rules": [
            { "id": "ARCH1005", "kind": "requiredInterface", "in": ["Domain"], "suffix": ["Repository", "Service"], "mustBeInterface": true }
          ]
        }
        """;

    [Fact]
    public async Task DomainClassRepository_ReportsArch1005()
    {
        const string source = @"namespace Shop.Domain { public sealed class OrderRepository { } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1005");
    }

    [Fact]
    public async Task DomainInterfaceRepository_DoesNotReport()
    {
        const string source = @"namespace Shop.Domain { public interface IOrderRepository { } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1005");
    }

    [Fact]
    public async Task ApplicationClassRepository_DoesNotReport()
    {
        const string source = @"namespace Shop.Application { public sealed class OrderRepository { } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1005");
    }

    [Fact]
    public async Task DomainClassService_ReportsArch1005()
    {
        const string source = @"namespace Shop.Domain { public sealed class PricingService { } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1005");
    }
}