using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for the fluent API of the state machine builder.
/// </summary>
public sealed class BuilderFluentApiTests
{

    [Fact]
    public void Builder_Build_ReturnsState()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
            .End()
            .Build();

        Assert.NotNull(root);
        Assert.IsType<State>(root);
    }

    [Fact]
    public void Builder_NoStates_ReturnsEmptyRoot()
    {
        var root = new StateMachineBuilder()
            .Build();

        Assert.NotNull(root);
    }

    [Fact]
    public void Builder_MultipleStates_AllAccessible()
    {
        var statesEntered = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => statesEntered.Add("a"))
            .End()
            .State<TestState>("b")
                .Enter(_ => statesEntered.Add("b"))
            .End()
            .State<TestState>("c")
                .Enter(_ => statesEntered.Add("c"))
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");
        root.ChangeState("c");

        Assert.Equal(new[] { "a", "b", "c" }, statesEntered);
    }

    [Fact]
    public void Builder_AllMethodsChainable()
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
    public void Builder_NestedStatesChainable()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("L1")
                .Enter(s =>
                {
                    sequence.Add("L1:enter");
                    s.PushState("L2");
                })
                .State<TestState>("L2")
                    .Enter(s =>
                    {
                        sequence.Add("L2:enter");
                        s.PushState("L3");
                    })
                    .State<TestState>("L3")
                        .Enter(_ => sequence.Add("L3:enter"))
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("L1");

        Assert.Equal(new[] { "L1:enter", "L2:enter", "L3:enter" }, sequence);
    }

    [Fact]
    public void Builder_DefaultStateType()
    {
        State? captured = null;

        var root = new StateMachineBuilder()
            .State("test")
                .Enter(s => captured = s)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.NotNull(captured);
        Assert.IsType<State>(captured);
    }

    [Fact]
    public void Builder_CustomStateType()
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
    public void Builder_MixedStateTypes()
    {
        var types = new List<Type>();

        var root = new StateMachineBuilder()
            .State("default")
                .Enter(s => types.Add(s.GetType()))
            .End()
            .State<TestState>("custom")
                .Enter(s => types.Add(s.GetType()))
            .End()
            .Build();

        root.ChangeState("default");
        root.ChangeState("custom");

        Assert.Equal(new[] { typeof(State), typeof(TestState) }, types);
    }

    [Fact]
    public void Builder_MultipleEnter_LastWins()
    {
        var first = false;
        var second = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => first = true)
                .Enter(_ => second = true)
            .End()
            .Build();

        root.ChangeState("test");

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void Builder_MultipleUpdate_LastWins()
    {
        var first = false;
        var second = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => first = true)
                .Update((_, _) => second = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void Builder_MultipleExit_LastWins()
    {
        var first = false;
        var second = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Exit(_ => first = true)
                .Exit(_ => second = true)
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("test");
        root.ChangeState("other");

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void Builder_MultipleConditions_AllExecute()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => count++)
                .Condition(() => true, _ => count++)
                .Condition(() => true, _ => count++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(3, count);
    }

    [Fact]
    public void Builder_DifferentEvents_AllRegistered()
    {
        var first = false;
        var second = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("first", _ => first = true)
                .Event("second", _ => second = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("first");
        root.TriggerEvent("second");

        Assert.True(first);
        Assert.True(second);
    }

    [Fact]
    public void Builder_EmptyState_Works()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("empty")
            .End()
            .Build();

        root.ChangeState("empty");
        root.Update(1f);

        // No exception = success
    }

    [Fact]
    public void Builder_StateWithOnlyName_Works()
    {
        var root = new StateMachineBuilder()
            .State("justName")
            .End()
            .Build();

        root.ChangeState("justName");

        // No exception = success
    }

    [Fact]
    public void Builder_SiblingStatesWithChildren()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => sequence.Add("a:enter"))
                .State<TestState>("a1")
                    .Enter(_ => sequence.Add("a1:enter"))
                .End()
                .State<TestState>("a2")
                    .Enter(_ => sequence.Add("a2:enter"))
                .End()
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("b:enter"))
                .State<TestState>("b1")
                    .Enter(_ => sequence.Add("b1:enter"))
                .End()
            .End()
            .Build();

        root.ChangeState("a");
        var stateA = root;
        root.ChangeState("b");

        Assert.Equal(new[] { "a:enter", "b:enter" }, sequence);
    }

    [Fact]
    public void Builder_DeeplyNestedStates()
    {
        var depth = 0;

        var builder = new StateMachineBuilder()
            .State<TestState>("L1")
                .Enter(s =>
                {
                    depth = 1;
                    s.PushState("L2");
                })
                .State<TestState>("L2")
                    .Enter(s =>
                    {
                        depth = 2;
                        s.PushState("L3");
                    })
                    .State<TestState>("L3")
                        .Enter(s =>
                        {
                            depth = 3;
                            s.PushState("L4");
                        })
                        .State<TestState>("L4")
                            .Enter(s =>
                            {
                                depth = 4;
                                s.PushState("L5");
                            })
                            .State<TestState>("L5")
                                .Enter(_ => depth = 5)
                            .End()
                        .End()
                    .End()
                .End()
            .End()
            .Build();

        builder.ChangeState("L1");

        Assert.Equal(5, depth);
    }

}
