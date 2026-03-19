namespace FluentMachine;

/// <summary>
/// Default state with no extra functionality. Used for root of state hierarchy.
/// </summary>
public sealed class State : AbstractState
{
    public static readonly IState Empty = new State();
}
