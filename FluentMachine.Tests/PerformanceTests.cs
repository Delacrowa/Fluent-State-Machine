using System.Diagnostics;
using Xunit;

namespace FluentMachine.Tests;

public sealed class PerformanceTests
{
    [Fact]
    public void Update_10000Iterations_CompletesQuickly()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => { })
            .End()
            .Build();

        root.ChangeState("test");

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            root.Update(0.016f);
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void StateChange_10000Iterations_CompletesQuickly()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("a").End()
            .State<TestState>("b").End()
            .Build();

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            root.ChangeState("a");
            root.ChangeState("b");
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void TriggerEvent_10000Iterations_CompletesQuickly()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("myevent", _ => { })
            .End()
            .Build();

        root.ChangeState("test");

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            root.TriggerEvent("myevent");
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PushPopState_10000Iterations_CompletesQuickly()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .State<TestState>("child").End()
            .End()
            .Build();

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            root.ChangeState("parent");
            root.PopState();
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Conditions_1000Conditions_EvaluatedQuickly()
    {
        var state = new TestState();
        var count = 0;

        for (var i = 0; i < 1000; i++)
        {
            state.SetCondition(() => true, () => count++);
        }

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100; i++)
        {
            state.Update(1f);
        }

        sw.Stop();

        Assert.Equal(100000, count);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void DeepHierarchy_UpdatePerformance()
    {
        // Build 5-level deep hierarchy with auto-navigation
        var root = new StateMachineBuilder()
            .State("l0")
                .Enter(s => s.PushState("l1"))
                .State("l1")
                    .Enter(s => s.PushState("l2"))
                    .State("l2")
                        .Enter(s => s.PushState("l3"))
                        .State("l3")
                            .Enter(s => s.PushState("l4"))
                            .State("l4")
                            .End()
                        .End()
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("l0");

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            root.Update(0.016f);
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Took {sw.ElapsedMilliseconds}ms");
    }
}
