using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace RungerHeimdall.Cli;

internal sealed class FileAdditionalText : AdditionalText
{
    private readonly string _path;

    public FileAdditionalText(string path)
    {
        _path = path;
    }

    public override string Path => _path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(File.ReadAllText(_path, Encoding.UTF8), Encoding.UTF8);
    }
}

internal sealed class CliOptionsProvider : AnalyzerConfigOptionsProvider
{
    private static readonly Dictionary<string, string> Empty =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly AnalyzerConfigOptions _global;

    public CliOptionsProvider(string? projectDirectory)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            values["build_property.MSBuildProjectDirectory"] = projectDirectory!;
        }

        _global = new SimpleOptions(values);
    }

    public override AnalyzerConfigOptions GlobalOptions => _global;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new SimpleOptions(Empty);

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new SimpleOptions(Empty);

    private sealed class SimpleOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        public SimpleOptions(Dictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);
    }
}

internal static class CliAnalyzerOptions
{
    private static readonly string[] ConfigurationFiles =
    {
        "architecture.json",
        "heimdall-baseline.json",
    };

    public static AnalyzerOptions Create(string? projectDirectory)
    {
        var builder = ImmutableArray.CreateBuilder<AdditionalText>();

        if (!string.IsNullOrEmpty(projectDirectory))
        {
            foreach (var name in ConfigurationFiles)
            {
                var path = Path.Combine(projectDirectory!, name);
                if (File.Exists(path))
                {
                    builder.Add(new FileAdditionalText(path));
                }
            }
        }

        return new AnalyzerOptions(builder.ToImmutable(), new CliOptionsProvider(projectDirectory));
    }
}