namespace FluentMachine.Internal;

internal interface IStateUpdateAction
{
    void Execute(AbstractState state, float deltaTime);
}
