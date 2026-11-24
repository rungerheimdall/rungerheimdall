using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1001ConfigTests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Domain", "namespaces": ["Shop.Domain.*"], "rank": 0 },
            { "name": "Application", "namespaces": ["Shop.Application.*"], "rank": 1 },
            { "name": "Infrastructure", "namespaces": ["Shop.Infrastructure.*"], "rank": 2 },
            { "name": "Api", "namespaces": ["Shop.Api.*"], "rank": 3 }
          ],
          "rules": [
            { "id": "ARCH1001", "kind": "layerDependency", "from": "Domain", "deny": ["Infrastructure"], "severity": "warning" }
          ]
        }
        """;

    [Fact]
    public async Task FromLayerReferencingDeniedLayer_Reports()
    {
        const string source = @"
namespace Shop.Infrastructure { public sealed class SqlRepository { } }
namespace Shop.Domain { public sealed class OrderService { private readonly Shop.Infrastructure.SqlRepository _repository; } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task OtherLayerReferencingDeniedLayer_DoesNotReport()
    {
        const string source = @"
namespace Shop.Infrastructure { public sealed class SqlRepository { } }
namespace Shop.Application { public sealed class OrderHandler { private readonly Shop.Infrastructure.SqlRepository _repository; } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task FromLayerReferencingNonDeniedLayer_DoesNotReport()
    {
        const string source = @"
namespace Shop.Api { public sealed class OrdersEndpoint { } }
namespace Shop.Domain { public sealed class OrderService { private readonly Shop.Api.OrdersEndpoint _endpoint; } }";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }
}