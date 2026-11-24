using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1010RegexSourceGeneratedRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1010",
        "Prefer GeneratedRegex over new Regex",
        "'Regex' is constructed at runtime. Use [GeneratedRegex] to generate the matcher at compile time.",
        "Performance",
        DiagnosticSeverity.Warning);

    public string Id => "PERF1010";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IObjectCreationOperation creation)
        {
            return;
        }

        var type = creation.Constructor?.ContainingType;
        if (type is null)
        {
            return;
        }

        if (!type.ToDisplayString().Equals("System.Text.RegularExpressions.Regex", StringComparison.Ordinal))
        {
            return;
        }

        if (context.OwningSymbol is not IMethodSymbol)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            RuleDescriptor,
            creation.Syntax.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}