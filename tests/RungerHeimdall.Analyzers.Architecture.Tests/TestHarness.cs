using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace RungerHeimdall.Analyzers.Architecture.Tests;

internal static class TestHarness
{
    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        params AdditionalText[] additionalFiles)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withAnalyzers = additionalFiles.Length == 0
            ? compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            : compilation.WithAnalyzers(ImmutableArray.Create(analyzer), new AnalyzerOptions(additionalFiles.ToImmutableArray()));

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