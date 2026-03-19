using System.Diagnostics;
using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Stress tests to verify system stability under load.
/// </summary>
public sealed class StressTests
{

    [Fact]
    public void Stress_1000States_CreatesSuccessfully()
    {
        var builder = new StateMachineBuilder();

        for (var i = 0; i < 1000; i++)
        {
            builder.State($"state{i}").End();
        }

        var root = builder.Build();

        // Verify random access
        root.ChangeState("state0");
        root.ChangeState("state500");
        root.ChangeState("state999");
    }

    [Fact]
    public void Stress_1000Conditions_AllEvaluated()
    {
        var state = new TestState();
        var count = 0;

        for (var i = 0; i < 1000; i++)
        {
            state.SetCondition(() => true, () => count++);
        }

        state.Update(1f);

        Assert.Equal(1000, count);
    }

    [Fact]
    public void Stress_1000Events_AllRegistered()
    {
        var state = new TestState();
        var triggered = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            var eventName = $"event{i}";
            state.SetEvent(eventName, _ => triggered.Add(eventName));
        }

        for (var i = 0; i < 1000; i++)
        {
            state.TriggerEvent($"event{i}");
        }

        Assert.Equal(1000, triggered.Count);
    }

    [Fact]
    public void Stress_100000Updates_NoMemoryLeak()
    {
        var root = new StateMachineBuilder()
            .State("test")
                .Update((_, _) => { })
            .End()
            .Build();

        root.ChangeState("test");

        var initialMemory = GC.GetTotalMemory(true);

        for (var i = 0; i < 100000; i++)
        {
            root.Update(0.016f);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Allow some growth but not excessive (< 1MB)
        Assert.True(memoryGrowth < 1024 * 1024, $"Memory grew by {memoryGrowth} bytes");
    }

    [Fact]
    public void Stress_100000StateChanges_CompletesInTime()
    {
        var root = new StateMachineBuilder()
            .State("a").End()
            .State("b").End()
            .Build();

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100000; i++)
        {
            root.ChangeState(i % 2 == 0 ? "a" : "b");
        }

        sw.Stop();

        // Should complete in under 5 seconds
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Stress_100000Events_CompletesInTime()
    {
        var count = 0;
        var root = new StateMachineBuilder()
            .State("test")
                .Event("inc", _ => count++)
            .End()
            .Build();

        root.ChangeState("test");

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 100000; i++)
        {
            root.TriggerEvent("inc");
        }

        sw.Stop();

        Assert.Equal(100000, count);
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Stress_10LevelDeepHierarchy_WorksCorrectly()
    {
        var deepestEntered = false;

        // Build 10-level hierarchy manually
        var root = new StateMachineBuilder()
            .State("L0")
                .Enter(s => s.ChangeState("L1"))
                .State("L1")
                    .Enter(s => s.ChangeState("L2"))
                    .State("L2")
                        .Enter(s => s.ChangeState("L3"))
                        .State("L3")
                            .Enter(s => s.ChangeState("L4"))
                            .State("L4")
                                .Enter(s => s.ChangeState("L5"))
                                .State("L5")
                                    .Enter(s => s.ChangeState("L6"))
                                    .State("L6")
                                        .Enter(s => s.ChangeState("L7"))
                                        .State("L7")
                                            .Enter(s => s.ChangeState("L8"))
                                            .State("L8")
                                                .Enter(s => s.ChangeState("L9"))
                                                .State("L9")
                                                    .Enter(_ => deepestEntered = true)
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

        Assert.True(deepestEntered);
    }

    [Fact]
    public void Stress_DeepHierarchy_UpdateReachesBottom()
    {
        var bottomUpdated = false;

        var root = new StateMachineBuilder()
            .State("L0")
                .Enter(s => s.PushState("L1"))
                .State("L1")
                    .Enter(s => s.PushState("L2"))
                    .State("L2")
                        .Enter(s => s.PushState("L3"))
                        .State("L3")
                            .Enter(s => s.PushState("L4"))
                            .State("L4")
                                .Enter(s => s.PushState("L5"))
                                .State("L5")
                                    .Update((_, _) => bottomUpdated = true)
                                .End()
                            .End()
                        .End()
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("L0");
        root.Update(1f);

        Assert.True(bottomUpdated);
    }

    [Fact]
    public void Stress_RapidPushPop_MaintainsIntegrity()
    {
        var pushCount = 0;
        var popCount = 0;

        var root = new StateMachineBuilder()
            .State("base")
                .Enter(_ => pushCount++)
                .Exit(_ => popCount++)
                .Event("push", s => s.PushState("overlay"))
                .State("overlay")
                    .Enter(_ => pushCount++)
                    .Exit(_ => popCount++)
                    .Event("pop", s => s.Parent.PopState())
                .End()
            .End()
            .Build();

        root.ChangeState("base");

        for (var i = 0; i < 1000; i++)
        {
            root.TriggerEvent("push");
            root.TriggerEvent("pop");
        }

        // base entered once, overlay entered/exited 1000 times
        Assert.Equal(1001, pushCount);
        Assert.Equal(1000, popCount);
    }

    [Fact]
    public void Stress_AlternatingStates_NoStackOverflow()
    {
        var transitions = 0;

        var root = new StateMachineBuilder()
            .State("A")
                .Enter(_ => transitions++)
            .End()
            .State("B")
                .Enter(_ => transitions++)
            .End()
            .Build();

        for (var i = 0; i < 10000; i++)
        {
            root.ChangeState(i % 2 == 0 ? "A" : "B");
        }

        Assert.Equal(10000, transitions);
    }

    [Fact]
    public void Stress_ChainedConditionTransitions_CompleteCorrectly()
    {
        var statesVisited = new List<string>();
        var step = 0;

        var root = new StateMachineBuilder()
            .State("S0")
                .Enter(_ => statesVisited.Add("S0"))
                .Condition(() => step >= 1, s => s.Parent.ChangeState("S1"))
            .End()
            .State("S1")
                .Enter(_ => statesVisited.Add("S1"))
                .Condition(() => step >= 2, s => s.Parent.ChangeState("S2"))
            .End()
            .State("S2")
                .Enter(_ => statesVisited.Add("S2"))
                .Condition(() => step >= 3, s => s.Parent.ChangeState("S3"))
            .End()
            .State("S3")
                .Enter(_ => statesVisited.Add("S3"))
            .End()
            .Build();

        root.ChangeState("S0");
        Assert.Single(statesVisited);

        step = 1;
        root.Update(1f);
        Assert.Equal(2, statesVisited.Count);

        step = 2;
        root.Update(1f);
        Assert.Equal(3, statesVisited.Count);

        step = 3;
        root.Update(1f);
        Assert.Equal(4, statesVisited.Count);

        Assert.Equal(new[] { "S0", "S1", "S2", "S3" }, statesVisited);
    }

}
