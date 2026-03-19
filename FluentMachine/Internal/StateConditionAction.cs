using System;

namespace FluentMachine.Internal;

internal sealed class StateConditionAction<T>(Action<T> action) : IStateConditionAction where T : AbstractState
{
    public void Execute(AbstractState state) => action((T)state);
}
