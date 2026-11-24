using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2003Tests
{
    [Fact]
    public async Task AsyncVoid_ReportsPerf2003()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async void M()
                {
                    await Task.Yield();
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2003");
    }

    [Fact]
    public async Task AsyncTask_DoesNotReport()
    {
        const string source = """
            using System.Threading.Tasks;

            public static class C
            {
                public static async Task M()
                {
                    await Task.Yield();
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2003");
    }

    [Fact]
    public async Task AsyncVoidEventHandler_DoesNotReport()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            public sealed class C
            {
                public async void OnTick(object sender, EventArgs e)
                {
                    await Task.Yield();
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2003");
    }
}