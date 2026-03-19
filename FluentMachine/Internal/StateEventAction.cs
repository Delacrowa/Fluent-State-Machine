using System;

namespace FluentMachine.Internal;

internal sealed class StateEventAction<T>(Action<T> action) : IStateEventAction where T : AbstractState
{
    public void Execute(AbstractState state, EventArgs args) => action((T)state);
}

internal sealed class StateEventAction<T, TEvent>(string identifier, Action<T, TEvent> action) : IStateEventAction
    where T : AbstractState
    where TEvent : EventArgs
{
    public void Execute(AbstractState state, EventArgs args)
    {
        if (args is TEvent typedArgs)
        {
            action((T)state, typedArgs);
        }
        else if (args == EventArgs.Empty && typeof(TEvent) == typeof(EventArgs))
        {
            action((T)state, (TEvent)args);
        }
        else
        {
            throw new ApplicationException(
                $"Could not invoke event \"{identifier}\" with argument of type {args.GetType().Name}. Expected {typeof(TEvent).Name}");
        }
    }
}
