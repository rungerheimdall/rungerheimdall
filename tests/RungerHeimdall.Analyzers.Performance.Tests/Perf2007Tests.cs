using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2007Tests
{
    [Fact]
    public async Task TaskFromResultInsideLoop_ReportsPerf2007()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static void M(int[] items)
                {
                    foreach (var item in items)
                    {
                        var task = Task.FromResult(item);
                    }
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2007");
    }

    [Fact]
    public async Task TaskFromResultOutsideLoop_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static Task<int> M(int value) => Task.FromResult(value);
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2007");
    }
}