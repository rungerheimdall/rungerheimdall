using System;
using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Diagnostics;

public static class SeverityParser
{
    public static DiagnosticSeverity? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        switch (value!.ToLowerInvariant())
        {
            case "error":
                return DiagnosticSeverity.Error;
            case "warning":
                return DiagnosticSeverity.Warning;
            case "suggestion":
            case "info":
                return DiagnosticSeverity.Info;
            case "hidden":
                return DiagnosticSeverity.Hidden;
            default:
                return null;
        }
    }
}