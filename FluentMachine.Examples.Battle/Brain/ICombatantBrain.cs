using FluentMachine.Examples.Battle.Core;

namespace FluentMachine.Examples.Battle.Brain;

public interface ICombatantBrain
{
    void Decide();
    void SetReaction(Action<Part> decisionMade);
}
