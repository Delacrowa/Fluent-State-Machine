using System.Timers;
using RSG;

namespace Combat.Core
{
    public sealed class Combat
    {
        public bool IsFinished { get; private set; }
        public CombatResult Result { get; private set; }
        private const string CombatStart = nameof(CombatStart);
        private const string CombatCheckStatus = nameof(CombatCheckStatus);
        private const string CombatEnd = nameof(CombatEnd);
        private const string CombatContinue = nameof(CombatContinue);
        private const string TurnStart = nameof(TurnStart);
        private const string TurnEnd = nameof(TurnEnd);
        private readonly Combatants _combatants;
        private readonly Timer _timer;
        private readonly IState _machine;
        private bool _timeout;

        public Combat(Combatants combatants, double timeout)
        {
            _combatants = combatants;
            _timer = new Timer(timeout);
            _timer.Elapsed += OnTimeout;
            _machine = new StateMachineBuilder()
                .State(CombatStart)
                .Enter(s =>
                {
                    _combatants.Toss();
                    s.Parent.ChangeState(TurnStart);
                })
                .End()
                .State(TurnStart)
                .Enter(s =>
                {
                    _combatants.Prepare();
                    _timeout = false;
                    _timer.Start();
                })
                .Update((s, _) =>
                {
                    if (_combatants.IsReady)
                    {
                        s.Parent.ChangeState(TurnEnd);
                    }
                    else if (_timeout)
                    {
                        s.Parent.ChangeState(TurnEnd);
                    }
                })
                .End()
                .State(TurnEnd)
                .Enter(s =>
                {
                    _combatants.Fight();
                    _combatants.Reset();
                    s.Parent.ChangeState(CombatCheckStatus);
                })
                .End()
                .State(CombatCheckStatus)
                .Enter(s =>
                {
                    s.Parent.ChangeState(_combatants.IsAnyDead
                        ? CombatEnd
                        : CombatContinue);
                })
                .End()
                .State(CombatContinue)
                .Enter(s =>
                {
                    _combatants.Swap();
                    s.Parent.ChangeState(TurnStart);
                })
                .End()
                .State(CombatEnd)
                .Enter(s =>
                {
                    Result = _combatants.GetResult();
                    IsFinished = true;
                })
                .End()
                .Build();
        }

        public void Start() =>
            _machine.ChangeState(CombatStart);

        public void Update(float timeDelta) =>
            _machine.Update(timeDelta);

        private void OnTimeout(object sender, ElapsedEventArgs e)
        {
            _timeout = true;
            _timer.Stop();
        }
    }
}