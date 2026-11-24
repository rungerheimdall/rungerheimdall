using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2005Tests
{
    [Fact]
    public async Task EnumerableEnumeratedTwice_ReportsPerf2005()
    {
        const string source = """
            using System.Collections.Generic;

            public static class C
            {
                public static int M(IEnumerable<int> items)
                {
                    var total = 0;
                    foreach (var item in items) total += item;
                    foreach (var item in items) total += item;
                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2005");
    }

    [Fact]
    public async Task ListEnumeratedTwice_DoesNotReport()
    {
        const string source = """
            using System.Collections.Generic;

            public static class C
            {
                public static int M(List<int> items)
                {
                    var total = 0;
                    foreach (var item in items) total += item;
                    foreach (var item in items) total += item;
                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2005");
    }

    [Fact]
    public async Task EnumerableEnumeratedOnce_DoesNotReport()
    {
        const string source = """
            using System.Collections.Generic;

            public static class C
            {
                public static int M(IEnumerable<int> items)
                {
                    var total = 0;
                    foreach (var item in items) total += item;
                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2005");
    }
}