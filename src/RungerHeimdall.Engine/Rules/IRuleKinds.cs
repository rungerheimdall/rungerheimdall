namespace RungerHeimdall.Engine.Rules;

public interface ISymbolRule : IRule
{
    void Analyze(SymbolContext context);
}

public interface IOperationRule : IRule
{
    void Analyze(OperationContext context);
}