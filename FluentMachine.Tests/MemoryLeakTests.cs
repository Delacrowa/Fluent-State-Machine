using System;
using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests to verify that the new internal implementation doesn't create memory leaks via closures.
/// </summary>
public sealed class MemoryLeakTests
{
    [Fact]
    public void StateBuilder_NoClosureCapture_EnterAction()
    {
        // Arrange - create state machine with Enter action
        WeakReference? stateRef = null;

        void CreateAndRelease()
        {
            var root = new StateMachineBuilder()
                .State<TestState>("test")
                    .Enter(s => { /* Action using state */ })
                .End()
                .Build();

            root.ChangeState("test");

            // Get weak reference to the state
            stateRef = new WeakReference(root);
        }

        CreateAndRelease();

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // State should be collected (no closure holding reference)
        Assert.False(stateRef!.IsAlive, "State was not garbage collected - possible memory leak via closure");
    }

    [Fact]
    public void StateBuilder_NoClosureCapture_UpdateAction()
    {
        WeakReference? stateRef = null;

        void CreateAndRelease()
        {
            var root = new StateMachineBuilder()
                .State<TestState>("test")
                    .Update((s, dt) => { /* Action using state and delta */ })
                .End()
                .Build();

            root.ChangeState("test");
            root.Update(1f);

            stateRef = new WeakReference(root);
        }

        CreateAndRelease();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(stateRef!.IsAlive, "State was not garbage collected - possible memory leak via closure");
    }

    [Fact]
    public void StateBuilder_NoClosureCapture_EventAction()
    {
        WeakReference? stateRef = null;

        void CreateAndRelease()
        {
            var root = new StateMachineBuilder()
                .State<TestState>("test")
                    .Event("myEvent", s => { /* Action using state */ })
                .End()
                .Build();

            root.ChangeState("test");
            root.TriggerEvent("myEvent");

            stateRef = new WeakReference(root);
        }

        CreateAndRelease();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(stateRef!.IsAlive, "State was not garbage collected - possible memory leak via closure");
    }

    [Fact]
    public void StateBuilder_NoClosureCapture_ConditionAction()
    {
        WeakReference? stateRef = null;

        void CreateAndRelease()
        {
            var root = new StateMachineBuilder()
                .State<TestState>("test")
                    .Condition(() => true, s => { /* Action using state */ })
                .End()
                .Build();

            root.ChangeState("test");
            root.Update(1f);

            stateRef = new WeakReference(root);
        }

        CreateAndRelease();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(stateRef!.IsAlive, "State was not garbage collected - possible memory leak via closure");
    }

    [Fact]
    public void StateBuilder_NoClosureCapture_ExitAction()
    {
        WeakReference? stateRef = null;

        void CreateAndRelease()
        {
            var root = new StateMachineBuilder()
                .State<TestState>("a")
                    .Exit(s => { /* Action using state */ })
                .End()
                .State<TestState>("b")
                .End()
                .Build();

            root.ChangeState("a");
            root.ChangeState("b"); // This triggers Exit on "a"

            stateRef = new WeakReference(root);
        }

        CreateAndRelease();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(stateRef!.IsAlive, "State was not garbage collected - possible memory leak via closure");
    }

    [Fact]
    public void StateBuilder_NoClosureCapture_ComplexHierarchy()
    {
        WeakReference? stateRef = null;

        void CreateAndRelease()
        {
            var root = new StateMachineBuilder()
                .State<TestState>("parent")
                    .Enter(s => s.PushState("child1"))
                    .Update((s, dt) => { })
                    .Exit(s => { })
                    .Event("test", s => { })
                    .Condition(() => false, s => { })
                    .State<TestState>("child1")
                        .Enter(s => { })
                        .Update((s, dt) => { })
                        .Event("childEvent", s => { })
                    .End()
                    .State<TestState>("child2")
                        .Enter(s => { })
                    .End()
                .End()
                .Build();

            root.ChangeState("parent");
            root.Update(1f);
            root.TriggerEvent("childEvent");

            stateRef = new WeakReference(root);
        }

        CreateAndRelease();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(stateRef!.IsAlive, "Complex hierarchy was not garbage collected - possible memory leak via closures");
    }

    [Fact]
    public void TypedActions_ExecuteCorrectly_AfterMultipleUpdates()
    {
        // Verify that typed actions work correctly
        var enterCalled = false;
        var exitCalled = false;
        var updateCount = 0;
        var eventCalled = false;
        var conditionCalled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => enterCalled = true)
                .Exit(s => exitCalled = true)
                .Update((s, dt) => updateCount++)
                .Event("myEvent", s => eventCalled = true)
                .Condition(() => true, s => conditionCalled = true)
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        // Enter
        root.ChangeState("test");
        Assert.True(enterCalled);

        // Update
        root.Update(1f);
        root.Update(1f);
        Assert.Equal(2, updateCount);

        // Condition
        Assert.True(conditionCalled);

        // Event
        root.TriggerEvent("myEvent");
        Assert.True(eventCalled);

        // Exit
        root.ChangeState("other");
        Assert.True(exitCalled);
    }

    [Fact]
    public void TypedEventWithArgs_ExecutesCorrectly()
    {
        string? receivedValue = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("data", (s, args) => receivedValue = args.TestString)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("data", new TestEventArgs { TestString = "hello" });

        Assert.Equal("hello", receivedValue);
    }

    [Fact]
    public void StateReference_PassedCorrectly_ToAllActions()
    {
        TestState? capturedInEnter = null;
        TestState? capturedInUpdate = null;
        TestState? capturedInExit = null;
        TestState? capturedInEvent = null;
        TestState? capturedInCondition = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => capturedInEnter = s)
                .Update((s, dt) => capturedInUpdate = s)
                .Event("test", s => capturedInEvent = s)
                .Condition(() => true, s => capturedInCondition = s)
                .Exit(s => capturedInExit = s)
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.TriggerEvent("test");
        root.ChangeState("other");

        // All captures should be the same state instance
        Assert.NotNull(capturedInEnter);
        Assert.Same(capturedInEnter, capturedInUpdate);
        Assert.Same(capturedInEnter, capturedInEvent);
        Assert.Same(capturedInEnter, capturedInCondition);
        Assert.Same(capturedInEnter, capturedInExit);
    }
}
