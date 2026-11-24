using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1007Tests
{
    [Fact]
    public async Task HasFlag_ReportsPerf1007()
    {
        const string source = """
            using System;

            [Flags]
            public enum Options
            {
                None = 0,
                A = 1,
                B = 2
            }

            public static class C
            {
                public static bool M(Options options) => options.HasFlag(Options.A);
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1007");
    }

    [Fact]
    public async Task BitwiseComparison_DoesNotReport()
    {
        const string source = """
            using System;

            [Flags]
            public enum Options
            {
                None = 0,
                A = 1,
                B = 2
            }

            public static class C
            {
                public static bool M(Options options) => (options & Options.A) != 0;
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1007");
    }
}