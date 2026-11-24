using System;
using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2003AsyncVoidRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2003",
        "Avoid async void",
        "'{0}' is async void. Exceptions cannot be observed. Return Task instead.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF2003";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type)
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (member is not IMethodSymbol method
                || !method.IsAsync
                || !method.ReturnsVoid
                || method.IsImplicitlyDeclared)
            {
                continue;
            }

            if (IsEventHandler(method))
            {
                continue;
            }

            var location = method.Locations.Length > 0 ? method.Locations[0] : Location.None;
            var diagnostic = Diagnostic.Create(RuleDescriptor, location, method.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsEventHandler(IMethodSymbol method)
    {
        if (!method.Name.StartsWith("On", StringComparison.Ordinal))
        {
            return false;
        }

        if (method.Parameters.Length != 2)
        {
            return false;
        }

        var sender = method.Parameters[0].Type;
        var args = method.Parameters[1].Type;

        return sender.SpecialType == SpecialType.System_Object
            && args.Name.EndsWith("EventArgs", StringComparison.Ordinal);
    }
}