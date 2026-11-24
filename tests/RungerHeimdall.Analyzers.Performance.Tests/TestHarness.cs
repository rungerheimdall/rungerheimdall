using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace RungerHeimdall.Analyzers.Performance.Tests;

internal static class TestHarness
{
    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, DiagnosticAnalyzer analyzer)
    {
        return await AnalyzeAsync(source, analyzer, null, null);
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        string? path,
        string? projectDirectory,
        params AdditionalText[] additionalFiles)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path ?? string.Empty);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var options = new AnalyzerOptions(
            additionalFiles.ToImmutableArray(),
            new TestAnalyzerConfigOptionsProvider(projectDirectory));

        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer), options);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrEmpty(tpa))
        {
            return Array.Empty<MetadataReference>();
        }

        return tpa!.Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}

internal sealed class InMemoryAdditionalText : AdditionalText
{
    private readonly SourceText _text;

    public InMemoryAdditionalText(string path, string text)
    {
        Path = path;
        _text = SourceText.From(text, Encoding.UTF8);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return _text;
    }
}

internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptions _globalOptions;

    public TestAnalyzerConfigOptionsProvider(string? projectDirectory)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            values["build_property.MSBuildProjectDirectory"] = projectDirectory!;
        }

        _globalOptions = new TestAnalyzerConfigOptions(values);
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
}

internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    public static readonly TestAnalyzerConfigOptions Empty =
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    private readonly Dictionary<string, string> _values;

    public TestAnalyzerConfigOptions(Dictionary<string, string> values)
    {
        _values = values;
    }

    public override bool TryGetValue(string key, out string value)
    {
        if (_values.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }
}