namespace FluentMachine.Examples.SharkLife;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("Shark state machine. Press Ctrl-C to exit.");

        var shark = new Shark();

        var rootState = new StateMachineBuilder()
            .State<NormalState>("Swimming")
                .Enter(state => state.Shark = shark)
                .Update((state, _) =>
                {
                    state.OnUpdate();
                    if (shark.Hunger > 5)
                        state.PushState("Hunting");
                })
                .State<HungryState>("Hunting")
                    .Enter(state => state.Shark = shark)
                    .Update((state, _) =>
                    {
                        state.OnUpdate();
                        if (shark.Hunger <= 5)
                            state.Parent.PopState();
                    })
                .End()
            .End()
            .Build();

        rootState.ChangeState("Swimming");

        while (true)
        {
            rootState.Update(1.0f);
            Thread.Sleep(1000);
        }
    }
}
