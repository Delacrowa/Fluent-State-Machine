using System;
using System.Collections.Generic;

namespace RSG
{
    /// <summary>
    ///     State with a specified handler type.
    /// </summary>
    public abstract class AbstractState : IState
    {
        /// <summary>
        ///     Parent state, or null if this is the root level state.
        /// </summary>
        public IState Parent { get; set; }
        /// <summary>
        ///     Stack of active child states.
        /// </summary>
        private readonly Stack<IState> _activeChildren = new Stack<IState>();
        /// <summary>
        ///     Dictionary of all children (active and inactive), and their names.
        /// </summary>
        private readonly IDictionary<string, IState> _children = new Dictionary<string, IState>();
        private readonly IList<Condition> _conditions = new List<Condition>();
        /// <summary>
        ///     Dictionary of all actions associated with this state.
        /// </summary>
        private readonly IDictionary<string, Action<EventArgs>> _events = new Dictionary<string, Action<EventArgs>>();
        /// <summary>
        ///     Action called when we enter the state.
        /// </summary>
        private Action _onEnter;
        /// <summary>
        ///     Action called when we exit the state.
        /// </summary>
        private Action _onExit;
        /// <summary>
        ///     Action called when the state gets updated.
        /// </summary>
        private Action<float> _onUpdate;

        /// <summary>
        ///     Cast the specified EventArgs to a specified type, throwing a descriptive exception if this fails.
        /// </summary>
        private static TEvent CheckEventArgs<TEvent>(string identifier, EventArgs args)
            where TEvent : EventArgs
        {
            try
            {
                return (TEvent) args;
            }
            catch (InvalidCastException ex)
            {
                throw new ApplicationException("Could not invoke event \"" + identifier + "\" with argument of type " +
                                               args.GetType().Name + ". Expected " + typeof(TEvent).Name, ex);
            }
        }

        /// <summary>
        ///     Create a new state as a child of the current state.
        /// </summary>
        public void AddChild(IState newState, string stateName)
        {
            try
            {
                _children.Add(stateName, newState);
                newState.Parent = this;
            }
            catch (ArgumentException)
            {
                throw new ApplicationException("State with name \"" + stateName +
                                               "\" already exists in list of children.");
            }
        }

        /// <summary>
        ///     Create a new state as a child of the current state and automatically derive
        ///     its name from its handler type.
        /// </summary>
        public void AddChild(IState newState)
        {
            var name = newState.GetType().Name;
            AddChild(newState, name);
        }

        /// <summary>
        ///     Pops the current state from the stack and pushes the specified one on.
        /// </summary>
        public void ChangeState(string stateName)
        {
            if (!_children.TryGetValue(stateName, out var newState))
            {
                throw new ApplicationException("Tried to change to state \"" + stateName +
                                               "\", but it is not in the list of children.");
            }
            if (_activeChildren.Count > 0)
            {
                _activeChildren.Pop().Exit();
            }

            // Activate the new state
            _activeChildren.Push(newState);
            newState.Enter();
        }

        /// <summary>
        ///     Triggered when we enter the state.
        /// </summary>
        public void Enter() =>
            _onEnter?.Invoke();

        /// <summary>
        ///     Triggered when we exit the state.
        /// </summary>
        public void Exit()
        {
            _onExit?.Invoke();

            while (_activeChildren.Count > 0)
            {
                _activeChildren.Pop().Exit();
            }
        }

        /// <summary>
        ///     Remove the current state from the active state stack and activate the state immediately beneath it.
        /// </summary>
        public void PopState()
        {
            if (_activeChildren.Count > 0)
            {
                _activeChildren.Pop().Exit();
            }
            else
            {
                throw new ApplicationException("PopState called on state with no active children to pop.");
            }
        }

        /// <summary>
        ///     Push another state from the existing dictionary of children to the top of the state stack.
        /// </summary>
        public void PushState(string stateName)
        {
            // Find the new state and add it
            if (!_children.TryGetValue(stateName, out var newState))
            {
                throw new ApplicationException("Tried to change to state \"" + stateName +
                                               "\", but it is not in the list of children.");
            }
            _activeChildren.Push(newState);
            newState.Enter();
        }

        /// <summary>
        ///     Set an action to be called when the state is updated an a specified
        ///     predicate is true.
        /// </summary>
        public void SetCondition(Func<bool> predicate, Action action)
        {
            _conditions.Add(new Condition
            {
                Predicate = predicate,
                Action = action
            });
        }

        /// <summary>
        ///     Action triggered on entering the state.
        /// </summary>
        public void SetEnterAction(Action onEnter) =>
            _onEnter = onEnter;

        /// <summary>
        ///     Sets an action to be associated with an identifier that can later be used
        ///     to trigger it.
        ///     Convenience method that uses default event args intended for events that
        ///     don't need any arguments.
        /// </summary>
        public void SetEvent(string identifier, Action<EventArgs> eventTriggeredAction) =>
            SetEvent<EventArgs>(identifier, eventTriggeredAction);

        /// <summary>
        ///     Sets an action to be associated with an identifier that can later be used
        ///     to trigger it.
        /// </summary>
        public void SetEvent<TEvent>(string identifier, Action<TEvent> eventTriggeredAction)
            where TEvent : EventArgs =>
            _events.Add(identifier, args => eventTriggeredAction(CheckEventArgs<TEvent>(identifier, args)));

        /// <summary>
        ///     Action triggered on exiting the state.
        /// </summary>
        public void SetExitAction(Action onExit) =>
            _onExit = onExit;

        /// <summary>
        ///     Action which passes the current state object and the delta time since the
        ///     last update to a function.
        /// </summary>
        public void SetUpdateAction(Action<float> onUpdate) =>
            _onUpdate = onUpdate;

        /// <summary>
        ///     Triggered when and event occurs. Executes the event's action if the
        ///     current state is at the top of the stack, otherwise triggers it on
        ///     the next state down.
        /// </summary>
        /// <param name="name">Name of the event to trigger</param>
        public void TriggerEvent(string name) =>
            TriggerEvent(name, EventArgs.Empty);

        /// <summary>
        ///     Triggered when and event occurs. Executes the event's action if the
        ///     current state is at the top of the stack, otherwise triggers it on
        ///     the next state down.
        /// </summary>
        /// <param name="name">Name of the event to trigger</param>
        /// <param name="eventArgs">Arguments to send to the event</param>
        public void TriggerEvent(string name, EventArgs eventArgs)
        {
            // Only update the child at the end of the tree
            if (_activeChildren.Count > 0)
            {
                _activeChildren.Peek().TriggerEvent(name, eventArgs);
                return;
            }
            if (_events.TryGetValue(name, out var myEvent))
            {
                myEvent(eventArgs);
            }
        }

        /// <summary>
        ///     Update this state and its children with a specified delta time.
        /// </summary>
        public void Update(float deltaTime)
        {
            // Only update the child at the end of the tree
            if (_activeChildren.Count > 0)
            {
                _activeChildren.Peek().Update(deltaTime);
                return;
            }

            _onUpdate?.Invoke(deltaTime);

            // Update conditions
            for (var i = 0; i < _conditions.Count; i++)
            {
                if (_conditions[i].Predicate())
                {
                    _conditions[i].Action();
                }
            }
        }

        /// <summary>
        ///     Data structure for associating a condition with an action.
        /// </summary>
        private struct Condition
        {
            public Func<bool> Predicate;
            public Action Action;
        }
    }
}