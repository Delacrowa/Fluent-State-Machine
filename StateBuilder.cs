using System;

namespace RSG
{
    /// <summary>
    ///     Builder providing a fluent API for constructing states.
    /// </summary>
    public sealed class StateBuilder<T, TParent> : IStateBuilder<T, TParent>
        where T : AbstractState, new()
    {
        /// <summary>
        ///     Class to return when we call .End()
        /// </summary>
        private readonly TParent _parentBuilder;
        /// <summary>
        ///     The current state we're building.
        /// </summary>
        private readonly T _state;

        /// <summary>
        ///     Create a new state builder with a specified parent state and parent builder.
        /// </summary>
        /// <param name="parentBuilder">
        ///     The parent builder, or what we will return
        ///     when .End is called.
        /// </param>
        /// <param name="parentState">The parent of the new state to create.</param>
        public StateBuilder(TParent parentBuilder, AbstractState parentState)
        {
            _parentBuilder = parentBuilder;
            _state = new T();
            parentState.AddChild(_state);
        }

        /// <summary>
        ///     Create a new state builder with a specified parent state, parent builder,
        ///     and name for the new state.
        /// </summary>
        /// <param name="parentBuilder">
        ///     The parent builder, or what we will return
        ///     when .End is called.
        /// </param>
        /// <param name="parentState">The parent of the new state to create.</param>
        /// <param name="name">Name of the state to add.</param>
        public StateBuilder(TParent parentBuilder, AbstractState parentState, string name)
        {
            _parentBuilder = parentBuilder;
            _state = new T();
            parentState.AddChild(_state, name);
        }

        /// <summary>
        ///     Set an action to be called on update when a condition is true.
        /// </summary>
        public IStateBuilder<T, TParent> Condition(Func<bool> predicate, Action<T> action)
        {
            _state.SetCondition(predicate, () => action(_state));
            return this;
        }

        /// <summary>
        ///     Finalise the current state and return the builder for its parent.
        /// </summary>
        public TParent End() =>
            _parentBuilder;

        /// <summary>
        ///     Set an action to be called when we enter the state.
        /// </summary>
        public IStateBuilder<T, TParent> Enter(Action<T> onEnter)
        {
            _state.SetEnterAction(() => onEnter(_state));
            return this;
        }

        /// <summary>
        ///     Set an action to be triggerable when an event with the specified name is raised.
        /// </summary>
        public IStateBuilder<T, TParent> Event(string identifier, Action<T> action)
        {
            _state.SetEvent<EventArgs>(identifier, _ => action(_state));
            return this;
        }

        /// <summary>
        ///     Set an action with arguments to be triggerable when an event with the specified name is raised.
        /// </summary>
        public IStateBuilder<T, TParent> Event<TEvent>(string identifier, Action<T, TEvent> action)
            where TEvent : EventArgs
        {
            _state.SetEvent<TEvent>(identifier, args => action(_state, args));
            return this;
        }

        /// <summary>
        ///     Set an action to be called when we exit the state.
        /// </summary>
        public IStateBuilder<T, TParent> Exit(Action<T> onExit)
        {
            _state.SetExitAction(() => onExit(_state));
            return this;
        }

        /// <summary>
        ///     Create a child state with a specified handler type. The state will take the
        ///     name of the handler type.
        /// </summary>
        /// <typeparam name="NewStateT">Handler type for the new state</typeparam>
        /// <returns>A new state builder object for the new child state</returns>
        public IStateBuilder<NewStateT, IStateBuilder<T, TParent>> State<NewStateT>()
            where NewStateT : AbstractState, new() =>
            new StateBuilder<NewStateT, IStateBuilder<T, TParent>>(this, _state);

        /// <summary>
        ///     Create a named child state with a specified handler type.
        /// </summary>
        /// <typeparam name="NewStateT">Handler type for the new state</typeparam>
        /// <param name="name">String for identifying state in parent</param>
        /// <returns>A new state builder object for the new child state</returns>
        public IStateBuilder<NewStateT, IStateBuilder<T, TParent>> State<NewStateT>(string name)
            where NewStateT : AbstractState, new() =>
            new StateBuilder<NewStateT, IStateBuilder<T, TParent>>(this, _state, name);

        /// <summary>
        ///     Create a child state with the default handler type.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A state builder object for the new child state</returns>
        public IStateBuilder<State, IStateBuilder<T, TParent>> State(string name) =>
            new StateBuilder<State, IStateBuilder<T, TParent>>(this, _state, name);

        /// <summary>
        ///     Set an action to be called when we update the state.
        /// </summary>
        public IStateBuilder<T, TParent> Update(Action<T, float> onUpdate)
        {
            _state.SetUpdateAction(dt => onUpdate(_state, dt));
            return this;
        }
    }
}