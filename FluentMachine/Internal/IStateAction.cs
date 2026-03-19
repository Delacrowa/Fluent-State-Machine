namespace FluentMachine.Internal;

internal interface IStateAction
{
    void Execute(AbstractState state);
}
