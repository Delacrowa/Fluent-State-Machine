using System;

namespace FluentMachine.Internal;

internal interface IStateEventAction
{
    void Execute(AbstractState state, EventArgs args);
}
