using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class ExemptionTests
{
    private const string ConfigWithActiveExemption = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
            { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
          ],
          "exemptions": [
            { "scope": "type", "target": "Shop.Domain.OrderService", "reason": "migration in progress", "expiresOn": "2099-12-31" }
          ]
        }
        """;

    private const string ConfigWithExpiredExemption = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
            { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
          ],
          "exemptions": [
            { "scope": "type", "target": "Shop.Domain.OrderService", "reason": "migration in progress", "expiresOn": "2020-01-01" }
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
    public async Task ActiveExemption_SuppressesDiagnostic()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", ConfigWithActiveExemption));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task ExpiredExemption_DoesNotSuppressAndReportsHdl9001()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", ConfigWithExpiredExemption));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "HDL9001");
    }

    [Fact]
    public async Task ExemptionForOtherType_DoesNotSuppress()
    {
        const string config = """
            {
              "layers": [
                { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
                { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
              ],
              "exemptions": [
                { "scope": "type", "target": "Shop.Domain.SomeOtherType", "reason": "migration in progress" }
              ]
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task NamespaceExemption_SuppressesWholeNamespace()
    {
        const string config = """
            {
              "layers": [
                { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
                { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
              ],
              "exemptions": [
                { "scope": "namespace", "target": "Shop.Domain", "reason": "legacy module" }
              ]
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }
}