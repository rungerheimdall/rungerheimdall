using System;
using System.Threading;
using Microsoft.CodeAnalysis;

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
        Compilation = compilation;
        ReportDiagnostic = reportDiagnostic;
        CancellationToken = cancellationToken;
    }

    public IOperation Operation { get; }

    public ISymbol? OwningSymbol { get; }

    public Compilation Compilation { get; }

    public Action<Diagnostic> ReportDiagnostic { get; }

    public CancellationToken CancellationToken { get; }
}