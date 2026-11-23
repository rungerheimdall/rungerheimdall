using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1002Tests
{
    [Fact]
    public async Task CountOnList_ReportsPerf1002()
    {
        const string source = @"
using System.Collections.Generic;
using System.Linq;

public static class C
{
    public static int M(List<int> items) => items.Count();
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1002");
    }

    [Fact]
    public async Task CountOnEnumerable_DoesNotReport()
    {
        const string source = @"
using System.Collections.Generic;
using System.Linq;

public static class C
{
    public static int M(IEnumerable<int> items) => items.Count();
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1002");
    }

    [Fact]
    public async Task CountWithPredicate_DoesNotReport()
    {
        const string source = @"
using System.Collections.Generic;
using System.Linq;

public static class C
{
    public static int M(List<int> items) => items.Count(x => x > 0);
}";

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1002");
    }
}