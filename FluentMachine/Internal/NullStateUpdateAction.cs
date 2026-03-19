using System;

namespace FluentMachine.Internal;

internal sealed class NullStateUpdateAction : IStateUpdateAction
{
    public static readonly IStateUpdateAction Instance = new NullStateUpdateAction();
    public void Execute(AbstractState state, float deltaTime) { }
}

internal sealed class LegacyStateUpdateAction(Action<float> action) : IStateUpdateAction
{
    public void Execute(AbstractState state, float deltaTime) => action(deltaTime);
}
