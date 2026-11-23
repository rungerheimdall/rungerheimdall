using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Json;

namespace RungerHeimdall.Engine.Configuration;

public static class ConfigLoader
{
    public const string FileName = "architecture.json";

    public static bool TryLoad(
        IEnumerable<AdditionalText>? files,
        CancellationToken cancellationToken,
        out ArchitectureConfig? config,
        out string? error)
    {
        config = null;
        error = null;

        if (files is null)
        {
            return false;
        }

        foreach (var file in files)
        {
            if (!IsConfigFile(file.Path))
            {
                continue;
            }

            var text = file.GetText(cancellationToken);
            if (text is null)
            {
                error = $"Configuration file '{file.Path}' could not be read.";
                return true;
            }

            try
            {
                var root = JsonValue.Parse(text.ToString());
                if (root is not JsonObject rootObject)
                {
                    error = $"Configuration file '{file.Path}' must contain a JSON object.";
                    return true;
                }

                config = ArchitectureConfigReader.Read(rootObject);
                return true;
            }
            catch (FormatException exception)
            {
                error = $"Configuration file '{file.Path}' is invalid: {exception.Message}";
                return true;
            }
        }

        return false;
    }

    private static bool IsConfigFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');
        var separator = normalized.LastIndexOf('/');
        var name = separator >= 0 ? normalized.Substring(separator + 1) : normalized;

        return name.Equals(FileName, StringComparison.OrdinalIgnoreCase);
    }
}