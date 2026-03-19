using FluentMachine.Examples.Battle.Core;

namespace FluentMachine.Examples.Battle.Brain;

public sealed class RandomDecisionBrain(Random random) : ICombatantBrain
{
    private Action<Part> _decisionMade = _ => { };

    public void Decide() => _decisionMade(GetRandomPart());

    public void SetReaction(Action<Part> decisionMade) => _decisionMade = decisionMade;

    private Part GetRandomPart() =>
        Enum.GetValues<Part>().OrderBy(_ => random.Next()).First();
}
