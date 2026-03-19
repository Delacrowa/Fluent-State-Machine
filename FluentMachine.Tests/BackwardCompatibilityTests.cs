using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests to verify backward compatibility with legacy API usage.
/// </summary>
public sealed class BackwardCompatibilityTests
{

    [Fact]
    public void LegacySetEnterAction_StillWorks()
    {
        var state = new TestState();
        var called = false;

        state.SetEnterAction(() => called = true);
        state.Enter();

        Assert.True(called);
    }

    [Fact]
    public void LegacySetEnterAction_CanBeOverwritten()
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

    [Fact]
    public void LegacySetExitAction_StillWorks()
    {
        var state = new TestState();
        var called = false;

        state.SetExitAction(() => called = true);
        state.Exit();

        Assert.True(called);
    }

    [Fact]
    public void LegacySetUpdateAction_StillWorks()
    {
        var state = new TestState();
        var receivedDelta = -1f;

        state.SetUpdateAction(dt => receivedDelta = dt);
        state.Update(0.016f);

        Assert.Equal(0.016f, receivedDelta);
    }

    [Fact]
    public void LegacySetCondition_StillWorks()
    {
        var state = new TestState();
        var condition = false;
        var count = 0;

        state.SetCondition(() => condition, () => count++);

        state.Update(1f);
        Assert.Equal(0, count);

        condition = true;
        state.Update(1f);
        Assert.Equal(1, count);
    }

    [Fact]
    public void LegacySetCondition_MultipleConds_AllEvaluated()
    {
        var state = new TestState();
        var count = 0;

        state.SetCondition(() => true, () => count++);
        state.SetCondition(() => true, () => count++);
        state.SetCondition(() => false, () => count++);

        state.Update(1f);

        Assert.Equal(2, count);
    }

    [Fact]
    public void LegacySetEvent_StillWorks()
    {
        var state = new TestState();
        var called = false;

        state.SetEvent("test", _ => called = true);
        state.TriggerEvent("test");

        Assert.True(called);
    }

    [Fact]
    public void LegacySetEventWithArgs_StillWorks()
    {
        var state = new TestState();
        string? received = null;

        state.SetEvent<TestEventArgs>("data", args => received = args.TestString);
        state.TriggerEvent("data", new TestEventArgs { TestString = "hello" });

        Assert.Equal("hello", received);
    }

    [Fact]
    public void LegacySetEvent_WrongArgsType_Throws()
    {
        var state = new TestState();

        state.SetEvent<OtherTestEventArgs>("typed", _ => { });

        Assert.Throws<ApplicationException>(() =>
            state.TriggerEvent("typed", new TestEventArgs()));
    }

    [Fact]
    public void MixedUsage_LegacyAndBuilder_BothWork()
    {
        // Create state machine with builder
        var builderEnter = false;

        var root = new StateMachineBuilder()
            .State<TestState>("builderState")
                .Enter(_ => builderEnter = true)
            .End()
            .Build();

        // Also use legacy API directly on a separate state
        var legacyState = new TestState();
        var legacyEnter = false;
        legacyState.SetEnterAction(() => legacyEnter = true);

        // Both should work
        root.ChangeState("builderState");
        legacyState.Enter();

        Assert.True(builderEnter);
        Assert.True(legacyEnter);
    }

    [Fact]
    public void DirectUsage_AddChild_Works()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child, "child");
        parent.ChangeState("child");

        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void DirectUsage_PushPopState_Works()
    {
        var parent = new TestState();
        var child = new TestState();
        var childEntered = false;
        var childExited = false;

        child.SetEnterAction(() => childEntered = true);
        child.SetExitAction(() => childExited = true);

        parent.AddChild(child, "child");
        parent.PushState("child");

        Assert.True(childEntered);

        parent.PopState();

        Assert.True(childExited);
    }

    [Fact]
    public void DirectUsage_FullHierarchy_Works()
    {
        var root = new TestState();
        var level1 = new TestState();
        var level2 = new TestState();

        var sequence = new List<string>();

        level1.SetEnterAction(() => sequence.Add("L1:enter"));
        level1.SetExitAction(() => sequence.Add("L1:exit"));
        level2.SetEnterAction(() => sequence.Add("L2:enter"));
        level2.SetExitAction(() => sequence.Add("L2:exit"));

        root.AddChild(level1, "L1");
        level1.AddChild(level2, "L2");

        root.ChangeState("L1");
        level1.ChangeState("L2");
        level1.PopState();
        root.PopState();

        Assert.Equal(new[] { "L1:enter", "L2:enter", "L2:exit", "L1:exit" }, sequence);
    }

    [Fact]
    public void TriggerEvent_WithoutArgs_Works()
    {
        var state = new TestState();
        EventArgs? received = null;

        state.SetEvent("test", args => received = args);
        state.TriggerEvent("test");

        Assert.Equal(EventArgs.Empty, received);
    }

    [Fact]
    public void TriggerEvent_NonExistent_DoesNotThrow()
    {
        var state = new TestState();

        var ex = Record.Exception(() => state.TriggerEvent("nonexistent"));

        Assert.Null(ex);
    }

}
