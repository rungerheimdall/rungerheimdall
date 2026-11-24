using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RungerHeimdall.Engine.Operations;

public static class OperationFacts
{
    public static bool IsInsideLoop(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IForEachLoopOperation
                || parent is IForLoopOperation
                || parent is IWhileLoopOperation)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetEnclosingLoop(IOperation operation, out ILoopOperation? loop)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is ILoopOperation candidate)
            {
                loop = candidate;
                return true;
            }
        }

        loop = null;
        return false;
    }
}