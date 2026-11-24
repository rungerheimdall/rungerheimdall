using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2006ValueTaskReuseRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2006",
        "Avoid awaiting a ValueTask more than once",
        "'{0}' is awaited more than once in '{1}'. Convert it to a Task first or await it once.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF2006";

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

            if (member is not IMethodSymbol method)
            {
                continue;
            }

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var syntax = reference.GetSyntax(context.CancellationToken);
                var model = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
                AnalyzeMethod(syntax, model, method, context);
            }
        }
    }

    private static void AnalyzeMethod(
        SyntaxNode syntax,
        SemanticModel model,
        IMethodSymbol method,
        SymbolContext context)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var first = new Dictionary<string, IdentifierNameSyntax>(StringComparer.Ordinal);

        foreach (var awaitExpression in syntax.DescendantNodes().OfType<AwaitExpressionSyntax>())
        {
            if (awaitExpression.Expression is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            var name = identifier.Identifier.Text;
            counts[name] = counts.TryGetValue(name, out var count) ? count + 1 : 1;

            if (!first.ContainsKey(name))
            {
                first[name] = identifier;
            }
        }

        foreach (var pair in counts)
        {
            if (pair.Value < 2)
            {
                continue;
            }

            var identifier = first[pair.Key];
            var symbol = model.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
            var type = symbol switch
            {
                ILocalSymbol local => local.Type,
                IParameterSymbol parameter => parameter.Type,
                _ => null,
            };

            if (!IsValueTask(type))
            {
                continue;
            }

            var diagnostic = Diagnostic.Create(
                RuleDescriptor,
                identifier.GetLocation(),
                pair.Key,
                method.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsValueTask(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named || named.Name != "ValueTask")
        {
            return false;
        }

        return named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";
    }
}