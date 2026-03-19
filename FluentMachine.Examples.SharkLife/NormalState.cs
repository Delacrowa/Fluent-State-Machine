namespace FluentMachine.Examples.SharkLife;

public sealed class NormalState : AbstractState
{
    public Shark Shark { get; set; } = null!;

    public void OnUpdate()
    {
        Console.WriteLine("Swimming around...");
        Shark.Hunger++;
    }
}
