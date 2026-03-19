using FluentMachine.Examples.Battle.Brain;
using FluentMachine.Examples.Battle.Core;

namespace FluentMachine.Examples.Battle;

public static class Program
{
    private static readonly ICombatLogger Logger = new CombatLogger();

    public static void Main()
    {
        var random = new Random(DateTime.Now.Millisecond);
        var first = BuildCombatant("Bambino", random);
        var second = BuildCombatant("Gringo", random);
        var combatants = new Combatants(first, second, random, Logger);

        const double timeout = 1_000;
        var combat = new Combat(combatants, timeout);
        combat.Start();

        while (!combat.IsFinished)
        {
            combat.Update((float)timeout / 1000);
            Thread.Sleep((int)timeout);
        }

        Logger.Log($"Winner: {combat.Result.Winner}, Looser: {combat.Result.Looser}");
        Console.ReadKey();
    }

    private static Combatant BuildCombatant(string name, Random random) =>
        new(new CombatantProfile(name, 30, 10), new RandomDecisionBrain(random), Logger);
}
