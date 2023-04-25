namespace Combat.Core
{
    public sealed class CombatantProfile
    {
        public string Id { get; }
        public int Health { get; private set; }
        public int Damage { get; }

        public CombatantProfile(string id, int health, int damage)
        {
            Id = id;
            Health = health;
            Damage = damage;
        }

        public void ApplyDamage(int damage)
        {
            var result = Health - damage;
            Health = result >= 0 ? result : 0;
        }

        public override string ToString() =>
            $"{Id} {nameof(Health)}: {Health}, {nameof(Damage)}: {Damage}";
    }
}