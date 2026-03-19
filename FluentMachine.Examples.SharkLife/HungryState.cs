namespace FluentMachine.Examples.SharkLife;

public sealed class HungryState : AbstractState
{
    private readonly Random _random = new();

    public Shark Shark { get; set; } = null!;

    public void OnUpdate()
    {
        if (_random.Next(5) <= 1)
        {
            Console.WriteLine("Feeding");
            Shark.Hunger -= 5;
        }
        else
        {
            Console.WriteLine("Hunting");
        }
        Shark.Hunger++;
    }
}
