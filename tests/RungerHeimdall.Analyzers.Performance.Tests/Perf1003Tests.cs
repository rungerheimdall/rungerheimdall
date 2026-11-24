using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1003Tests
{
    [Fact]
    public async Task ToListInsideForeach_ReportsPerf1003()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;

            public static class C
            {
                public static int M(List<int> items)
                {
                    var total = 0;
                    foreach (var item in items.ToList())
                    {
                        total += item;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1003");
    }

    [Fact]
    public async Task DirectForeach_DoesNotReport()
    {
        const string source = """
            using System.Collections.Generic;

            public static class C
            {
                public static int M(List<int> items)
                {
                    var total = 0;
                    foreach (var item in items)
                    {
                        total += item;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1003");
    }
}