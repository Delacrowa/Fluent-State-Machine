namespace FluentMachine.Internal;

internal interface IStateConditionAction
{
    void Execute(AbstractState state);
}
