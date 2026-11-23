using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Diagnostics;

public static class DiagnosticFactory
{
    public const string HelpLinkBase = "https://rungerheimdall.github.io/rungerheimdall/rules/";

    public static DiagnosticDescriptor Create(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity defaultSeverity,
        bool isEnabledByDefault = true)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category,
            defaultSeverity,
            isEnabledByDefault,
            helpLinkUri: HelpLinkBase + id);
    }
}