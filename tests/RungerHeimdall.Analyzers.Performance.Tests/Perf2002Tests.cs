using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2002Tests
{
    [Fact]
    public async Task TaskRunInsideAsync_ReportsPerf2002()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task M()
                {
                    await Task.Run(() => { });
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2002");
    }

    [Fact]
    public async Task TaskRunInSyncMethod_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static Task M()
                {
                    return Task.Run(() => { });
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2002");
    }

    [Fact]
    public async Task AwaitWithoutTaskRun_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task M()
                {
                    await Task.Delay(1);
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2002");
    }
}