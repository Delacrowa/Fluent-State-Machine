using System;

namespace FluentMachine;

/// <summary>
/// Non-generic state interface.
/// </summary>
public interface IState
{
    /// <summary>
    /// Parent state, or State.Empty if this is the root level state.
    /// </summary>
    IState Parent { get; set; }

    /// <summary>
    /// Change to the state with the specified name.
    /// </summary>
    void ChangeState(string stateName);

    /// <summary>
    /// Triggered when we enter the state.
    /// </summary>
    void Enter();

    /// <summary>
    /// Triggered when we exit the state.
    /// </summary>
    void Exit();

    /// <summary>
    /// Exit out of the current state and enter whatever state is below it in the stack.
    /// </summary>
    void PopState();

    /// <summary>
    /// Push another state above the current one, so that popping it will return to the current state.
    /// </summary>
    void PushState(string stateName);

    /// <summary>
    /// Trigger an event on this state or one of its children.
    /// </summary>
    void TriggerEvent(string name);

    /// <summary>
    /// Trigger an event with arguments on this state or one of its children.
    /// </summary>
    void TriggerEvent(string name, EventArgs eventArgs);

    /// <summary>
    /// Update this state and its children with a specified delta time.
    /// </summary>
    void Update(float deltaTime);
}
