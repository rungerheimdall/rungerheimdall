using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class HotPathLayeringTests
{
    private const string MinimalApiSource = """
        using System;
        using Microsoft.AspNetCore.Builder;

        namespace Microsoft.AspNetCore.Builder
        {
            public static class EndpointRouteBuilderExtensions
            {
                public static void MapGet(this object builder, string pattern, Action handler) { }
            }
        }

        public static class C
        {
            public static void Register(object app)
            {
                app.MapGet("/", () => { var _ = DateTime.Now; });
            }
        }
        """;

    private const string IteratorSource = """
        using System;
        using System.Collections.Generic;

        public static class C
        {
            public static IEnumerable<long> Stamps()
            {
                yield return DateTime.Now.Ticks;
            }
        }
        """;

    private const string HeuristicSource = """
        using System;

        public static class C
        {
            public static long ProcessOrder() => DateTime.Now.Ticks;
        }
        """;

    private const string PropagationSource = """
        using System;

        namespace RungerHeimdall
        {
            [AttributeUsage(AttributeTargets.Method)]
            public sealed class HotPathAttribute : Attribute { }
        }

        public static class C
        {
            [RungerHeimdall.HotPath]
            public static long Seed() => 1;

            public static long Caller() => DateTime.Now.Ticks + Seed();
        }
        """;

    [Fact]
    public async Task MinimalApiHandler_IsTreatedAsHotPath()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(MinimalApiSource, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }

    [Fact]
    public async Task Iterator_IsTreatedAsHotPath()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(IteratorSource, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }

    [Fact]
    public async Task NamingHeuristic_IsOffByDefault()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(HeuristicSource, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }

    [Fact]
    public async Task NamingHeuristic_ReportsWhenEnabled()
    {
        var config = new Dictionary<string, string>
        {
            ["rungerheimdall_hotpath_heuristic"] = "true",
        };

        var diagnostics = await TestHarness.AnalyzeWithConfigAsync(
            HeuristicSource,
            new PerformanceAnalyzer(),
            config);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }

    [Fact]
    public async Task SingleLayerPropagation_IsOnByDefault()
    {
        var diagnostics = await TestHarness.AnalyzeAsync(PropagationSource, new PerformanceAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }

    [Fact]
    public async Task SingleLayerPropagation_CanBeDisabled()
    {
        var config = new Dictionary<string, string>
        {
            ["rungerheimdall_hotpath_propagation"] = "false",
        };

        var diagnostics = await TestHarness.AnalyzeWithConfigAsync(
            PropagationSource,
            new PerformanceAnalyzer(),
            config);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1006");
    }
}