using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2006Tests
{
    [Fact]
    public async Task ValueTaskAwaitedTwice_ReportsPerf2006()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task<int> M(ValueTask<int> value)
                {
                    var a = await value;
                    var b = await value;
                    return a + b;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2006");
    }

    [Fact]
    public async Task TaskAwaitedTwice_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task<int> M(Task<int> task)
                {
                    var a = await task;
                    var b = await task;
                    return a + b;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2006");
    }

    [Fact]
    public async Task ValueTaskAwaitedOnce_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task<int> M(ValueTask<int> value)
                {
                    return await value;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2006");
    }
}