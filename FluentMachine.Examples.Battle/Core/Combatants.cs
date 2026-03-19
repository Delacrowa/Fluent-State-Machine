namespace FluentMachine.Examples.Battle.Core;

public sealed class Combatants
{
    private readonly Random _random;
    private readonly ICombatLogger _logger;
    private Combatant _attacker;
    private Combatant _defender;

    public Combatants(Combatant attacker, Combatant defender, Random random, ICombatLogger logger)
    {
        _attacker = attacker;
        _defender = defender;
        _random = random;
        _logger = logger;
    }

    public bool IsAnyDead => _attacker.IsDead || _defender.IsDead;
    public bool IsReady => _attacker.IsReady && _defender.IsReady;

    public void Fight()
    {
        if (_attacker.IsReady)
            _attacker.Attack(_defender);
    }

    public CombatResult GetResult() => new(
        _attacker.IsDead ? _defender : _attacker,
        _attacker.IsDead ? _attacker : _defender);

    public void Prepare()
    {
        _attacker.PrepareAttack();
        _defender.PrepareDefense();
    }

    public void Reset()
    {
        _attacker.Reset();
        _defender.Reset();
    }

    public void Swap() => (_attacker, _defender) = (_defender, _attacker);

    public void Toss()
    {
        if (_random.Next(0, 2) != 0)
            Swap();

        _logger.Log($"Attacker - {_attacker}, Defender - {_defender}");
    }
}
