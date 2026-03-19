namespace FluentMachine.Examples.Battle.Core;

public sealed class CombatantProfile(string id, int health, int damage)
{
    public string Id { get; } = id;
    public int Health { get; private set; } = health;
    public int Damage { get; } = damage;

    public void ApplyDamage(int dmg) => Health = Math.Max(0, Health - dmg);

    public override string ToString() => $"{Id} Health: {Health}, Damage: {Damage}";
}
