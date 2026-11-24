using System.Linq;
using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1011Tests
{
    [Fact]
    public async Task NonGenericEnumerableOfInts_ReportsPerf1011()
    {
        const string source = """
            using System.Collections;

            public static class C
            {
                public static int M(IEnumerable items)
                {
                    var total = 0;
                    foreach (int item in items)
                    {
                        total += item;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "PERF1011");
    }

    [Fact]
    public async Task GenericEnumerable_DoesNotReport()
    {
        const string source = """
            using System.Collections.Generic;

            public static class C
            {
                public static int M(IEnumerable<int> items)
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

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1011");
    }

    [Fact]
    public async Task NonGenericEnumerableOfObjects_DoesNotReport()
    {
        const string source = """
            using System.Collections;

            public static class C
            {
                public static int M(IEnumerable items)
                {
                    var total = 0;
                    foreach (string item in items)
                    {
                        total += item.Length;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1011");
    }

    [Fact]
    public async Task ArrayOfStructs_DoesNotReport()
    {
        const string source = """
            public struct Plain
            {
                public int X;
            }

            public static class C
            {
                public static int M(Plain[] items)
                {
                    var total = 0;
                    foreach (var item in items)
                    {
                        total += item.X;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1011");
    }
}