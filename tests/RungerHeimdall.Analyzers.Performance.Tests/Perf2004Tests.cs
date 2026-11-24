using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2004Tests
{
    [Fact]
    public async Task TaskResult_ReportsPerf2004()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static int M(Task<int> task) => task.Result;
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2004");
    }

    [Fact]
    public async Task TaskWait_ReportsPerf2004()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static void M(Task task) => task.Wait();
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2004");
    }

    [Fact]
    public async Task AwaitTask_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task<int> M(Task<int> task) => await task;
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2004");
    }
}