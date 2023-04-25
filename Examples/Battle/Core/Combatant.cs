using Combat.Brain;

namespace Combat.Core
{
    public sealed class Combatant
    {
        public bool IsDead => _profile.Health <= 0;
        public bool IsReady { get; private set; }
        private readonly CombatantProfile _profile;
        private readonly ICombatantBrain _input;
        private readonly ICombatLogger _logger;
        private Part _part = Part.Body;

        public Combatant(CombatantProfile profile, ICombatantBrain input, ICombatLogger logger)
        {
            _profile = profile;
            _logger = logger;
            _input = input;
            _input.SetReaction(OnDecision);
        }

        public void Attack(Combatant defender)
        {
            if (_part == defender._part)
            {
                _logger.Log($"{defender} blocked damage from {this}");
            }
            else
            {
                defender.TakeDamage(_profile);
                _logger.Log(defender.IsDead
                    ? $"{this} killed {defender}"
                    : $"{this} hits {defender}");
            }
        }

        public void PrepareAttack() =>
            _input.Decide();

        public void PrepareDefense() =>
            _input.Decide();

        public void Reset() =>
            IsReady = false;

        private void OnDecision(Part target)
        {
            _part = target;
            IsReady = true;
        }

        private void TakeDamage(CombatantProfile from) =>
            _profile.ApplyDamage(from.Damage);

        public override string ToString() =>
            $"{_profile.Id} ({_profile.Health})";
    }
}