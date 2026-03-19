using System;

namespace FluentMachine;

/// <summary>
/// Data structure for associating a condition with an action (legacy API support).
/// </summary>
public sealed class StateCondition(Func<bool> predicate, Action action)
{
    public void Execute()
    {
        if (predicate())
            action();
    }
}
