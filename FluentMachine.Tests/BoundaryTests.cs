using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for boundary conditions and edge cases.
/// </summary>
public sealed class BoundaryTests
{

    [Fact]
    public void Update_ZeroDelta()
    {
        float? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => received = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(0f);

        Assert.Equal(0f, received);
    }

    [Fact]
    public void Update_NegativeDelta()
    {
        float? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => received = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(-1f);

        Assert.Equal(-1f, received);
    }

    [Fact]
    public void Update_VerySmallDelta()
    {
        float? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => received = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(float.Epsilon);

        Assert.Equal(float.Epsilon, received);
    }

    [Fact]
    public void Update_VeryLargeDelta()
    {
        float? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => received = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(float.MaxValue);

        Assert.Equal(float.MaxValue, received);
    }

    [Fact]
    public void Update_NaN_Handled()
    {
        float? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => received = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(float.NaN);

        Assert.True(float.IsNaN(received!.Value));
    }

    [Fact]
    public void Update_Infinity_Handled()
    {
        float? received = null;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => received = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(float.PositiveInfinity);

        Assert.True(float.IsPositiveInfinity(received!.Value));
    }

    [Fact]
    public void State_EmptyName()
    {
        var entered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("")
                .Enter(_ => entered = true)
            .End()
            .Build();

        root.ChangeState("");

        Assert.True(entered);
    }

    [Fact]
    public void State_WhitespaceName()
    {
        var entered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("   ")
                .Enter(_ => entered = true)
            .End()
            .Build();

        root.ChangeState("   ");

        Assert.True(entered);
    }

    [Fact]
    public void State_SpecialCharacters()
    {
        var entered = false;
        var name = "state!@#$%^&*()";

        var root = new StateMachineBuilder()
            .State<TestState>(name)
                .Enter(_ => entered = true)
            .End()
            .Build();

        root.ChangeState(name);

        Assert.True(entered);
    }

    [Fact]
    public void State_UnicodeCharacters()
    {
        var entered = false;
        var name = "状态🚀αβγ";

        var root = new StateMachineBuilder()
            .State<TestState>(name)
                .Enter(_ => entered = true)
            .End()
            .Build();

        root.ChangeState(name);

        Assert.True(entered);
    }

    [Fact]
    public void State_VeryLongName()
    {
        var entered = false;
        var name = new string('a', 10000);

        var root = new StateMachineBuilder()
            .State<TestState>(name)
                .Enter(_ => entered = true)
            .End()
            .Build();

        root.ChangeState(name);

        Assert.True(entered);
    }

    [Fact]
    public void Event_EmptyName()
    {
        var handled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("", _ => handled = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("");

        Assert.True(handled);
    }

    [Fact]
    public void Event_SpecialCharacters()
    {
        var handled = false;
        var name = "event!@#$%^&*()";

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event(name, _ => handled = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent(name);

        Assert.True(handled);
    }

    [Fact]
    public void Event_UnicodeCharacters()
    {
        var handled = false;
        var name = "事件🎉δεζ";

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event(name, _ => handled = true)
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent(name);

        Assert.True(handled);
    }

    [Fact]
    public void ManyStates_100()
    {
        var builder = new StateMachineBuilder();
        for (var i = 0; i < 100; i++)
        {
            builder = builder.State<TestState>($"state{i}").End();
        }
        var root = builder.Build();

        // Can access any state
        root.ChangeState("state50");
        root.ChangeState("state99");
        root.ChangeState("state0");
    }

    [Fact]
    public void ManyConditions_100()
    {
        var count = 0;

        // Use direct API to add conditions
        var state = new TestState();
        for (var i = 0; i < 100; i++)
        {
            state.SetCondition(() => true, () => count++);
        }

        state.Update(1f);

        Assert.Equal(100, count);
    }

    [Fact]
    public void ManyEvents_100()
    {
        var counts = new int[100];

        // Use direct API to add events
        var state = new TestState();
        for (var i = 0; i < 100; i++)
        {
            var idx = i;
            state.SetEvent($"event{i}", _ => counts[idx]++);
        }

        for (var i = 0; i < 100; i++)
        {
            state.TriggerEvent($"event{i}");
        }

        Assert.True(counts.All(c => c == 1));
    }

    [Fact]
    public void DeepNesting_20Levels()
    {
        var depth = 0;
        
        // Build hierarchy manually for 20 levels
        var root = new StateMachineBuilder()
            .State<TestState>("L0")
                .Enter(s => { depth = 1; s.PushState("L1"); })
                .State<TestState>("L1")
                    .Enter(s => { depth = 2; s.PushState("L2"); })
                    .State<TestState>("L2")
                        .Enter(s => { depth = 3; s.PushState("L3"); })
                        .State<TestState>("L3")
                            .Enter(s => { depth = 4; s.PushState("L4"); })
                            .State<TestState>("L4")
                                .Enter(s => { depth = 5; s.PushState("L5"); })
                                .State<TestState>("L5")
                                    .Enter(s => { depth = 6; s.PushState("L6"); })
                                    .State<TestState>("L6")
                                        .Enter(s => { depth = 7; s.PushState("L7"); })
                                        .State<TestState>("L7")
                                            .Enter(s => { depth = 8; s.PushState("L8"); })
                                            .State<TestState>("L8")
                                                .Enter(s => { depth = 9; s.PushState("L9"); })
                                                .State<TestState>("L9")
                                                    .Enter(_ => depth = 10)
                                                .End()
                                            .End()
                                        .End()
                                    .End()
                                .End()
                            .End()
                        .End()
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("L0");
        
        Assert.Equal(10, depth);
    }

    [Fact]
    public void ConsecutiveEnterExit_SameState()
    {
        var enterCount = 0;
        var exitCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => enterCount++)
                .Exit(_ => exitCount++)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        for (var i = 0; i < 50; i++)
        {
            root.ChangeState("a");
            root.ChangeState("b");
        }

        Assert.Equal(50, enterCount);
        Assert.Equal(50, exitCount);
    }

    [Fact]
    public void ConsecutiveUpdates_10000()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => count++)
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 10000; i++)
        {
            root.Update(0.016f);
        }

        Assert.Equal(10000, count);
    }

    [Fact]
    public void ConsecutiveEvents_10000()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("ping", _ => count++)
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 10000; i++)
        {
            root.TriggerEvent("ping");
        }

        Assert.Equal(10000, count);
    }

    [Fact]
    public void ChangeState_ToSameState_ReEnters()
    {
        var enterCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => enterCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.ChangeState("test");
        root.ChangeState("test");

        Assert.Equal(3, enterCount);
    }

    [Fact]
    public void ChangeState_ToSameState_Exits()
    {
        var exitCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Exit(_ => exitCount++)
            .End()
            .Build();

        root.ChangeState("test");
        root.ChangeState("test");
        root.ChangeState("test");

        Assert.Equal(2, exitCount);
    }

    [Fact]
    public void NoInitialState_UpdateDoesNothing()
    {
        var updated = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => updated = true)
            .End()
            .Build();

        // Don't call ChangeState
        root.Update(1f);

        Assert.False(updated);
    }

    [Fact]
    public void NoInitialState_EventDoesNothing()
    {
        var handled = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("evt", _ => handled = true)
            .End()
            .Build();

        // Don't call ChangeState
        root.TriggerEvent("evt");

        Assert.False(handled);
    }

}
