using System;

namespace FluentMachine;

/// <summary>
/// Builder providing a fluent API for constructing states.
/// </summary>
public sealed class StateBuilder<T, TParent> : IStateBuilder<T, TParent> where T : AbstractState, new()
{
    private readonly TParent _parentBuilder;
    private readonly T _state;

    public StateBuilder(TParent parentBuilder, AbstractState parentState)
    {
        _parentBuilder = parentBuilder;
        _state = new T();
        parentState.AddChild(_state);
    }

    public StateBuilder(TParent parentBuilder, AbstractState parentState, string name)
    {
        _parentBuilder = parentBuilder;
        _state = new T();
        parentState.AddChild(_state, name);
    }

    public IStateBuilder<T, TParent> Condition(Func<bool> predicate, Action<T> action)
    {
        _state.SetTypedCondition(predicate, action);
        return this;
    }

    public TParent End() => _parentBuilder;

    public IStateBuilder<T, TParent> Enter(Action<T> onEnter)
    {
        _state.SetTypedEnterAction(onEnter);
        return this;
    }

    public IStateBuilder<T, TParent> Event(string identifier, Action<T> action)
    {
        _state.SetTypedEvent(identifier, action);
        return this;
    }

    public IStateBuilder<T, TParent> Event<TEvent>(string identifier, Action<T, TEvent> action) where TEvent : EventArgs
    {
        _state.SetTypedEvent(identifier, action);
        return this;
    }

    public IStateBuilder<T, TParent> Exit(Action<T> onExit)
    {
        _state.SetTypedExitAction(onExit);
        return this;
    }

    public IStateBuilder<NewStateT, IStateBuilder<T, TParent>> State<NewStateT>() where NewStateT : AbstractState, new() =>
        new StateBuilder<NewStateT, IStateBuilder<T, TParent>>(this, _state);

    public IStateBuilder<NewStateT, IStateBuilder<T, TParent>> State<NewStateT>(string name) where NewStateT : AbstractState, new() =>
        new StateBuilder<NewStateT, IStateBuilder<T, TParent>>(this, _state, name);

    public IStateBuilder<State, IStateBuilder<T, TParent>> State(string name) =>
        new StateBuilder<State, IStateBuilder<T, TParent>>(this, _state, name);

    public IStateBuilder<T, TParent> Update(Action<T, float> onUpdate)
    {
        _state.SetTypedUpdateAction(onUpdate);
        return this;
    }
}
