using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class BaselineTests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
            { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
          ]
        }
        """;

    private const string Source = @"
namespace Shop.Infrastructure
{
    public sealed class SqlRepository { }
}

namespace Shop.Domain
{
    public sealed class OrderService
    {
        private readonly Shop.Infrastructure.SqlRepository _repository;
    }
}";

    [Fact]
    public async Task TypeBaseline_SuppressesDiagnostic()
    {
        const string baseline = """
            {
              "version": 1,
              "entries": [
                { "ruleId": "ARCH1001", "containingType": "Shop.Domain.OrderService" }
              ]
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config),
            new InMemoryAdditionalText("/repo/heimdall-baseline.json", baseline));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task FileBaseline_SuppressesDiagnostic()
    {
        const string baseline = """
            {
              "version": 1,
              "entries": [
                { "ruleId": "ARCH1001", "file": "Shop.Domain/OrderService.cs" }
              ]
            }
            """;

        var diagnostics = await TestHarness.AnalyzeWithPathAsync(
            Source,
            new ArchitectureAnalyzer(),
            "C:/repo/src/Shop.Domain/OrderService.cs",
            "C:/repo/src",
            new InMemoryAdditionalText("/repo/architecture.json", Config),
            new InMemoryAdditionalText("/repo/heimdall-baseline.json", baseline));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task BaselineForOtherType_DoesNotSuppress()
    {
        const string baseline = """
            {
              "version": 1,
              "entries": [
                { "ruleId": "ARCH1001", "containingType": "Shop.Domain.OtherType" }
              ]
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config),
            new InMemoryAdditionalText("/repo/heimdall-baseline.json", baseline));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task InvalidBaseline_ReportsHdl9003()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config),
            new InMemoryAdditionalText("/repo/heimdall-baseline.json", "{"));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "HDL9003");
    }
}