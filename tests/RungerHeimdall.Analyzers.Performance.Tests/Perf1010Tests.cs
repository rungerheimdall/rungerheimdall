using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1010Tests
{
    [Fact]
    public async Task NewRegexInMethod_ReportsPerf1010()
    {
        const string source = """
            using System.Text.RegularExpressions;

            public static class C
            {
                public static bool M(string value)
                {
                    var regex = new Regex("^a");
                    return regex.IsMatch(value);
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1010");
    }

    [Fact]
    public async Task NewRegexInField_DoesNotReport()
    {
        const string source = """
            using System.Text.RegularExpressions;

            public static class C
            {
                private static readonly Regex Pattern = new Regex("^a");
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1010");
    }
}