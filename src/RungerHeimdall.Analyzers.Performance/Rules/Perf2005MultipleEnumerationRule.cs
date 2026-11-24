using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf2005MultipleEnumerationRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF2005",
        "Avoid enumerating an IEnumerable more than once",
        "'{0}' is enumerated more than once in '{1}'. Materialize it once or use a concrete collection.",
        "Performance",
        DiagnosticSeverity.Info);

    public string Id => "PERF2005";

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

        foreach (var statement in syntax.DescendantNodes().OfType<ForEachStatementSyntax>())
        {
            if (statement.Expression is not IdentifierNameSyntax identifier)
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

            if (!IsEnumerableOnly(type))
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

    private static bool IsEnumerableOnly(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named || named.TypeKind != TypeKind.Interface)
        {
            return false;
        }

        var definition = named.OriginalDefinition.ToDisplayString();
        return definition == "System.Collections.Generic.IEnumerable<T>"
            || definition == "System.Collections.IEnumerable";
    }
}