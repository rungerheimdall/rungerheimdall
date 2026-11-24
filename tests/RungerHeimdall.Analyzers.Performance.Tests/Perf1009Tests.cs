using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1009Tests
{
    [Fact]
    public async Task SubstringForParse_ReportsPerf1009()
    {
        const string source = """
            public static class C
            {
                public static int M(string value) => int.Parse(value.Substring(0, 2));
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1009");
    }

    [Fact]
    public async Task SubstringForComparison_ReportsPerf1009()
    {
        const string source = """
            public static class C
            {
                public static bool M(string value) => value.Substring(0, 2).Equals("ab");
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1009");
    }

    [Fact]
    public async Task SubstringAssignedToVariable_DoesNotReport()
    {
        const string source = """
            public static class C
            {
                public static string M(string value)
                {
                    var part = value.Substring(0, 2);
                    return part;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1009");
    }
}