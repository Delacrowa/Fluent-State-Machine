using System;

namespace FluentMachine.Internal;

internal sealed class StateConditionEntry(Func<bool> predicate, IStateConditionAction action)
{
    public void Execute(AbstractState state)
    {
        if (predicate())
            action.Execute(state);
    }
}
