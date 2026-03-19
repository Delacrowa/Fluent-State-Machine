namespace FluentMachine.Examples.Battle.Core;

public sealed class CombatResult(Combatant winner, Combatant looser)
{
    public Combatant Winner { get; } = winner;
    public Combatant Looser { get; } = looser;
}
