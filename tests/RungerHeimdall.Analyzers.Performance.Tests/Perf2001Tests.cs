using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf2001Tests
{
    private const string Prelude = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace RungerHeimdall
        {
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
            public sealed class HotPathAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
            public sealed class ColdPathAttribute : Attribute { }
        }
        """;

    [Fact]
    public async Task LinqInsideLoop_ReportsPerf2001()
    {
        var source = Prelude + """

            public static class C
            {
                public static int M(List<int> items)
                {
                    var total = 0;
                    foreach (var item in items)
                    {
                        total += items.Where(x => x > 0).Count();
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2001");
    }

    [Fact]
    public async Task LinqOutsideLoop_DoesNotReport()
    {
        var source = Prelude + """

            public static class C
            {
                public static int M(List<int> items)
                {
                    return items.Where(x => x > 0).Count();
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2001");
    }

    [Fact]
    public async Task ColdPathMethod_DoesNotReport()
    {
        var source = Prelude + """

            public static class C
            {
                [RungerHeimdall.ColdPath]
                public static int M(List<int> items)
                {
                    var total = 0;
                    foreach (var item in items)
                    {
                        total += items.Where(x => x > 0).Count();
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF2001");
    }

    [Fact]
    public async Task HotPathMethod_StillReportsInsideLoop()
    {
        var source = Prelude + """

            public static class C
            {
                [RungerHeimdall.HotPath]
                public static int M(List<int> items)
                {
                    var total = 0;
                    foreach (var item in items)
                    {
                        total += items.Where(x => x > 0).Count();
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF2001");
    }
}