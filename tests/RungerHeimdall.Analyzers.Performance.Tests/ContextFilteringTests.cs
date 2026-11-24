using Xunit;

namespace RungerHeimdall.Analyzers.Performance.Tests;

public sealed class ContextFilteringTests
{
    private const string ProjectDirectory = "C:/repo/Shop";

    private const string Source = """
        namespace Shop.Domain;

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

    [Fact]
    public async Task BaselineFile_SuppressesPerf1004()
    {
        var baseline = new InMemoryAdditionalText(
            ProjectDirectory + "/heimdall-baseline.json",
            """
            {
              "version": 1,
              "entries": [
                { "ruleId": "PERF1004", "file": "C.cs" }
              ]
            }
            """);

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new PerformanceAnalyzer(),
            ProjectDirectory + "/C.cs",
            ProjectDirectory,
            baseline);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }

    [Fact]
    public async Task BaselineEntryForAnotherRule_DoesNotSuppressPerf1004()
    {
        var baseline = new InMemoryAdditionalText(
            ProjectDirectory + "/heimdall-baseline.json",
            """
            {
              "version": 1,
              "entries": [
                { "ruleId": "ARCH1001", "file": "C.cs" }
              ]
            }
            """);

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new PerformanceAnalyzer(),
            ProjectDirectory + "/C.cs",
            ProjectDirectory,
            baseline);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }

    [Fact]
    public async Task TypeExemption_SuppressesPerf1004()
    {
        var configuration = new InMemoryAdditionalText(
            ProjectDirectory + "/architecture.json",
            """
            {
              "version": 1,
              "exemptions": [
                { "scope": "type", "target": "Shop.Domain.C", "reason": "migration in progress" }
              ]
            }
            """);

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new PerformanceAnalyzer(),
            ProjectDirectory + "/C.cs",
            ProjectDirectory,
            configuration);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }

    [Fact]
    public async Task ExpiredExemption_DoesNotSuppressPerf1004()
    {
        var configuration = new InMemoryAdditionalText(
            ProjectDirectory + "/architecture.json",
            """
            {
              "version": 1,
              "exemptions": [
                { "scope": "type", "target": "Shop.Domain.C", "reason": "migration in progress", "expiresOn": "2020-01-01" }
              ]
            }
            """);

        var diagnostics = await TestHarness.AnalyzeAsync(
            Source,
            new PerformanceAnalyzer(),
            ProjectDirectory + "/C.cs",
            ProjectDirectory,
            configuration);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF1004");
    }
}