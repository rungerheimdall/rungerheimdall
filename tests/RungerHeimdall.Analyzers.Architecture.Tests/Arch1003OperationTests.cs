using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1003OperationTests
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
    public async Task MethodBodyInvokingInfrastructure_ReportsArch1003()
    {
        const string source = @"
namespace Shop.Infrastructure
{
    public static class Sql
    {
        public static void Run() { }
    }
}

namespace Shop.Domain
{
    public sealed class OrderService
    {
        public void Handle()
        {
            Shop.Infrastructure.Sql.Run();
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
    public async Task MethodBodyInInfrastructure_DoesNotReport()
    {
        const string source = @"
namespace Shop.Infrastructure
{
    public static class Sql
    {
        public static void Run() { }
    }

    public sealed class Worker
    {
        public void Execute()
        {
            Sql.Run();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1003");
    }

    [Fact]
    public async Task FieldAndMethodBodySameNamespace_ReportsOnce()
    {
        const string source = @"
namespace Shop.Infrastructure
{
    public static class Sql
    {
        public static void Run() { }
    }
}

namespace Shop.Domain
{
    public sealed class OrderService
    {
        private readonly Shop.Infrastructure.Sql _sql = new Shop.Infrastructure.Sql();

        public void Handle()
        {
            Shop.Infrastructure.Sql.Run();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(
            source,
            new ArchitectureAnalyzer(),
            new InMemoryAdditionalText("/repo/architecture.json", Config));

        var matches = diagnostics.Where(diagnostic => diagnostic.Id == "ARCH1003").ToArray();
        Assert.Single(matches);
    }
}