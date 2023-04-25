using System;
using System.Linq;
using Combat.Core;

namespace Combat.Brain
{
    public sealed class RandomDecisionBrain : ICombatantBrain
    {
        private readonly Random _random;
        private Action<Part> _decisionMade = delegate { };

        public RandomDecisionBrain(Random random) =>
            _random = random;

        public void Decide() =>
            _decisionMade.Invoke(GetRandomPart());

        public void SetReaction(Action<Part> decisionMade) =>
            _decisionMade = decisionMade;

        private Part GetRandomPart() =>
            Enum.GetValues(typeof(Part))
                .Cast<Part>()
                .OrderBy(x => _random.Next())
                .FirstOrDefault();
    }
}