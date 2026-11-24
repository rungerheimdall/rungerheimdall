using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1003Tests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
            { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
          ],
          "rules": [
            { "id": "ARCH1003", "kind": "forbiddenNamespace", "in": ["Domain"], "deny": ["Shop.Infrastructure.*"] }
          ]
        }
        """;

    [Fact]
    public async Task DomainReferencingInfrastructure_ReportsArch1003()
    {
        const string source = @"
namespace Shop.Infrastructure
{
    public sealed class SqlRepository { }
}

namespace Shop.Domain
{
    public sealed class OrderService
    {
        private readonly Shop.Infrastructure.SqlRepository _repository;

        public OrderService(Shop.Infrastructure.SqlRepository repository)
        {
            _repository = repository;
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1003");
    }

    [Fact]
    public async Task InfrastructureReferencingDomain_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Order { }
}

namespace Shop.Infrastructure
{
    public sealed class OrderRepository
    {
        public Shop.Domain.Order Find() => new Shop.Domain.Order();
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1003");
    }

    [Fact]
    public async Task DomainReferencingAllowedNamespace_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Aggregate { }

    public sealed class OrderService
    {
        private readonly Aggregate _aggregate = new Aggregate();
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1003");
    }
}