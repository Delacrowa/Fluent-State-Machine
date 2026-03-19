using System;
using System.Collections.Generic;
using FluentMachine.Internal;

namespace FluentMachine;

/// <summary>
/// Base class for all states in the state machine hierarchy.
/// </summary>
public abstract class AbstractState : IState
{
    public IState Parent { get; set; } = State.Empty;

    private readonly Stack<IState> _activeChildren = new();
    private readonly Dictionary<string, IState> _children = new();
    private readonly List<StateConditionEntry> _typedConditions = new();
    private readonly Dictionary<string, IStateEventAction> _typedEvents = new();
    private readonly List<StateCondition> _conditions = new();

    private IStateAction _enterAction = NullStateAction.Instance;
    private IStateAction _exitAction = NullStateAction.Instance;
    private IStateUpdateAction _updateAction = NullStateUpdateAction.Instance;

    public void AddChild(IState newState, string stateName)
    {
        if (!_children.TryAdd(stateName, newState))
            throw new ApplicationException($"State with name \"{stateName}\" already exists in list of children.");
        newState.Parent = this;
    }

    public void AddChild(IState newState) => AddChild(newState, newState.GetType().Name);

    public void ChangeState(string stateName)
    {
        if (!_children.TryGetValue(stateName, out var newState))
            throw new ApplicationException($"Tried to change to state \"{stateName}\", but it is not in the list of children.");

        if (_activeChildren.Count > 0)
            _activeChildren.Pop().Exit();

        _activeChildren.Push(newState);
        newState.Enter();
    }

    public void Enter() => _enterAction.Execute(this);

    public void Exit()
    {
        _exitAction.Execute(this);

        while (_activeChildren.Count > 0)
            _activeChildren.Pop().Exit();
    }

    public void PopState()
    {
        if (_activeChildren.Count > 0)
            _activeChildren.Pop().Exit();
        else
            throw new ApplicationException("PopState called on state with no active children to pop.");
    }

    public void PushState(string stateName)
    {
        if (!_children.TryGetValue(stateName, out var newState))
            throw new ApplicationException($"Tried to change to state \"{stateName}\", but it is not in the list of children.");

        _activeChildren.Push(newState);
        newState.Enter();
    }

    public void SetCondition(Func<bool> predicate, Action action) =>
        _conditions.Add(new StateCondition(predicate, action));

    public void SetEnterAction(Action onEnter) => _enterAction = new LegacyStateAction(onEnter);

    public void SetEvent(string identifier, Action<EventArgs> eventTriggeredAction) =>
        SetEvent<EventArgs>(identifier, eventTriggeredAction);

    public void SetEvent<TEvent>(string identifier, Action<TEvent> eventTriggeredAction) where TEvent : EventArgs =>
        _typedEvents.Add(identifier, new LegacyEventAction<TEvent>(identifier, eventTriggeredAction));

    public void SetExitAction(Action onExit) => _exitAction = new LegacyStateAction(onExit);

    public void SetUpdateAction(Action<float> onUpdate) => _updateAction = new LegacyStateUpdateAction(onUpdate);

    public void TriggerEvent(string name) => TriggerEvent(name, EventArgs.Empty);

    public void TriggerEvent(string name, EventArgs eventArgs)
    {
        if (_activeChildren.Count > 0)
        {
            _activeChildren.Peek().TriggerEvent(name, eventArgs);
            return;
        }

        if (_typedEvents.TryGetValue(name, out var action))
            action.Execute(this, eventArgs);
    }

    public void Update(float deltaTime)
    {
        if (_activeChildren.Count > 0)
        {
            _activeChildren.Peek().Update(deltaTime);
            return;
        }

        _updateAction.Execute(this, deltaTime);

        for (var i = 0; i < _typedConditions.Count; i++)
            _typedConditions[i].Execute(this);

        for (var i = 0; i < _conditions.Count; i++)
            _conditions[i].Execute();
    }

    internal void SetTypedEnterAction<T>(Action<T> onEnter) where T : AbstractState =>
        _enterAction = new StateAction<T>(onEnter);

    internal void SetTypedExitAction<T>(Action<T> onExit) where T : AbstractState =>
        _exitAction = new StateAction<T>(onExit);

    internal void SetTypedUpdateAction<T>(Action<T, float> onUpdate) where T : AbstractState =>
        _updateAction = new StateUpdateAction<T>(onUpdate);

    internal void SetTypedCondition<T>(Func<bool> predicate, Action<T> action) where T : AbstractState =>
        _typedConditions.Add(new StateConditionEntry(predicate, new StateConditionAction<T>(action)));

    internal void SetTypedEvent<T>(string identifier, Action<T> action) where T : AbstractState =>
        _typedEvents.Add(identifier, new StateEventAction<T>(action));

    internal void SetTypedEvent<T, TEvent>(string identifier, Action<T, TEvent> action)
        where T : AbstractState
        where TEvent : EventArgs =>
        _typedEvents.Add(identifier, new StateEventAction<T, TEvent>(identifier, action));

    private sealed class LegacyEventAction<TEvent>(string identifier, Action<TEvent> action) : IStateEventAction
        where TEvent : EventArgs
    {
        public void Execute(AbstractState state, EventArgs args)
        {
            if (args is TEvent typedArgs)
            {
                action(typedArgs);
            }
            else if (args == EventArgs.Empty && typeof(TEvent) == typeof(EventArgs))
            {
                action((TEvent)args);
            }
            else
            {
                throw new ApplicationException(
                    $"Could not invoke event \"{identifier}\" with argument of type {args.GetType().Name}. Expected {typeof(TEvent).Name}");
            }
        }
    }
}
