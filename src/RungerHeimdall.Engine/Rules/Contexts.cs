using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RungerHeimdall.Engine.Rules;

public readonly struct SymbolContext
{
    public SymbolContext(
        ISymbol symbol,
        Compilation compilation,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        Symbol = symbol;
        Compilation = compilation;
        ReportDiagnostic = reportDiagnostic;
        CancellationToken = cancellationToken;
    }

    public ISymbol Symbol { get; }

    public Compilation Compilation { get; }

    public Action<Diagnostic> ReportDiagnostic { get; }

    public CancellationToken CancellationToken { get; }
}

public readonly struct OperationContext
{
    public OperationContext(
        IOperation operation,
        ISymbol? owningSymbol,
        Compilation compilation,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        Operation = operation;
        OwningSymbol = owningSymbol;
        InnermostSymbol = FindInnermostSymbol(operation, owningSymbol);
        Compilation = compilation;
        ReportDiagnostic = reportDiagnostic;
        CancellationToken = cancellationToken;
    }

    public IOperation Operation { get; }

    public ISymbol? OwningSymbol { get; }

    public ISymbol? InnermostSymbol { get; }

    public Compilation Compilation { get; }

    public Action<Diagnostic> ReportDiagnostic { get; }

    public CancellationToken CancellationToken { get; }

    private static ISymbol? FindInnermostSymbol(IOperation operation, ISymbol? fallback)
    {
        for (var current = operation; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case IAnonymousFunctionOperation anonymous:
                    return anonymous.Symbol;

                case ILocalFunctionOperation local:
                    return local.Symbol;
            }
        }

        return fallback;
    }
}