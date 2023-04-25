using System;
using System.Threading;
using Combat.Brain;
using Combat.Core;

namespace Combat
{
    public static class Program
    {
        private static readonly ICombatLogger _logger = new CombatLogger();
        private static Core.Combat _combat;

        public static void Main(string[] args)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var first = BuildCombatant("Bambino", random);
            var second = BuildCombatant("Gringo", random);
            var combatants = new Combatants(first, second, random, _logger);

            const double timeout = 1_000;
            _combat = new Core.Combat(combatants, timeout);
            _combat.Start();

            while (!_combat.IsFinished)
            {
                _combat.Update((float) timeout / 1000);
                Thread.Sleep((int) timeout);
            }

            _logger.Log($"Winner: {_combat.Result.Winner}, Looser: {_combat.Result.Looser}");
            Console.ReadKey();
        }

        private static Combatant BuildCombatant(string name, Random random)
        {
            const int health = 30;
            const int damage = 10;
            return new Combatant(BuildProfile(name, health, damage), new RandomDecisionBrain(random), _logger);
        }

        private static CombatantProfile BuildProfile(string name, int health, int damage) =>
            new CombatantProfile(name, health, damage);
    }
}