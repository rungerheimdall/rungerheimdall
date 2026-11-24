using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1004Tests
{
    [Fact]
    public async Task StringConcatInsideLoop_ReportsPerf1004()
    {
        const string source = """
            public static class C
            {
                public static string M(string[] items)
                {
                    var result = "";
                    foreach (var item in items)
                    {
                        result += item;
                    }

                    return result;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }

    [Fact]
    public async Task StringConcatOutsideLoop_DoesNotReport()
    {
        const string source = """
            public static class C
            {
                public static string M(string a, string b)
                {
                    var result = a + b;
                    return result;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }

    [Fact]
    public async Task NumericAddInsideLoop_DoesNotReport()
    {
        const string source = """
            public static class C
            {
                public static int M(int[] items)
                {
                    var total = 0;
                    foreach (var item in items)
                    {
                        total += item;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }
}