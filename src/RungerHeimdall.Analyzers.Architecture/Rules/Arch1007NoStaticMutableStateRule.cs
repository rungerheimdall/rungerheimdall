using Microsoft.CodeAnalysis;
using RungerHeimdall.Engine.Configuration;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Architecture.Rules;

public sealed class Arch1007NoStaticMutableStateRule : ISymbolRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "ARCH1007",
        "Avoid static mutable state",
        "'{0}.{1}' is mutable static state. Prefer instance state or an injected abstraction.",
        "Architecture",
        DiagnosticSeverity.Warning);

    private readonly ScopeMatcher _scope;
    private readonly DiagnosticSeverity? _severityOverride;

    public Arch1007NoStaticMutableStateRule(LayerIndex layers, RuleConfig config)
    {
        _scope = new ScopeMatcher(layers, config.In);
        _severityOverride = SeverityParser.Parse(config.Severity);
    }

    public string Id => "ARCH1007";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(SymbolContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type)
        {
            return;
        }

        if (!_scope.IsEmpty && !_scope.Matches(type))
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (member is not IFieldSymbol field)
            {
                continue;
            }

            if (field.IsImplicitlyDeclared || !field.IsStatic || field.IsReadOnly || field.IsConst)
            {
                continue;
            }

            var location = field.Locations.Length > 0 ? field.Locations[0] : Location.None;
            var diagnostic = _severityOverride.HasValue
                ? Diagnostic.Create(
                    RuleDescriptor,
                    location,
                    _severityOverride.Value,
                    additionalLocations: null,
                    properties: null,
                    type.Name,
                    field.Name)
                : Diagnostic.Create(RuleDescriptor, location, type.Name, field.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}