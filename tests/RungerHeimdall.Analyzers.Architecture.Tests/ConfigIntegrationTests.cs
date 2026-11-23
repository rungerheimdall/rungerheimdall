using Microsoft.CodeAnalysis;
using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class ConfigIntegrationTests
{
    [Fact]
    public async Task ConfiguredLayers_ReportArch1001()
    {
        const string config = """
            {
              "layers": [
                { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
                { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
              ]
            }
            """;

        const string source = @"
namespace Shop.Infrastructure
{
    public sealed class SqlRepo { }
}

namespace Shop.Domain
{
    public sealed class OrderService
    {
        private readonly Shop.Infrastructure.SqlRepo _repo;

        public OrderService(Shop.Infrastructure.SqlRepo repo)
        {
            _repo = repo;
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task ConfiguredSeverity_OverridesDefaultWarning()
    {
        const string config = """
            {
              "layers": [
                { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
                { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
              ],
              "rules": [
                { "id": "ARCH1001", "kind": "layerDependency", "severity": "error" }
              ]
            }
            """;

        const string source = @"
namespace Shop.Infrastructure { public sealed class SqlRepo { } }
namespace Shop.Domain { public sealed class OrderService { private readonly Shop.Infrastructure.SqlRepo _repo; public OrderService(Shop.Infrastructure.SqlRepo repo) { _repo = repo; } } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", config));

        var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == "ARCH1001"));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task InvalidConfig_ReportsHdl9002()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(
            "namespace Shop.Domain { public sealed class A { } }",
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", "{"));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "HDL9002");
    }
}