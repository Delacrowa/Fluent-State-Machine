using Xunit;

namespace FluentMachine.Tests;

public sealed class EdgeCaseTests
{

    [Fact]
    public void DeepNesting_TenLevels_WorksCorrectly()
    {
        var deepestEntered = false;

        var root = new StateMachineBuilder()
            .State("l1")
                .Enter(s => s.ChangeState("l2"))
                .State("l2")
                    .Enter(s => s.ChangeState("l3"))
                    .State("l3")
                        .Enter(s => s.ChangeState("l4"))
                        .State("l4")
                            .Enter(s => s.ChangeState("l5"))
                            .State("l5")
                                .Enter(_ => deepestEntered = true)
                            .End()
                        .End()
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("l1");

        Assert.True(deepestEntered);
    }

    [Fact]
    public void RapidStateChanges_NoExceptions()
    {
        var root = new StateMachineBuilder()
            .State("a").End()
            .State("b").End()
            .State("c").End()
            .Build();

        var ex = Record.Exception(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                root.ChangeState("a");
                root.ChangeState("b");
                root.ChangeState("c");
            }
        });

        Assert.Null(ex);
    }

    [Fact]
    public void StateTransition_DuringEnter_WorksCorrectly()
    {
        var bEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(s => s.Parent.ChangeState("b"))
            .End()
            .State<TestState>("b")
                .Enter(_ => bEntered = true)
            .End()
            .Build();

        root.ChangeState("a");

        Assert.True(bEntered);
    }

    [Fact]
    public void PushState_DuringEnter_WorksCorrectly()
    {
        var childEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .State<TestState>("child")
                    .Enter(_ => childEntered = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");

        Assert.True(childEntered);
    }

    [Fact]
    public void StateTransition_DuringUpdate_WorksCorrectly()
    {
        var bUpdated = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Update((s, _) => s.Parent.ChangeState("b"))
            .End()
            .State<TestState>("b")
                .Update((_, _) => bUpdated = true)
            .End()
            .Build();

        root.ChangeState("a");
        root.Update(1f);
        root.Update(1f);

        Assert.True(bUpdated);
    }

    [Fact]
    public void EmptyStateMachine_Update_DoesNotThrow()
    {
        var root = new StateMachineBuilder().Build();

        var ex = Record.Exception(() => root.Update(1f));

        Assert.Null(ex);
    }

    [Fact]
    public void EmptyStateMachine_TriggerEvent_DoesNotThrow()
    {
        var root = new StateMachineBuilder().Build();

        var ex = Record.Exception(() => root.TriggerEvent("test"));

        Assert.Null(ex);
    }

    [Fact]
    public void MultipleEvents_SameName_ThrowsArgumentException()
    {
        var state = new TestState();

        state.SetEvent("test", _ => { });

        Assert.Throws<ArgumentException>(() =>
            state.SetEvent("test", _ => { }));
    }

    [Fact]
    public void Condition_ThrowsException_PropagatesUp()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => throw new InvalidOperationException("test"), _ => { })
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<InvalidOperationException>(() => root.Update(1f));
    }

    [Fact]
    public void EnterAction_ThrowsException_PropagatesUp()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => throw new InvalidOperationException("test"))
            .End()
            .Build();

        Assert.Throws<InvalidOperationException>(() => root.ChangeState("test"));
    }

    [Fact]
    public void ExitAction_ThrowsException_PropagatesUp()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(_ => throw new InvalidOperationException("test"))
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");

        Assert.Throws<InvalidOperationException>(() => root.ChangeState("b"));
    }

    [Fact]
    public void LargeNumberOfStates_WorksCorrectly()
    {
        var builder = new StateMachineBuilder();

        for (var i = 0; i < 100; i++)
        {
            builder.State($"state{i}").End();
        }

        var root = builder.Build();

        for (var i = 0; i < 100; i++)
        {
            var ex = Record.Exception(() => root.ChangeState($"state{i}"));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void LargeNumberOfConditions_AllExecuted()
    {
        var count = 0;
        var state = new TestState();

        for (var i = 0; i < 100; i++)
        {
            state.SetCondition(() => true, () => count++);
        }

        state.Update(1f);

        Assert.Equal(100, count);
    }

    [Fact]
    public void SetEnterAction_CalledMultipleTimes_LastWins()
    {
        var state = new TestState();
        var first = false;
        var second = false;

        state.SetEnterAction(() => first = true);
        state.SetEnterAction(() => second = true);

        state.Enter();

        Assert.False(first);
        Assert.True(second);
    }

}
