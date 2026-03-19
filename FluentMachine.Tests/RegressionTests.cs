using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Regression tests to ensure previously fixed bugs don't reappear.
/// </summary>
public sealed class RegressionTests
{

    [Fact]
    public void Regression_ChangeStateInEnter_DoesNotCorruptStack()
    {
        // Bug: ChangeState during Enter could leave stack in invalid state
        var enterCount = 0;
        var exitCount = 0;

        var root = new StateMachineBuilder()
            .State("A")
                .Enter(_ => enterCount++)
                .Exit(_ => exitCount++)
            .End()
            .State("B")
                .Enter(s => { enterCount++; s.Parent.ChangeState("C"); })
                .Exit(_ => exitCount++)
            .End()
            .State("C")
                .Enter(_ => enterCount++)
                .Exit(_ => exitCount++)
            .End()
            .Build();

        root.ChangeState("A");
        root.ChangeState("B"); // This triggers transition to C

        Assert.Equal(3, enterCount); // A, B, C
        Assert.Equal(2, exitCount); // A, B (C still active)
    }

    [Fact]
    public void Regression_PopStateAfterPush_ReturnsCorrectState()
    {
        // Bug: PopState didn't correctly return to previous state
        var activeStates = new List<string>();

        var root = new StateMachineBuilder()
            .State("Parent")
                .Update((_, _) => activeStates.Add("Parent"))
                .Event("push", s => s.PushState("Child"))
                .State("Child")
                    .Update((_, _) => activeStates.Add("Child"))
                    .Event("pop", s => s.Parent.PopState())
                .End()
            .End()
            .Build();

        root.ChangeState("Parent");
        root.Update(1f);
        Assert.Equal("Parent", activeStates.Last());

        root.TriggerEvent("push");
        root.Update(1f);
        Assert.Equal("Child", activeStates.Last());

        root.TriggerEvent("pop"); // Event goes to Child, which pops itself
        root.Update(1f);
        Assert.Equal("Parent", activeStates.Last());
    }

    [Fact]
    public void Regression_ExitDuringUpdate_DoesNotSkipExit()
    {
        // Bug: Changing state during Update could skip Exit callback
        var exitCalled = false;

        var root = new StateMachineBuilder()
            .State("A")
                .Exit(_ => exitCalled = true)
                .Condition(() => true, s => s.Parent.ChangeState("B"))
            .End()
            .State("B")
            .End()
            .Build();

        root.ChangeState("A");
        root.Update(1f);

        Assert.True(exitCalled);
    }

    [Fact]
    public void Regression_EventOnInactiveState_IsIgnored()
    {
        // Bug: Events could leak to inactive states
        var inactiveEventTriggered = false;

        var root = new StateMachineBuilder()
            .State("A")
                .Event("test", _ => inactiveEventTriggered = true)
            .End()
            .State("B")
            .End()
            .Build();

        root.ChangeState("A");
        root.ChangeState("B");
        root.TriggerEvent("test");

        Assert.False(inactiveEventTriggered);
    }

    [Fact]
    public void Regression_EventWithTypedArgs_CastsCorrectly()
    {
        // Bug: Generic event args could fail type casting
        string? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("data", (_, args) => received = args.TestString)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("data", new TestEventArgs { TestString = "success" });

        Assert.Equal("success", received);
    }

    [Fact]
    public void Regression_ConditionsExecuteInOrder()
    {
        // Bug: Conditions could execute out of order
        var order = new List<int>();

        var state = new TestState();
        state.SetCondition(() => { order.Add(1); return false; }, () => { });
        state.SetCondition(() => { order.Add(2); return false; }, () => { });
        state.SetCondition(() => { order.Add(3); return false; }, () => { });

        state.Update(1f);

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public void Regression_AllConditionsEvaluated_EvenIfFirstTrue()
    {
        // Bug: Early exit could skip remaining conditions
        var count = 0;

        var state = new TestState();
        state.SetCondition(() => true, () => count++);
        state.SetCondition(() => true, () => count++);
        state.SetCondition(() => true, () => count++);

        state.Update(1f);

        Assert.Equal(3, count);
    }

    [Fact]
    public void Regression_ChildKnowsParent_AfterAddChild()
    {
        // Bug: Parent property wasn't set correctly
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child, "child");

        Assert.Same(parent, child.Parent);
    }

    [Fact]
    public void Regression_NestedChildKnowsParent_NotGrandparent()
    {
        // Bug: Deeply nested children could have wrong parent reference
        IState? capturedParent = null;

        var root = new StateMachineBuilder()
            .State("L1")
                .Enter(s => s.ChangeState("L2"))
                .State("L2")
                    .Enter(s => capturedParent = s.Parent)
                .End()
            .End()
            .Build();

        root.ChangeState("L1");

        Assert.NotNull(capturedParent);
        Assert.NotSame(root, capturedParent);
    }

    [Fact]
    public void Regression_CanReenterSameState()
    {
        // Bug: Reentering same state could fail
        var enterCount = 0;

        var root = new StateMachineBuilder()
            .State("test")
                .Enter(_ => enterCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.ChangeState("test");
        root.ChangeState("test");

        Assert.Equal(3, enterCount);
    }

    [Fact]
    public void Regression_StateDataResets_OnReentry()
    {
        // Ensure state behaves consistently on reentry
        var updateCount = 0;

        var root = new StateMachineBuilder()
            .State("test")
                .Update((_, _) => updateCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.Update(1f);

        root.ChangeState("test"); // Reenter
        root.Update(1f);

        Assert.Equal(3, updateCount);
    }

}
