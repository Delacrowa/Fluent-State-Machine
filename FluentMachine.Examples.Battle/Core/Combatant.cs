using FluentMachine.Examples.Battle.Brain;

namespace FluentMachine.Examples.Battle.Core;

public sealed class Combatant
{
    private readonly CombatantProfile _profile;
    private readonly ICombatantBrain _input;
    private readonly ICombatLogger _logger;
    private Part _part = Part.Body;

    public bool IsDead => _profile.Health <= 0;
    public bool IsReady { get; private set; }

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
            _logger.Log(defender.IsDead ? $"{this} killed {defender}" : $"{this} hits {defender}");
        }
    }

    public void PrepareAttack() => _input.Decide();

    public void PrepareDefense() => _input.Decide();

    public void Reset() => IsReady = false;

    public override string ToString() => $"{_profile.Id} ({_profile.Health})";

    private void OnDecision(Part target)
    {
        _part = target;
        IsReady = true;
    }

    private void TakeDamage(CombatantProfile from) => _profile.ApplyDamage(from.Damage);
}
