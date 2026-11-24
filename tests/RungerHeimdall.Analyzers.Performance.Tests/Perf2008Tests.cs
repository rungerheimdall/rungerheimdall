using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2008Tests
{
    [Fact]
    public async Task ArrayResizeInsideLoop_ReportsPerf2008()
    {
        const string source = """
            using System;

            public static class C
            {
                public static void M(int[] items)
                {
                    foreach (var item in items)
                    {
                        Array.Resize(ref items, items.Length + 1);
                    }
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2008");
    }

    [Fact]
    public async Task ArrayResizeOutsideLoop_DoesNotReport()
    {
        const string source = """
            using System;

            public static class C
            {
                public static void M(ref int[] items)
                {
                    Array.Resize(ref items, items.Length + 1);
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2008");
    }
}