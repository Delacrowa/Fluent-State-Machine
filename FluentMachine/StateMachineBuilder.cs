namespace FluentMachine;

/// <summary>
/// Entry point for fluent API for constructing state machines.
/// </summary>
public sealed class StateMachineBuilder
{
    private readonly State _root = new();

    /// <summary>
    /// Return the root state once everything has been set up.
    /// </summary>
    public IState Build() => _root;

    /// <summary>
    /// Create a new state of a specified type and add it as a child of the root state.
    /// </summary>
    public IStateBuilder<T, StateMachineBuilder> State<T>() where T : AbstractState, new() =>
        new StateBuilder<T, StateMachineBuilder>(this, _root);

    /// <summary>
    /// Create a new state of a specified type with a specified name and add it as a child of the root state.
    /// </summary>
    public IStateBuilder<T, StateMachineBuilder> State<T>(string stateName) where T : AbstractState, new() =>
        new StateBuilder<T, StateMachineBuilder>(this, _root, stateName);

    /// <summary>
    /// Create a new state with a specified name and add it as a child of the root state.
    /// </summary>
    public IStateBuilder<State, StateMachineBuilder> State(string stateName) =>
        new StateBuilder<State, StateMachineBuilder>(this, _root, stateName);
}
