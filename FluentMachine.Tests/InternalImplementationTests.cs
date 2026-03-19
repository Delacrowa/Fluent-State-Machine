using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests verifying internal implementation details work correctly.
/// These test the action wrapper classes indirectly through the public API.
/// </summary>
public sealed class InternalImplementationTests
{

    [Fact]
    public void EnterAction_ReceivesStateReference()
    {
        TestState? capturedState = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => capturedState = s)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.NotNull(capturedState);
        Assert.IsType<TestState>(capturedState);
    }

    [Fact]
    public void ExitAction_NoClosureCapture()
    {
        var localValue = 42;
        var capturedAtExit = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(_ => capturedAtExit = localValue)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        localValue = 100;
        root.ChangeState("b");

        Assert.Equal(100, capturedAtExit); // Value at time of exit
    }

    [Fact]
    public void UpdateAction_ReceivesCurrentDelta()
    {
        var deltas = new List<float>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => deltas.Add(dt))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(0.1f);
        root.Update(0.2f);
        root.Update(0.3f);

        Assert.Equal(new[] { 0.1f, 0.2f, 0.3f }, deltas);
    }

    [Fact]
    public void UpdateAction_StateReferenceConsistent()
    {
        var states = new List<TestState>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((s, _) => states.Add(s))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(3, states.Count);
        Assert.True(states.All(s => ReferenceEquals(s, states[0])));
    }

    [Fact]
    public void ConditionAction_StateReferenceConsistent()
    {
        var states = new List<TestState>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, s => states.Add(s))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(2, states.Count);
        Assert.Same(states[0], states[1]);
    }

    [Fact]
    public void ConditionAction_PredicateEvaluatedFresh()
    {
        var counter = 0;
        var actionCalls = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => counter % 2 == 0, _ => actionCalls++)
            .End()
            .Build();

        root.ChangeState("test");

        counter = 0;
        root.Update(1f);
        Assert.Equal(1, actionCalls);

        counter = 1;
        root.Update(1f);
        Assert.Equal(1, actionCalls); // Not called

        counter = 2;
        root.Update(1f);
        Assert.Equal(2, actionCalls);
    }

    [Fact]
    public void EventAction_StateReferenceConsistent()
    {
        var states = new List<TestState>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("capture", s => states.Add(s))
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("capture");
        root.TriggerEvent("capture");
        root.TriggerEvent("capture");

        Assert.Equal(3, states.Count);
        Assert.True(states.All(s => ReferenceEquals(s, states[0])));
    }

    [Fact]
    public void EventAction_TypedArgs_CastedCorrectly()
    {
        TestEventArgs? captured = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("data", (_, args) => captured = args)
            .End()
            .Build();

        root.ChangeState("test");
        var original = new TestEventArgs { TestString = "test" };
        root.TriggerEvent("data", original);

        Assert.Same(original, captured);
    }

    [Fact]
    public void NoEnterAction_DoesNotThrow()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState("test"));

        Assert.Null(ex);
    }

    [Fact]
    public void NoExitAction_DoesNotThrow()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("a")
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        var ex = Record.Exception(() => root.ChangeState("b"));

        Assert.Null(ex);
    }

    [Fact]
    public void NoUpdateAction_DoesNotThrow()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
            .End()
            .Build();

        root.ChangeState("test");
        var ex = Record.Exception(() => root.Update(1f));

        Assert.Null(ex);
    }

    [Fact]
    public void NoConditions_DoesNotThrow()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
            .End()
            .Build();

        root.ChangeState("test");
        var ex = Record.Exception(() => root.Update(1f));

        Assert.Null(ex);
    }

    [Fact]
    public void NoEvents_UnknownEventDoesNotThrow()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
            .End()
            .Build();

        root.ChangeState("test");
        var ex = Record.Exception(() => root.TriggerEvent("unknown"));

        Assert.Null(ex);
    }

    [Fact]
    public void ReplacingEnterAction_OldNotCalled()
    {
        var oldCalled = false;
        var newCalled = false;

        var state = new TestState();
        state.SetEnterAction(() => oldCalled = true);
        state.SetEnterAction(() => newCalled = true);
        state.Enter();

        Assert.False(oldCalled);
        Assert.True(newCalled);
    }

    [Fact]
    public void ReplacingExitAction_OldNotCalled()
    {
        var oldCalled = false;
        var newCalled = false;

        var state = new TestState();
        state.SetExitAction(() => oldCalled = true);
        state.SetExitAction(() => newCalled = true);
        state.Exit();

        Assert.False(oldCalled);
        Assert.True(newCalled);
    }

    [Fact]
    public void ReplacingUpdateAction_OldNotCalled()
    {
        var oldCalled = false;
        var newCalled = false;

        var state = new TestState();
        state.SetUpdateAction(_ => oldCalled = true);
        state.SetUpdateAction(_ => newCalled = true);
        state.Update(1f);

        Assert.False(oldCalled);
        Assert.True(newCalled);
    }

    [Fact]
    public void DuplicateEventHandler_ThrowsArgumentException()
    {
        var state = new TestState();
        state.SetEvent("test", _ => { });

        Assert.Throws<ArgumentException>(() => 
            state.SetEvent("test", _ => { }));
    }

    [Fact]
    public void ManyStates_EachHasIndependentActions()
    {
        var enterCounts = new int[10];

        var builder = new StateMachineBuilder();
        for (var i = 0; i < 10; i++)
        {
            var idx = i;
            builder = builder
                .State<TestState>($"state{i}")
                    .Enter(_ => enterCounts[idx]++)
                .End();
        }
        var root = builder.Build();

        root.ChangeState("state5");
        root.ChangeState("state3");
        root.ChangeState("state5");

        Assert.Equal(0, enterCounts[0]);
        Assert.Equal(1, enterCounts[3]);
        Assert.Equal(2, enterCounts[5]);
    }

    [Fact]
    public void SameStateMultipleCallbacks_AllIndependent()
    {
        var sequence = new List<int>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => sequence.Add(1))
                .Update((_, _) => sequence.Add(2))
                .Condition(() => true, _ => sequence.Add(3))
                .Event("evt", _ => sequence.Add(4))
                .Exit(_ => sequence.Add(5))
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.TriggerEvent("evt");
        root.ChangeState("other");

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, sequence);
    }

}
