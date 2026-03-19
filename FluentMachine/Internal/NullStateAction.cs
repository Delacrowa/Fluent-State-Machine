using System;

namespace FluentMachine.Internal;

internal sealed class NullStateAction : IStateAction
{
    public static readonly IStateAction Instance = new NullStateAction();
    public void Execute(AbstractState state) { }
}

internal sealed class LegacyStateAction(Action action) : IStateAction
{
    public void Execute(AbstractState state) => action();
}
