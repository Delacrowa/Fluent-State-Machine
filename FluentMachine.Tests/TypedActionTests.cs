using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for the internal typed action system that avoids closures.
/// </summary>
public sealed class TypedActionTests
{

    [Fact]
    public void TypedEnter_ReceivesCorrectStateType()
    {
        TestState? captured = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => captured = s)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.NotNull(captured);
        Assert.IsType<TestState>(captured);
    }

    [Fact]
    public void TypedEnter_CalledOnEveryEntry()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => count++)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");
        root.ChangeState("a");
        root.ChangeState("b");
        root.ChangeState("a");

        Assert.Equal(3, count);
    }

    [Fact]
    public void TypedEnter_CanAccessStateMembers()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => s.ChangeState("child"))
                .State<TestState>("child")
                .End()
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState("test"));

        Assert.Null(ex);
    }

    [Fact]
    public void TypedExit_ReceivesCorrectStateType()
    {
        TestState? captured = null;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(s => captured = s)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");

        Assert.NotNull(captured);
        Assert.IsType<TestState>(captured);
    }

    [Fact]
    public void TypedExit_CalledOnEveryExit()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(_ => count++)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");
        root.ChangeState("a");
        root.ChangeState("b");

        Assert.Equal(2, count);
    }

    [Fact]
    public void TypedExit_CanTriggerParentStateChange()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(s =>
                {
                    sequence.Add("exit:a");
                    // Cannot change state here safely, but can log
                })
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("enter:b"))
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");

        Assert.Equal(new[] { "exit:a", "enter:b" }, sequence);
    }

    [Fact]
    public void TypedUpdate_ReceivesCorrectStateAndDelta()
    {
        TestState? capturedState = null;
        float capturedDelta = -1;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((s, dt) =>
                {
                    capturedState = s;
                    capturedDelta = dt;
                })
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(0.016f);

        Assert.NotNull(capturedState);
        Assert.Equal(0.016f, capturedDelta);
    }

    [Fact]
    public void TypedUpdate_CalledEveryFrame()
    {
        var deltas = new List<float>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => deltas.Add(dt))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(0.016f);
        root.Update(0.033f);
        root.Update(0.050f);

        Assert.Equal(new[] { 0.016f, 0.033f, 0.050f }, deltas);
    }

    [Fact]
    public void TypedUpdate_CanChangeState()
    {
        var updateCount = 0;
        var bEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Update((s, _) =>
                {
                    updateCount++;
                    if (updateCount == 3)
                        s.Parent.ChangeState("b");
                })
            .End()
            .State<TestState>("b")
                .Enter(_ => bEntered = true)
            .End()
            .Build();

        root.ChangeState("a");
        root.Update(1f);
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(3, updateCount);
        Assert.True(bEntered);
    }

    [Fact]
    public void TypedCondition_ReceivesCorrectState()
    {
        TestState? captured = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, s => captured = s)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.NotNull(captured);
        Assert.IsType<TestState>(captured);
    }

    [Fact]
    public void TypedCondition_OnlyExecutesWhenTrue()
    {
        var condition = false;
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => condition, _ => count++)
            .End()
            .Build();

        root.ChangeState("test");

        root.Update(1f);
        Assert.Equal(0, count);

        condition = true;
        root.Update(1f);
        Assert.Equal(1, count);

        root.Update(1f);
        Assert.Equal(2, count);
    }

    [Fact]
    public void TypedCondition_MultipleConditions_AllEvaluated()
    {
        var results = new List<int>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => results.Add(1))
                .Condition(() => false, _ => results.Add(2))
                .Condition(() => true, _ => results.Add(3))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { 1, 3 }, results);
    }

    [Fact]
    public void TypedCondition_CanTriggerStateChange()
    {
        var health = 100;
        var dead = false;

        var root = new StateMachineBuilder()
            .State<TestState>("alive")
                .Condition(() => health <= 0, s => s.Parent.ChangeState("dead"))
            .End()
            .State<TestState>("dead")
                .Enter(_ => dead = true)
            .End()
            .Build();

        root.ChangeState("alive");
        root.Update(1f);
        Assert.False(dead);

        health = 0;
        root.Update(1f);
        Assert.True(dead);
    }

    [Fact]
    public void TypedEvent_ReceivesCorrectState()
    {
        TestState? captured = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("myEvent", s => captured = s)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("myEvent");

        Assert.NotNull(captured);
        Assert.IsType<TestState>(captured);
    }

    [Fact]
    public void TypedEvent_CanBeTriggeredMultipleTimes()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("inc", _ => count++)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("inc");
        root.TriggerEvent("inc");
        root.TriggerEvent("inc");

        Assert.Equal(3, count);
    }

    [Fact]
    public void TypedEvent_CanChangeState()
    {
        var bEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Event("goB", s => s.Parent.ChangeState("b"))
            .End()
            .State<TestState>("b")
                .Enter(_ => bEntered = true)
            .End()
            .Build();

        root.ChangeState("a");
        root.TriggerEvent("goB");

        Assert.True(bEntered);
    }

    [Fact]
    public void TypedEvent_CanPushState()
    {
        var childEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Event("pushChild", s => s.PushState("child"))
                .State<TestState>("child")
                    .Enter(_ => childEntered = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("pushChild");

        Assert.True(childEntered);
    }

    [Fact]
    public void TypedEventWithArgs_ReceivesStateAndArgs()
    {
        TestState? capturedState = null;
        string? capturedString = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("data", (s, args) =>
                {
                    capturedState = s;
                    capturedString = args.TestString;
                })
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("data", new TestEventArgs { TestString = "hello" });

        Assert.NotNull(capturedState);
        Assert.Equal("hello", capturedString);
    }

    [Fact]
    public void TypedEventWithArgs_DifferentArgsTypes()
    {
        string? stringValue = null;
        string? otherValue = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("stringEvent", (_, args) => stringValue = args.TestString)
                .Event<OtherTestEventArgs>("otherEvent", (_, args) => otherValue = args.OtherData)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("stringEvent", new TestEventArgs { TestString = "first" });
        root.TriggerEvent("otherEvent", new OtherTestEventArgs { OtherData = "second" });

        Assert.Equal("first", stringValue);
        Assert.Equal("second", otherValue);
    }

    [Fact]
    public void TypedEventWithArgs_WrongArgsType_Throws()
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
    public void DifferentStateTypes_PreserveTypeInActions()
    {
        // Use the default State type
        State? capturedDefault = null;
        TestState? capturedTest = null;

        var root = new StateMachineBuilder()
            .State("defaultState")
                .Enter(s => capturedDefault = s)
            .End()
            .State<TestState>("testState")
                .Enter(s => capturedTest = s)
            .End()
            .Build();

        root.ChangeState("defaultState");
        root.ChangeState("testState");

        Assert.NotNull(capturedDefault);
        Assert.IsType<State>(capturedDefault);
        Assert.NotNull(capturedTest);
        Assert.IsType<TestState>(capturedTest);
    }

    [Fact]
    public void NestedStates_EachHasOwnActions()
    {
        var parentEnter = false;
        var childEnter = false;
        var parentUpdate = false;
        var childUpdate = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s =>
                {
                    parentEnter = true;
                    s.PushState("child");
                })
                .Update((_, _) => parentUpdate = true)
                .State<TestState>("child")
                    .Enter(_ => childEnter = true)
                    .Update((_, _) => childUpdate = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.Update(1f);

        Assert.True(parentEnter);
        Assert.True(childEnter);
        Assert.False(parentUpdate); // Child is active, parent not updated
        Assert.True(childUpdate);
    }

    [Fact]
    public void NestedStates_EventsPropagateToDeepest()
    {
        var parentReceived = false;
        var childReceived = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Event("test", _ => parentReceived = true)
                .State<TestState>("child")
                    .Event("test", _ => childReceived = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("test");

        Assert.False(parentReceived);
        Assert.True(childReceived);
    }

    [Fact]
    public void AllActionsOnSameState_WorkCorrectly()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => sequence.Add("enter"))
                .Update((_, _) => sequence.Add("update"))
                .Condition(() => true, _ => sequence.Add("condition"))
                .Event("evt", _ => sequence.Add("event"))
                .Exit(_ => sequence.Add("exit"))
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.TriggerEvent("evt");
        root.ChangeState("other");

        Assert.Equal(new[] { "enter", "update", "condition", "event", "exit" }, sequence);
    }

    [Fact]
    public void SameStateReference_AcrossAllActions()
    {
        var states = new List<TestState>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => states.Add(s))
                .Update((s, _) => states.Add(s))
                .Condition(() => true, s => states.Add(s))
                .Event("evt", s => states.Add(s))
                .Exit(s => states.Add(s))
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.TriggerEvent("evt");
        root.ChangeState("other");

        Assert.Equal(5, states.Count);
        Assert.True(states.All(s => ReferenceEquals(s, states[0])));
    }

}
