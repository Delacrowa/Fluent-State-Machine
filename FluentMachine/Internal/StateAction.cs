using System;

namespace FluentMachine.Internal;

internal sealed class StateAction<T>(Action<T> action) : IStateAction where T : AbstractState
{
    public void Execute(AbstractState state) => action((T)state);
}
