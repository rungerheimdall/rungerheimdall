using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RungerHeimdall.Analyzers.Performance.Tests;

internal static class TestHarness
{
    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, DiagnosticAnalyzer analyzer)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
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