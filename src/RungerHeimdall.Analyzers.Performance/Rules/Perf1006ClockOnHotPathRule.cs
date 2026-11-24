using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RungerHeimdall.Engine.Diagnostics;
using RungerHeimdall.Engine.HotPath;
using RungerHeimdall.Engine.Operations;
using RungerHeimdall.Engine.Rules;

namespace RungerHeimdall.Analyzers.Performance.Rules;

public sealed class Perf1006ClockOnHotPathRule : IOperationRule
{
    public static readonly DiagnosticDescriptor RuleDescriptor = DiagnosticFactory.Create(
        "PERF1006",
        "Avoid reading the system clock on a hot path",
        "'{0}' reads the system clock on a hot path. Inject TimeProvider and call GetUtcNow instead.",
        "Performance",
        DiagnosticSeverity.Info);

    private static readonly HashSet<string> ClockProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "Now",
        "UtcNow",
        "Today",
    };

    private static readonly HashSet<string> ClockTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.DateTime",
        "System.DateTimeOffset",
    };

    private readonly HotPathRegistry _hotPath;

    public Perf1006ClockOnHotPathRule(HotPathRegistry hotPath)
    {
        _hotPath = hotPath;
    }

    public string Id => "PERF1006";

    public DiagnosticDescriptor Descriptor => RuleDescriptor;

    public void Analyze(OperationContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation reference)
        {
            return;
        }

        var property = reference.Property;
        if (!ClockProperties.Contains(property.Name))
        {
            return;
        }

        var containingType = property.ContainingType?.ToDisplayString();
        if (containingType is null || !ClockTypes.Contains(containingType))
        {
            return;
        }

        if (_hotPath.IsCold(context.InnermostSymbol))
        {
            return;
        }

        if (!_hotPath.IsHot(context.InnermostSymbol) && !OperationFacts.IsInsideLoop(reference))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            RuleDescriptor,
            reference.Syntax.GetLocation(),
            containingType + "." + property.Name));
    }
}