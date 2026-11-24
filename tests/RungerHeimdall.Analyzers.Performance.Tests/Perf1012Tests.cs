using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1012Tests
{
    [Fact]
    public async Task ToLowerInEquality_ReportsPerf1012()
    {
        const string source = """
            public static class C
            {
                public static bool M(string value) => value.ToLower() == "x";
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1012");
    }

    [Fact]
    public async Task ToUpperInsideContains_ReportsPerf1012()
    {
        const string source = """
            public static class C
            {
                public static bool M(string value) => value.ToUpper().Contains("X");
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1012");
    }

    [Fact]
    public async Task ToLowerWithoutComparison_DoesNotReport()
    {
        const string source = """
            public static class C
            {
                public static string M(string value) => value.ToLower();
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1012");
    }

    [Fact]
    public async Task ToLowerWithCulture_DoesNotReport()
    {
        const string source = """
            using System.Globalization;

            public static class C
            {
                public static bool M(string value) => value.ToLower(CultureInfo.InvariantCulture) == "x";
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1012");
    }
}