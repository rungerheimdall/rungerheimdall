using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Json;

namespace RungerHeimdall.Engine.Configuration;

public sealed class BaselineStore
{
    public const string FileName = "heimdall-baseline.json";

    private readonly HashSet<string> _byType;
    private readonly HashSet<string> _byFile;
    private readonly string? _projectDirectory;

    private BaselineStore(
        HashSet<string> byType,
        HashSet<string> byFile,
        string? projectDirectory)
    {
        _byType = byType;
        _byFile = byFile;
        _projectDirectory = projectDirectory;
    }

    public static BaselineStore Empty { get; } = new BaselineStore(
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        null);

    public int Count => _byType.Count + _byFile.Count;

    public static BaselineStore Load(
        IEnumerable<AdditionalText>? files,
        CancellationToken cancellationToken,
        string? projectDirectory,
        out string? error)
    {
        error = null;

        if (files is null)
        {
            return Empty;
        }

        foreach (var file in files)
        {
            if (!IsBaselineFile(file.Path))
            {
                continue;
            }

            var text = file.GetText(cancellationToken);
            if (text is null)
            {
                error = $"Baseline file '{file.Path}' could not be read.";
                return Empty;
            }

            try
            {
                var root = JsonValue.Parse(text.ToString());
                if (root is not JsonObject rootObject)
                {
                    error = $"Baseline file '{file.Path}' must contain a JSON object.";
                    return Empty;
                }

                var byType = new HashSet<string>(StringComparer.Ordinal);
                var byFile = new HashSet<string>(StringComparer.Ordinal);
                var entries = rootObject.GetArray("entries");

                if (entries is not null)
                {
                    foreach (var item in entries.Items)
                    {
                        if (item is not JsonObject entry)
                        {
                            continue;
                        }

                        var ruleId = entry.GetString("ruleId");
                        if (string.IsNullOrWhiteSpace(ruleId))
                        {
                            continue;
                        }

                        var containingType = entry.GetString("containingType");
                        if (!string.IsNullOrWhiteSpace(containingType))
                        {
                            byType.Add(ruleId + "|" + containingType);
                        }

                        var filePath = entry.GetString("file");
                        if (!string.IsNullOrWhiteSpace(filePath))
                        {
                            byFile.Add(ruleId + "|" + Normalize(filePath!));
                        }
                    }
                }

                return new BaselineStore(byType, byFile, projectDirectory);
            }
            catch (FormatException exception)
            {
                error = $"Baseline file '{file.Path}' is invalid: {exception.Message}";
                return Empty;
            }
        }

        return Empty;
    }

    public bool IsSuppressed(string ruleId, ISymbol? containingSymbol, Location location)
    {
        if (_byType.Count > 0 && containingSymbol is not null)
        {
            var typeName = GetTypeName(containingSymbol);
            if (_byType.Contains(ruleId + "|" + typeName))
            {
                return true;
            }
        }

        if (_byFile.Count > 0 && location.IsInSource)
        {
            var relative = ToRelative(location.GetLineSpan().Path);
            if (relative is not null && _byFile.Contains(ruleId + "|" + relative))
            {
                return true;
            }
        }

        return false;
    }

    private string? ToRelative(string path)
    {
        var normalized = Normalize(path);
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(_projectDirectory))
        {
            var root = Normalize(_projectDirectory!).TrimEnd('/');
            if (normalized.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(root.Length + 1);
            }
        }

        return normalized;
    }

    private static string GetTypeName(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol type)
        {
            return type.ToDisplayString();
        }

        return symbol.ContainingType?.ToDisplayString() ?? symbol.ToDisplayString();
    }

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool IsBaselineFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var normalized = Normalize(path);
        var separator = normalized.LastIndexOf('/');
        var name = separator >= 0 ? normalized.Substring(separator + 1) : normalized;

        return name.Equals(FileName, StringComparison.OrdinalIgnoreCase);
    }
}