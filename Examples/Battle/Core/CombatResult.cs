namespace Combat.Core
{
    public sealed class CombatResult
    {
        public readonly Combatant Winner;
        public readonly Combatant Looser;

        public CombatResult(Combatant winner, Combatant looser)
        {
            Winner = winner;
            Looser = looser;
        }
    }
}