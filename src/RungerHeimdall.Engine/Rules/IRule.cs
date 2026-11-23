using Microsoft.CodeAnalysis;

namespace RungerHeimdall.Engine.Rules;

public interface IRule
{
    string Id { get; }

    DiagnosticDescriptor Descriptor { get; }
}