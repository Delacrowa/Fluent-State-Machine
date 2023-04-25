namespace RSG
{
    /// <summary>
    ///     Entry point for fluent API for constructing states.
    /// </summary>
    public sealed class StateMachineBuilder
    {
        /// <summary>
        ///     Root level state.
        /// </summary>
        private readonly State _root = new State();

        /// <summary>
        ///     Return the root state once everything has been set up.
        /// </summary>
        public IState Build() =>
            _root;

        /// <summary>
        ///     Create a new state of a specified type and add it as a child of the root state.
        /// </summary>
        /// <typeparam name="T">Type of the state to add</typeparam>
        /// <returns>Builder used to configure the new state</returns>
        public IStateBuilder<T, StateMachineBuilder> State<T>() where T : AbstractState, new() =>
            new StateBuilder<T, StateMachineBuilder>(this, _root);

        /// <summary>
        ///     Create a new state of a specified type with a specified name and add it as a
        ///     child of the root state.
        /// </summary>
        /// <typeparam name="T">Type of the state to add</typeparam>
        /// <param name="stateName">Name for the new state</param>
        /// <returns>Builder used to configure the new state</returns>
        public IStateBuilder<T, StateMachineBuilder> State<T>(string stateName) where T : AbstractState, new() =>
            new StateBuilder<T, StateMachineBuilder>(this, _root, stateName);

        /// <summary>
        ///     Create a new state with a specified name and add it as a
        ///     child of the root state.
        /// </summary>
        /// <param name="stateName">Name for the new state</param>
        /// <returns>Builder used to configure the new state</returns>
        public IStateBuilder<State, StateMachineBuilder> State(string stateName) =>
            new StateBuilder<State, StateMachineBuilder>(this, _root, stateName);
    }
}