namespace FluentMachine.Examples.Battle.Core;

public sealed class CombatLogger : ICombatLogger
{
    public void Log(string message) => Console.WriteLine(message);
}
