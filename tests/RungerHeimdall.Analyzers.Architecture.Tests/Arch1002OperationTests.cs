using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1002OperationTests
{
    private const string Config = """
        {
          "layers": [
            { "name": "Api", "namespaces": ["Shop.Api.*"], "rank": 3 },
            { "name": "Application", "namespaces": ["Shop.Application.*"], "rank": 1 }
          ],
          "rules": [
            { "id": "ARCH1002", "kind": "forbiddenType", "in": ["Api"], "types": ["Shop.Persistence.DbContext"] }
          ]
        }
        """;

    [Fact]
    public async Task MethodBodyCreatingForbiddenType_ReportsArch1002()
    {
        const string source = @"
namespace Shop.Persistence
{
    public class DbContext { }
}

namespace Shop.Api
{
    public sealed class OrdersEndpoint
    {
        public void Handle()
        {
            var db = new Shop.Persistence.DbContext();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1002");
    }

    [Fact]
    public async Task FieldAndMethodBodySameTarget_ReportsOnce()
    {
        const string source = @"
namespace Shop.Persistence
{
    public class DbContext { }
}

namespace Shop.Api
{
    public sealed class OrdersEndpoint
    {
        private readonly Shop.Persistence.DbContext _db;

        public OrdersEndpoint(Shop.Persistence.DbContext db)
        {
            _db = db;
        }

        public void Handle()
        {
            var db = new Shop.Persistence.DbContext();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        var matches = diagnostics.Where(diagnostic => diagnostic.Id == "ARCH1002").ToArray();
        Assert.Single(matches);
    }

    [Fact]
    public async Task MethodBodyInAllowedLayer_DoesNotReport()
    {
        const string source = @"
namespace Shop.Persistence
{
    public class DbContext { }
}

namespace Shop.Application
{
    public sealed class OrdersHandler
    {
        public void Handle()
        {
            var db = new Shop.Persistence.DbContext();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1002");
    }
}