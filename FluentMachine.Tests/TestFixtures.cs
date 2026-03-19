namespace FluentMachine.Tests;

internal sealed class TestState : AbstractState
{
}

internal sealed class TestEventArgs : EventArgs
{
    public string? TestString { get; set; }
}

internal sealed class SecondTestEventArgs : EventArgs
{
}

internal sealed class OtherTestEventArgs : EventArgs
{
    public string? OtherData { get; set; }
}
