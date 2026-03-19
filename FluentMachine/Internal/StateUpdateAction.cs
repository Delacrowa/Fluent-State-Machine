using System;

namespace FluentMachine.Internal;

internal sealed class StateUpdateAction<T>(Action<T, float> action) : IStateUpdateAction where T : AbstractState
{
    public void Execute(AbstractState state, float deltaTime) => action((T)state, deltaTime);
}
