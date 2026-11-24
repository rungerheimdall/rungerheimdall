using System.Linq;
using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class Perf1008Tests
{
    [Fact]
    public async Task StructWithoutGenericInterface_ReportsPerf1008()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            public struct Plain
            {
                public int X;
            }

            public static class C
            {
                public static bool M(Plain a, Plain b) => EqualityComparer<Plain>.Default.Equals(a, b);
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "PERF1008");
    }

    [Fact]
    public async Task StructImplementingEquatable_DoesNotReport()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            public struct Equatable : IEquatable<Equatable>
            {
                public int X;

                public bool Equals(Equatable other) => X == other.X;

                public override bool Equals(object? obj) => obj is Equatable other && Equals(other);

                public override int GetHashCode() => X;
            }

            public static class C
            {
                public static bool M(Equatable a, Equatable b) => EqualityComparer<Equatable>.Default.Equals(a, b);
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1008");
    }

    [Fact]
    public async Task ReferenceType_DoesNotReport()
    {
        const string source = """
            using System.Collections.Generic;

            public static class C
            {
                public static bool M(string a, string b) => EqualityComparer<string>.Default.Equals(a, b);
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1008");
    }

    [Fact]
    public async Task StructComparerWithoutComparable_ReportsPerf1008()
    {
        const string source = """
            using System.Collections.Generic;

            public struct Plain
            {
                public int X;
            }

            public static class C
            {
                public static int M(Plain a, Plain b) => Comparer<Plain>.Default.Compare(a, b);
            }
            """;

        var diagnostics = await TestHarness.AnalyzeAsync(source, new PerformanceAnalyzer());

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "PERF1008");
    }
}