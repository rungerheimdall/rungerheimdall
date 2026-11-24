using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1008Tests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Api", "namespaces": ["Shop.Api.*"], "rank": 3 },
            { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 }
          ],
          "rules": [
            { "id": "ARCH1008", "kind": "endpointForbiddenCreation", "in": ["Api"], "deny": ["Shop.Infrastructure.*"] }
          ]
        }
        """;

    [Fact]
    public async Task ApiCreatingInfrastructure_ReportsArch1008()
    {
        const string source = @"
namespace Shop.Infrastructure
{
    public sealed class SqlRepository { }
}

namespace Shop.Api
{
    public sealed class OrdersEndpoint
    {
        public void Handle()
        {
            var repository = new Shop.Infrastructure.SqlRepository();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1008");
    }

    [Fact]
    public async Task ApiCreatingDomainType_DoesNotReport()
    {
        const string source = @"
namespace Shop.Domain
{
    public sealed class Order { }
}

namespace Shop.Api
{
    public sealed class OrdersEndpoint
    {
        public void Handle()
        {
            var order = new Shop.Domain.Order();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1008");
    }

    [Fact]
    public async Task InfrastructureCreatingInfrastructure_DoesNotReport()
    {
        const string source = @"
namespace Shop.Infrastructure
{
    public sealed class SqlRepository { }

    public sealed class RepositoryFactory
    {
        public SqlRepository Create()
        {
            return new SqlRepository();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1008");
    }
}