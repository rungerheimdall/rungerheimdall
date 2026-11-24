using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1001Tests
{
    [Fact]
    public async Task ParamsMethodWithoutArguments_ReportsPerf1001()
    {
        const string source = """
            public static class Logger
            {
                public static void Log(string format, params object[] args) { }

                public static void Call() { Log("hello"); }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1001");
    }

    [Fact]
    public async Task ParamsMethodWithArguments_DoesNotReport()
    {
        const string source = """
            public static class Logger
            {
                public static void Log(string format, params object[] args) { }

                public static void Call() { Log("hello", 1); }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1001");
    }

    [Fact]
    public async Task NonParamsMethod_DoesNotReport()
    {
        const string source = """
            public static class Logger
            {
                public static void Log(string format) { }

                public static void Call() { Log("hello"); }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1001");
    }
}