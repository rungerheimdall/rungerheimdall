using System.Linq;
using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1006Tests
{
    private const string Source = """
        using System;

        namespace RungerHeimdall
        {
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
            public sealed class HotPathAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
            public sealed class ColdPathAttribute : Attribute { }
        }

        public static class C
        {
            [RungerHeimdall.HotPath]
            public static long Hot() => DateTime.Now.Ticks;

            public static long Cold() => DateTime.Now.Ticks;

            [RungerHeimdall.HotPath]
            [RungerHeimdall.ColdPath]
            public static long Exempt() => DateTime.Now.Ticks;

            public static void Loop(int[] items)
            {
                foreach (var item in items)
                {
                    _ = DateTime.UtcNow;
                }
            }
        }
        """;

    [Fact]
    public async Task HotPathAndLoopReads_AreReported()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(Source, new PerformanceAnalyzer());

        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "PERF1006"));
    }

    [Fact]
    public async Task PlainMethod_DoesNotReport()
    {
        const string source = """
            using System;

            public static class C
            {
                public static long Read() => DateTime.Now.Ticks;
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }

    [Fact]
    public async Task TimeProviderUsage_DoesNotReport()
    {
        const string source = """
            using System;

            public static class C
            {
                [RungerHeimdall.HotPath]
                public static DateTime Read(TimeProvider time) => time.GetUtcNow().UtcDateTime;
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(
            """
            namespace RungerHeimdall
            {
                [System.AttributeUsage(System.AttributeTargets.Method)]
                public sealed class HotPathAttribute : System.Attribute { }
            }
            """ + source,
            new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }
}