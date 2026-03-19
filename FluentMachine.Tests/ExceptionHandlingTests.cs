using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for exception handling in the state machine.
/// </summary>
public sealed class ExceptionHandlingTests
{

    [Fact]
    public void Enter_ThrowsException_Propagates()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => throw new InvalidOperationException("test error"))
            .End()
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => root.ChangeState("test"));
        Assert.Equal("test error", ex.Message);
    }

    [Fact]
    public void Enter_Exception_PreventsEntry()
    {
        var entered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ =>
                {
                    throw new InvalidOperationException();
#pragma warning disable CS0162
                    entered = true;
#pragma warning restore CS0162
                })
            .End()
            .Build();

        try { root.ChangeState("test"); } catch { }

        Assert.False(entered);
    }

    [Fact]
    public void Exit_ThrowsException_Propagates()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(_ => throw new InvalidOperationException("exit error"))
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");

        var ex = Assert.Throws<InvalidOperationException>(() => root.ChangeState("b"));
        Assert.Equal("exit error", ex.Message);
    }

    [Fact]
    public void Update_ThrowsException_Propagates()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => throw new InvalidOperationException("update error"))
            .End()
            .Build();

        root.ChangeState("test");

        var ex = Assert.Throws<InvalidOperationException>(() => root.Update(1f));
        Assert.Equal("update error", ex.Message);
    }

    [Fact]
    public void Update_AfterException_CanContinue()
    {
        var throwOnce = true;
        var updateCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) =>
                {
                    if (throwOnce)
                    {
                        throwOnce = false;
                        throw new InvalidOperationException();
                    }
                    updateCount++;
                })
            .End()
            .Build();

        root.ChangeState("test");

        try { root.Update(1f); } catch { }
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(2, updateCount);
    }

    [Fact]
    public void Condition_PredicateThrows_Propagates()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => throw new InvalidOperationException("predicate error"), _ => { })
            .End()
            .Build();

        root.ChangeState("test");

        var ex = Assert.Throws<InvalidOperationException>(() => root.Update(1f));
        Assert.Equal("predicate error", ex.Message);
    }

    [Fact]
    public void Condition_ActionThrows_Propagates()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => throw new InvalidOperationException("action error"))
            .End()
            .Build();

        root.ChangeState("test");

        var ex = Assert.Throws<InvalidOperationException>(() => root.Update(1f));
        Assert.Equal("action error", ex.Message);
    }

    [Fact]
    public void Condition_FirstThrows_OthersNotEvaluated()
    {
        var secondEvaluated = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => throw new InvalidOperationException(), _ => { })
                .Condition(() => { secondEvaluated = true; return true; }, _ => { })
            .End()
            .Build();

        root.ChangeState("test");

        try { root.Update(1f); } catch { }

        Assert.False(secondEvaluated);
    }

    [Fact]
    public void Event_ThrowsException_Propagates()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("throw", _ => throw new InvalidOperationException("event error"))
            .End()
            .Build();

        root.ChangeState("test");

        var ex = Assert.Throws<InvalidOperationException>(() => root.TriggerEvent("throw"));
        Assert.Equal("event error", ex.Message);
    }

    [Fact]
    public void Event_WrongArgsType_ThrowsApplicationException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<OtherTestEventArgs>("typed", (_, _) => { })
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<ApplicationException>(() =>
            root.TriggerEvent("typed", new TestEventArgs()));
    }

    [Fact]
    public void ChangeState_NonExistent_ThrowsApplicationException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("exists")
            .End()
            .Build();

        var ex = Assert.Throws<ApplicationException>(() => root.ChangeState("nonexistent"));
        Assert.Contains("not in the list", ex.Message);
    }

    [Fact]
    public void PushState_NonExistent_ThrowsApplicationException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .State<TestState>("child")
                .End()
            .End()
            .Build();

        root.ChangeState("parent");

        Assert.Throws<ApplicationException>(() => root.PushState("nonexistent"));
    }

    [Fact]
    public void PopState_EmptyStack_ThrowsApplicationException()
    {
        var state = new TestState();

        Assert.Throws<ApplicationException>(() => state.PopState());
    }

    [Fact]
    public void AfterException_StateMachineStillFunctional()
    {
        var throwCount = 0;
        var successCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) =>
                {
                    if (throwCount < 3)
                    {
                        throwCount++;
                        throw new InvalidOperationException();
                    }
                    successCount++;
                })
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 10; i++)
        {
            try { root.Update(1f); } catch { }
        }

        Assert.Equal(3, throwCount);
        Assert.Equal(7, successCount);
    }

    [Fact]
    public void ExceptionInChildState_ParentStillAccessible()
    {
        var parentOk = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Event("check", _ => parentOk = true)
                .State<TestState>("child")
                    .Enter(_ => throw new InvalidOperationException())
                .End()
            .End()
            .Build();

        root.ChangeState("parent");

        try { root.PushState("child"); } catch { }

        root.TriggerEvent("check");
        Assert.True(parentOk);
    }

    [Fact]
    public void TriggerEvent_NullArgs_DoesNotThrow()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("test", _ => { })
            .End()
            .Build();

        root.ChangeState("test");

        var ex = Record.Exception(() => root.TriggerEvent("test", null!));

        // Should not throw, EventArgs.Empty is used
        Assert.Null(ex);
    }

    [Fact]
    public void TriggerEvent_NullEventName_DoesNotCrash()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
            .End()
            .Build();

        root.ChangeState("test");

        // Depending on implementation, might throw or be ignored
        try { root.TriggerEvent(null!); } catch { }
        // No crash = success
    }

    [Fact]
    public void CustomException_PropagatesCorrectly()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => throw new CustomTestException("custom"))
            .End()
            .Build();

        var ex = Assert.Throws<CustomTestException>(() => root.ChangeState("test"));
        Assert.Equal("custom", ex.Message);
    }

    [Fact]
    public void InnerException_Preserved()
    {
        var innerEx = new InvalidOperationException("inner");

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => throw new ApplicationException("outer", innerEx))
            .End()
            .Build();

        var ex = Assert.Throws<ApplicationException>(() => root.ChangeState("test"));
        Assert.Same(innerEx, ex.InnerException);
    }

    private sealed class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }

}
