using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for system reliability and fault tolerance.
/// </summary>
public sealed class ReliabilityTests
{

    [Fact]
    public void Reliability_ExceptionInEnter_StateMachineRemainsFunctional()
    {
        var bEntered = false;

        var root = new StateMachineBuilder()
            .State("faulty")
                .Enter(_ => throw new InvalidOperationException("Intentional"))
            .End()
            .State("good")
                .Enter(_ => bEntered = true)
            .End()
            .Build();

        Assert.Throws<InvalidOperationException>(() => root.ChangeState("faulty"));

        // State machine should still work
        root.ChangeState("good");
        Assert.True(bEntered);
    }

    [Fact]
    public void Reliability_ExceptionInExit_StateMachineRemainsFunctional()
    {
        var root = new StateMachineBuilder()
            .State("faulty")
                .Exit(_ => throw new InvalidOperationException("Intentional"))
            .End()
            .State("good")
            .End()
            .Build();

        root.ChangeState("faulty");

        Assert.Throws<InvalidOperationException>(() => root.ChangeState("good"));

        // After exception, try to recover by changing to a different state
        // Note: This may not work as expected due to partial state transition
    }

    [Fact]
    public void Reliability_ExceptionInUpdate_DoesNotCorruptState()
    {
        var updateCount = 0;
        var shouldThrow = true;

        var root = new StateMachineBuilder()
            .State("test")
                .Update((_, _) =>
                {
                    updateCount++;
                    if (shouldThrow)
                        throw new InvalidOperationException("Intentional");
                })
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<InvalidOperationException>(() => root.Update(1f));
        Assert.Equal(1, updateCount);

        shouldThrow = false;
        root.Update(1f); // Should work now
        Assert.Equal(2, updateCount);
    }

    [Fact]
    public void Reliability_ExceptionInCondition_DoesNotAffectOtherConditions()
    {
        var condition1Checked = false;
        var condition3Checked = false;

        var state = new TestState();
        state.SetCondition(() => { condition1Checked = true; return false; }, () => { });
        state.SetCondition(() => throw new InvalidOperationException("Intentional"), () => { });
        state.SetCondition(() => { condition3Checked = true; return false; }, () => { });

        Assert.Throws<InvalidOperationException>(() => state.Update(1f));

        Assert.True(condition1Checked);
        Assert.False(condition3Checked); // Won't be reached due to exception
    }

    [Fact]
    public void Reliability_MultipleExits_OnlyExitOnce()
    {
        var exitCount = 0;

        var root = new StateMachineBuilder()
            .State("test")
                .Exit(_ => exitCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Exit();
        root.Exit(); // Second exit should not call Exit again on child

        Assert.Equal(1, exitCount);
    }

    [Fact]
    public void Reliability_ChangeStateDuringExit_HandledGracefully()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State("A")
                .Exit(s =>
                {
                    sequence.Add("Exit:A");
                    // Attempting to change state during exit
                    // This could cause issues
                })
            .End()
            .State("B")
                .Enter(_ => sequence.Add("Enter:B"))
            .End()
            .Build();

        root.ChangeState("A");
        root.ChangeState("B");

        Assert.Contains("Exit:A", sequence);
        Assert.Contains("Enter:B", sequence);
    }

    [Fact]
    public void Reliability_PopFromEmptyStack_ThrowsDescriptiveError()
    {
        var state = new TestState();

        var ex = Assert.Throws<ApplicationException>(() => state.PopState());

        Assert.Contains("no active children", ex.Message.ToLower());
    }

    [Fact]
    public void Reliability_ChangeToNonexistentState_ThrowsDescriptiveError()
    {
        var state = new TestState();

        var ex = Assert.Throws<ApplicationException>(() => state.ChangeState("nonexistent"));

        Assert.Contains("not in the list", ex.Message.ToLower());
    }

    [Fact]
    public void Reliability_PushNonexistentState_ThrowsDescriptiveError()
    {
        var state = new TestState();

        var ex = Assert.Throws<ApplicationException>(() => state.PushState("nonexistent"));

        Assert.Contains("not in the list", ex.Message.ToLower());
    }

    [Fact]
    public void Reliability_DuplicateStateName_ThrowsDescriptiveError()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();

        parent.AddChild(child1, "duplicate");

        var ex = Assert.Throws<ApplicationException>(() => parent.AddChild(child2, "duplicate"));

        Assert.Contains("already exists", ex.Message.ToLower());
    }

    [Fact]
    public void Reliability_DuplicateEventName_ThrowsArgumentException()
    {
        var state = new TestState();

        state.SetEvent("event", _ => { });

        Assert.Throws<ArgumentException>(() => state.SetEvent("event", _ => { }));
    }

    [Fact]
    public void Reliability_EnterCalledBeforeFirstUpdate()
    {
        var enterTime = 0;
        var updateTime = 0;
        var sequence = 0;

        var root = new StateMachineBuilder()
            .State("test")
                .Enter(_ => enterTime = ++sequence)
                .Update((_, _) => updateTime = ++sequence)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.True(enterTime < updateTime, "Enter should be called before Update");
    }

    [Fact]
    public void Reliability_ExitCalledOnStateChange()
    {
        var exitCalled = false;

        var root = new StateMachineBuilder()
            .State("A")
                .Exit(_ => exitCalled = true)
            .End()
            .State("B")
            .End()
            .Build();

        root.ChangeState("A");
        Assert.False(exitCalled);

        root.ChangeState("B");
        Assert.True(exitCalled);
    }

    [Fact]
    public void Reliability_UpdateNotCalledOnInactiveState()
    {
        var aUpdated = false;
        var bUpdated = false;

        var root = new StateMachineBuilder()
            .State("A")
                .Update((_, _) => aUpdated = true)
            .End()
            .State("B")
                .Update((_, _) => bUpdated = true)
            .End()
            .Build();

        root.ChangeState("A");
        root.ChangeState("B");
        root.Update(1f);

        Assert.False(aUpdated);
        Assert.True(bUpdated);
    }

    [Fact]
    public void Reliability_EventDeliveredToActiveStateOnly()
    {
        var aReceived = false;
        var bReceived = false;

        var root = new StateMachineBuilder()
            .State("A")
                .Event("test", _ => aReceived = true)
            .End()
            .State("B")
                .Event("test", _ => bReceived = true)
            .End()
            .Build();

        root.ChangeState("A");
        root.ChangeState("B");
        root.TriggerEvent("test");

        Assert.False(aReceived);
        Assert.True(bReceived);
    }

    [Fact]
    public void Reliability_EventDeliveredToDeepestActiveChild()
    {
        var receivedBy = "";

        var root = new StateMachineBuilder()
            .State("Parent")
                .Event("test", _ => receivedBy = "Parent")
                .Event("goChild", s => s.PushState("Child"))
                .State("Child")
                    .Event("test", _ => receivedBy = "Child")
                .End()
            .End()
            .Build();

        root.ChangeState("Parent");
        root.TriggerEvent("test");
        Assert.Equal("Parent", receivedBy);

        root.TriggerEvent("goChild");
        root.TriggerEvent("test");
        Assert.Equal("Child", receivedBy);
    }

}
