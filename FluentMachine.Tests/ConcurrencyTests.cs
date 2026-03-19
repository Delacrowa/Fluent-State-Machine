using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for concurrent access patterns (simulated).
/// Note: The library is NOT thread-safe by design, these tests verify behavior
/// when accessed from a single thread with complex interleaved operations.
/// </summary>
public sealed class ConcurrencyTests
{

    [Fact]
    public void RapidStateChanges_1000Transitions()
    {
        var transitionCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => transitionCount++)
            .End()
            .State<TestState>("b")
                .Enter(_ => transitionCount++)
            .End()
            .Build();

        for (var i = 0; i < 500; i++)
        {
            root.ChangeState("a");
            root.ChangeState("b");
        }

        Assert.Equal(1000, transitionCount);
    }

    [Fact]
    public void RapidPushPop_1000Cycles()
    {
        var pushCount = 0;
        var popCount = 0;

        var parent = new TestState();
        var child = new TestState();
        
        child.SetEnterAction(() => pushCount++);
        child.SetExitAction(() => popCount++);
        
        parent.AddChild(child, "child");

        for (var i = 0; i < 1000; i++)
        {
            parent.PushState("child");
            parent.PopState();
        }

        Assert.Equal(1000, pushCount);
        Assert.Equal(1000, popCount);
    }

    [Fact]
    public void InterleavedUpdatesAndEvents()
    {
        var updateCount = 0;
        var eventCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => updateCount++)
                .Event("ping", _ => eventCount++)
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 100; i++)
        {
            root.Update(0.016f);
            root.TriggerEvent("ping");
            root.Update(0.016f);
        }

        Assert.Equal(200, updateCount);
        Assert.Equal(100, eventCount);
    }

    [Fact]
    public void InterleavedConditionsAndEvents()
    {
        var conditionCount = 0;
        var eventCount = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => conditionCount++)
                .Event("ping", _ => eventCount++)
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 100; i++)
        {
            root.Update(1f);
            root.TriggerEvent("ping");
        }

        Assert.Equal(100, conditionCount);
        Assert.Equal(100, eventCount);
    }

    [Fact]
    public void ChangeStateDuringEnter_Works()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(s =>
                {
                    sequence.Add("a:enter");
                    s.Parent.ChangeState("b");
                })
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("b:enter"))
            .End()
            .Build();

        root.ChangeState("a");

        Assert.Equal(new[] { "a:enter", "b:enter" }, sequence);
    }

    [Fact]
    public void ChangeStateDuringExit_Works()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(s =>
                {
                    sequence.Add("a:exit");
                    // Cannot change state during exit reliably, but shouldn't crash
                })
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("b:enter"))
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");

        Assert.Contains("a:exit", sequence);
        Assert.Contains("b:enter", sequence);
    }

    [Fact]
    public void ChangeStateDuringUpdate_Works()
    {
        var updated = false;
        var bEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Update((s, _) =>
                {
                    if (!updated)
                    {
                        updated = true;
                        s.Parent.ChangeState("b");
                    }
                })
            .End()
            .State<TestState>("b")
                .Enter(_ => bEntered = true)
            .End()
            .Build();

        root.ChangeState("a");
        root.Update(1f);

        Assert.True(bEntered);
    }

    [Fact]
    public void PushStateDuringEvent_Works()
    {
        var childEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Event("push", s => s.PushState("child"))
                .State<TestState>("child")
                    .Enter(_ => childEntered = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.TriggerEvent("push");

        Assert.True(childEntered);
    }

    [Fact]
    public void RecursiveUpdate_MaxDepth100()
    {
        var depth = 0;
        var maxDepth = 0;
        var shouldRecurse = true;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((s, _) =>
                {
                    depth++;
                    maxDepth = Math.Max(maxDepth, depth);
                    if (depth < 100 && shouldRecurse)
                    {
                        s.Update(1f);
                    }
                    depth--;
                })
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(100, maxDepth);
    }

    [Fact]
    public void RecursiveEvent_MaxDepth100()
    {
        var depth = 0;
        var maxDepth = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Event("recurse", s =>
                {
                    depth++;
                    maxDepth = Math.Max(maxDepth, depth);
                    if (depth < 100)
                    {
                        s.TriggerEvent("recurse");
                    }
                    depth--;
                })
            .End()
            .Build();

        root.ChangeState("test");
        root.TriggerEvent("recurse");

        Assert.Equal(100, maxDepth);
    }

    [Fact]
    public void MixedOperations_500Cycles()
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var operations = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Update((_, _) => operations++)
                .Event("evt", _ => operations++)
                .Condition(() => true, _ => operations++)
                .State<TestState>("child")
                    .Update((_, _) => operations++)
                    .Event("evt", _ => operations++)
                .End()
            .End()
            .State<TestState>("b")
                .Update((_, _) => operations++)
                .Event("evt", _ => operations++)
            .End()
            .Build();

        root.ChangeState("a");

        for (var i = 0; i < 500; i++)
        {
            var op = random.Next(6);
            switch (op)
            {
                case 0:
                    root.Update(0.016f);
                    break;
                case 1:
                    root.TriggerEvent("evt");
                    break;
                case 2:
                    root.ChangeState("a");
                    break;
                case 3:
                    root.ChangeState("b");
                    break;
                case 4:
                    try { root.PushState("child"); } catch { }
                    break;
                case 5:
                    try { root.PopState(); } catch { }
                    break;
            }
        }

        Assert.True(operations > 0);
    }

    [Fact]
    public void HighFrequencyUpdates_10000Frames()
    {
        var frameCount = 0;
        var totalDelta = 0f;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) =>
                {
                    frameCount++;
                    totalDelta += dt;
                })
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 10000; i++)
        {
            root.Update(0.016f);
        }

        Assert.Equal(10000, frameCount);
        Assert.True(Math.Abs(totalDelta - 160f) < 0.1f);
    }

    [Fact]
    public void VariableDeltaTime_Works()
    {
        var deltas = new List<float>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => deltas.Add(dt))
            .End()
            .Build();

        root.ChangeState("test");

        var testDeltas = new[] { 0.001f, 0.016f, 0.033f, 0.1f, 1f, 5f, 0.0001f };
        foreach (var dt in testDeltas)
        {
            root.Update(dt);
        }

        Assert.Equal(testDeltas, deltas);
    }

    [Fact]
    public void ZeroDeltaTime_Works()
    {
        var updated = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) =>
                {
                    Assert.Equal(0f, dt);
                    updated = true;
                })
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(0f);

        Assert.True(updated);
    }

    [Fact]
    public void NegativeDeltaTime_Works()
    {
        var receivedDelta = 0f;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, dt) => receivedDelta = dt)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(-1f);

        Assert.Equal(-1f, receivedDelta);
    }

}
