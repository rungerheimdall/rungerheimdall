using Xunit;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

public sealed class Arch1001Tests
{
    [Fact]
    public async Task DomainDependingOnInfrastructure_ReportsArch1001()
    {
        const string source = @"
namespace MyApp.Infrastructure
{
    public sealed class SqlRepository
    {
    }
}

namespace MyApp.Domain
{
    public sealed class OrderService
    {
        private readonly MyApp.Infrastructure.SqlRepository _repository;

        public OrderService(MyApp.Infrastructure.SqlRepository repository)
        {
            _repository = repository;
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new ArchitectureAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task InfrastructureDependingOnDomain_DoesNotReport()
    {
        const string source = @"
namespace MyApp.Domain
{
    public sealed class Order
    {
    }
}

namespace MyApp.Infrastructure
{
    public sealed class OrderRepository
    {
        public MyApp.Domain.Order Find()
        {
            return new MyApp.Domain.Order();
        }
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new ArchitectureAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task SameLayer_DoesNotReport()
    {
        const string source = @"
namespace MyApp.Domain
{
    public sealed class Aggregate
    {
    }

    public sealed class OrderService
    {
        private readonly Aggregate _aggregate = new Aggregate();
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new ArchitectureAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }

    [Fact]
    public async Task FrameworkTypes_DoNotReport()
    {
        const string source = @"
using System.Collections.Generic;

namespace MyApp.Domain
{
    public sealed class Order
    {
        public List<string> Lines { get; } = new List<string>();
    }
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new ArchitectureAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "ARCH1001");
    }
}