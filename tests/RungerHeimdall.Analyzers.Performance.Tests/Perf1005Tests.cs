using System.Linq;
using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1005Tests
{
    [Fact]
    public async Task CapturingLoopVariable_ReportsPerf1005()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            public static class C
            {
                public static void M(List<string> items)
                {
                    foreach (var item in items)
                    {
                        Action a = () => Console.WriteLine(item);
                        a();
                    }
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "PERF1005");
    }

    [Fact]
    public async Task CapturingForLoopVariable_ReportsPerf1005()
    {
        const string source = """
            using System;

            public static class C
            {
                public static void M()
                {
                    for (var i = 0; i < 10; i++)
                    {
                        Action a = () => Console.WriteLine(i);
                        a();
                    }
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "PERF1005");
    }

    [Fact]
    public async Task LambdaWithoutLoopVariable_DoesNotReport()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            public static class C
            {
                public static void M(List<string> items)
                {
                    foreach (var item in items)
                    {
                        Action a = () => Console.WriteLine("x");
                        a();
                    }
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1005");
    }

    [Fact]
    public async Task LambdaOutsideLoop_DoesNotReport()
    {
        const string source = """
            using System;

            public static class C
            {
                public static Action M()
                {
                    var value = 1;
                    return () => Console.WriteLine(value);
                }
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1005");
    }
}