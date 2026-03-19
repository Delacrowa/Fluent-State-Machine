using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for event handling in the state machine.
/// </summary>
public sealed class EventTests
{

    [Fact]
    public void TriggerEvent_CallsHandler()
    {
        var handled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("myEvent", _ => handled = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("myEvent");

        Assert.True(handled);
    }

    [Fact]
    public void TriggerEvent_WithArgs_PassesArgs()
    {
        string? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("data", (_, args) => received = args.TestString)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("data", new TestEventArgs { TestString = "hello" });

        Assert.Equal("hello", received);
    }

    [Fact]
    public void TriggerEvent_UnknownEvent_DoesNotThrow()
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
    public void MultipleEvents_EachHasOwnHandler()
    {
        var eventA = false;
        var eventB = false;
        var eventC = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("a", _ => eventA = true)
                .Event("b", _ => eventB = true)
                .Event("c", _ => eventC = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("b");

        Assert.False(eventA);
        Assert.True(eventB);
        Assert.False(eventC);
    }

    [Fact]
    public void SameEvent_MultipleStates_OnlyActiveHandles()
    {
        var aHandled = false;
        var bHandled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Event("test", _ => aHandled = true)
            .End()
            .State<TestState>("b")
                .Event("test", _ => bHandled = true)
            .End()
            .Build();

        root.ChangeState("a");
        root.TriggerEvent("test");

        Assert.True(aHandled);
        Assert.False(bHandled);

        aHandled = false;
        root.ChangeState("b");
        root.TriggerEvent("test");

        Assert.False(aHandled);
        Assert.True(bHandled);
    }

    [Fact]
    public void Event_ReceivesStateReference()
    {
        TestState? captured = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("capture", s => captured = s)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("capture");

        Assert.NotNull(captured);
        Assert.IsType<TestState>(captured);
    }

    [Fact]
    public void Event_CanTriggerStateChange()
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
    public void Event_CanPushChildState()
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
    public void Event_CanPopState()
    {
        var childExited = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .State<TestState>("child")
                    .Event("pop", s => s.Parent.PopState())
                    .Exit(_ => childExited = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("pop");

        Assert.True(childExited);
    }

    [Fact]
    public void Event_WrongArgsType_Throws()
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
    public void Event_DifferentArgsTypes_DifferentHandlers()
    {
        string? stringResult = null;
        string? otherResult = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<TestEventArgs>("strEvent", (_, args) => stringResult = args.TestString)
                .Event<OtherTestEventArgs>("otherEvent", (_, args) => otherResult = args.OtherData)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("strEvent", new TestEventArgs { TestString = "str" });
        root.TriggerEvent("otherEvent", new OtherTestEventArgs { OtherData = "other" });

        Assert.Equal("str", stringResult);
        Assert.Equal("other", otherResult);
    }

    [Fact]
    public void Event_NullArgs_PassesEventArgsEmpty()
    {
        EventArgs? received = null;

        // Using legacy API directly to check
        var state = new TestState();
        state.SetEvent("test", args => received = args);
        state.TriggerEvent("test");

        Assert.Equal(EventArgs.Empty, received);
    }

    [Fact]
    public void Event_PropagatedToDeepestChild()
    {
        var parentHandled = false;
        var childHandled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Event("test", _ => parentHandled = true)
                .State<TestState>("child")
                    .Event("test", _ => childHandled = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("test");

        Assert.False(parentHandled);
        Assert.True(childHandled);
    }

    [Fact]
    public void Event_HandledByParent_WhenChildDoesNotHandle()
    {
        var parentHandled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Event("parentOnly", _ => parentHandled = true)
                .State<TestState>("child")
                    .Event("childOnly", _ => { })
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("parentOnly");

        // Event NOT handled by parent because child doesn't handle and doesn't propagate
        Assert.False(parentHandled);
    }

    [Fact]
    public void Event_DeepHierarchy_ReachesBottom()
    {
        var levels = new bool[5];

        var builder = new StateMachineBuilder()
            .State<TestState>("L0")
                .Enter(s => s.PushState("L1"))
                .Event("test", _ => levels[0] = true)
                .State<TestState>("L1")
                    .Enter(s => s.PushState("L2"))
                    .Event("test", _ => levels[1] = true)
                    .State<TestState>("L2")
                        .Enter(s => s.PushState("L3"))
                        .Event("test", _ => levels[2] = true)
                        .State<TestState>("L3")
                            .Enter(s => s.PushState("L4"))
                            .Event("test", _ => levels[3] = true)
                            .State<TestState>("L4")
                                .Event("test", _ => levels[4] = true)
                            .End()
                        .End()
                    .End()
                .End()
            .End()
            .Build();

        builder.ChangeState("L0");
        builder.TriggerEvent("test");

        Assert.False(levels[0]);
        Assert.False(levels[1]);
        Assert.False(levels[2]);
        Assert.False(levels[3]);
        Assert.True(levels[4]);
    }

    [Fact]
    public void Event_HandlerThrows_PropagatesException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("throw", _ => throw new InvalidOperationException("test"))
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<InvalidOperationException>(() => root.TriggerEvent("throw"));
    }

    [Fact]
    public void Event_CastFails_ThrowsApplicationException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event<OtherTestEventArgs>("typed", (_, _) => { })
            .End()
            .Build();

        root.ChangeState("test");

        var ex = Assert.Throws<ApplicationException>(() =>
            root.TriggerEvent("typed", new TestEventArgs()));

        Assert.Contains("invoke event", ex.Message.ToLower());
    }

    [Fact]
    public void Event_CanBeTriggerred_DuringEnter()
    {
        var eventHandled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(s => s.TriggerEvent("init"))
                .Event("init", _ => eventHandled = true)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.True(eventHandled);
    }

    [Fact]
    public void Event_CanBeTriggerred_DuringUpdate()
    {
        var eventCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((s, _) => s.TriggerEvent("tick"))
                .Event("tick", _ => eventCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void Event_CanBeTriggerred_DuringCondition()
    {
        var eventCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, s => s.TriggerEvent("cond"))
                .Event("cond", _ => eventCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void Event_SameName_ThrowsOnDuplicate()
    {
        var state = new TestState();
        state.SetEvent("test", _ => { });

        Assert.Throws<ArgumentException>(() => 
            state.SetEvent("test", _ => { }));
    }

}
