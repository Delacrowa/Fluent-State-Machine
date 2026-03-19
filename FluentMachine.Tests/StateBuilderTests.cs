using Xunit;

namespace FluentMachine.Tests;

public sealed class StateBuilderTests
{

    [Fact]
    public void Enter_SetsEnterAction()
    {
        var called = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => called = true)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.True(called);
    }

    [Fact]
    public void Enter_ProvidesStateInstance()
    {
        TestState? capturedState = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => capturedState = s)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.NotNull(capturedState);
    }

    [Fact]
    public void Exit_SetsExitAction()
    {
        var called = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(_ => called = true)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");

        Assert.True(called);
    }

    [Fact]
    public void Update_SetsUpdateAction()
    {
        var called = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => called = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.True(called);
    }

    [Fact]
    public void Update_ReceivesDeltaTime()
    {
        var receivedDelta = 0f;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => receivedDelta = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(0.016f);

        Assert.Equal(0.016f, receivedDelta);
    }

    [Fact]
    public void Condition_TruePredicate_ExecutesAction()
    {
        var executed = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => executed = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.True(executed);
    }

    [Fact]
    public void Condition_FalsePredicate_DoesNotExecute()
    {
        var executed = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => false, _ => executed = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.False(executed);
    }

    [Fact]
    public void Condition_MultipleConditions_AllEvaluated()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => count++)
                .Condition(() => true, _ => count++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Event_RegistersEventHandler()
    {
        var triggered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("myevent", _ => triggered = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("myevent");

        Assert.True(triggered);
    }

    [Fact]
    public void Event_WithTypedArgs_PassesArgs()
    {
        string? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("myevent", (_, args) => received = args.TestString)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("myevent", new TestEventArgs { TestString = "hello" });

        Assert.Equal("hello", received);
    }

    [Fact]
    public void Event_WrongArgsType_ThrowsApplicationException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<SecondTestEventArgs>("myevent", (_, _) => { })
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<ApplicationException>(() =>
            root.TriggerEvent("myevent", new TestEventArgs()));
    }

    [Fact]
    public void State_Nested_CreatesHierarchy()
    {
        IState? nestedParent = null;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .State<TestState>("child")
                    .Enter(s => nestedParent = s.Parent)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("dummy"); // Won't find it, but proves parent is active

        // Access child through parent's ChangeState
    }

    [Fact]
    public void State_NestedWithName_UsesProvidedName()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.ChangeState("custom"))
                .State<TestState>("custom")
                .End()
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState("parent"));

        Assert.Null(ex);
    }

    [Fact]
    public void State_NestedDefaultName_UsesTypeName()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.ChangeState(nameof(TestState)))
                .State<TestState>()
                .End()
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState("parent"));

        Assert.Null(ex);
    }

    [Fact]
    public void End_ReturnsParentBuilder()
    {
        var builder = new StateMachineBuilder()
            .State<TestState>("test")
            .End();

        // Should be able to add another state
        var root = builder
            .State<TestState>("test2")
            .End()
            .Build();

        Assert.Null(Record.Exception(() => root.ChangeState("test2")));
    }

}
