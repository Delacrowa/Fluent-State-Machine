using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for condition evaluation and state transitions.
/// </summary>
public sealed class ConditionTests
{

    [Fact]
    public void Condition_True_ExecutesAction()
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
    public void Condition_False_DoesNotExecute()
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
    public void Condition_EvaluatedEveryUpdate()
    {
        var count = 0;
        var trigger = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => trigger, _ => count++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        Assert.Equal(0, count);

        trigger = true;
        root.Update(1f);
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(3, count);
    }

    [Fact]
    public void MultipleConditions_AllEvaluated()
    {
        var results = new List<int>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => results.Add(1))
                .Condition(() => true, _ => results.Add(2))
                .Condition(() => true, _ => results.Add(3))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    [Fact]
    public void MultipleConditions_OnlyTrueOnesExecute()
    {
        var results = new List<int>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => results.Add(1))
                .Condition(() => false, _ => results.Add(2))
                .Condition(() => true, _ => results.Add(3))
                .Condition(() => false, _ => results.Add(4))
                .Condition(() => true, _ => results.Add(5))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { 1, 3, 5 }, results);
    }

    [Fact]
    public void MultipleConditions_ExecuteInOrder()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => sequence.Add("first"))
                .Condition(() => true, _ => sequence.Add("second"))
                .Condition(() => true, _ => sequence.Add("third"))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { "first", "second", "third" }, sequence);
    }

    [Fact]
    public void Condition_CanReadExternalState()
    {
        var health = 100;
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => health < 50, _ => count++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        Assert.Equal(0, count);

        health = 49;
        root.Update(1f);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Condition_CanModifyExternalState()
    {
        var counter = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => counter < 5, _ => counter++)
            .End()
            .Build();

        root.ChangeState("test");

        for (var i = 0; i < 10; i++)
        {
            root.Update(1f);
        }

        Assert.Equal(5, counter);
    }

    [Fact]
    public void Condition_CanTriggerChangeState()
    {
        var trigger = false;
        var bEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Condition(() => trigger, s => s.Parent.ChangeState("b"))
            .End()
            .State<TestState>("b")
                .Enter(_ => bEntered = true)
            .End()
            .Build();

        root.ChangeState("a");
        root.Update(1f);
        Assert.False(bEntered);

        trigger = true;
        root.Update(1f);
        Assert.True(bEntered);
    }

    [Fact]
    public void Condition_CanTriggerPushState()
    {
        var trigger = false;
        var childEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Condition(() => trigger, s => s.PushState("child"))
                .State<TestState>("child")
                    .Enter(_ => childEntered = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.Update(1f);
        Assert.False(childEntered);

        trigger = true;
        root.Update(1f);
        Assert.True(childEntered);
    }

    [Fact]
    public void Condition_CanTriggerPopState()
    {
        var trigger = false;
        var childExited = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .State<TestState>("child")
                    .Condition(() => trigger, s => s.Parent.PopState())
                    .Exit(_ => childExited = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        Assert.False(childExited);

        trigger = true;
        root.Update(1f);
        Assert.True(childExited);
    }

    [Fact]
    public void Condition_PredicateThrows_PropagatesException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => throw new InvalidOperationException("test"), _ => { })
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<InvalidOperationException>(() => root.Update(1f));
    }

    [Fact]
    public void Condition_ActionThrows_PropagatesException()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, _ => throw new InvalidOperationException("test"))
            .End()
            .Build();

        root.ChangeState("test");

        Assert.Throws<InvalidOperationException>(() => root.Update(1f));
    }

    [Fact]
    public void Condition_NotEvaluatedOnParent_WhenChildActive()
    {
        var parentCondition = false;
        var childCondition = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Condition(() => true, _ => parentCondition = true)
                .State<TestState>("child")
                    .Condition(() => true, _ => childCondition = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.Update(1f);

        Assert.False(parentCondition);
        Assert.True(childCondition);
    }

    [Fact]
    public void Condition_EvaluatedOnParent_AfterChildPop()
    {
        var parentCondition = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Condition(() => true, _ => parentCondition = true)
                .State<TestState>("child")
                    .Condition(() => true, s => s.Parent.PopState())
                .End()
            .End()
            .Build();

        root.ChangeState("parent");

        // First update: child pops
        root.Update(1f);
        Assert.False(parentCondition);

        // Second update: parent condition runs
        root.Update(1f);
        Assert.True(parentCondition);
    }

    [Fact]
    public void Condition_ExecutedAfterUpdate()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => sequence.Add("update"))
                .Condition(() => true, _ => sequence.Add("condition"))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { "update", "condition" }, sequence);
    }

    [Fact]
    public void Condition_ExecutedBeforeNextFrame()
    {
        var frame = 0;
        var conditionFrame = -1;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => frame == 2, _ => conditionFrame = frame)
            .End()
            .Build();

        root.ChangeState("test");

        frame = 1;
        root.Update(1f);

        frame = 2;
        root.Update(1f);

        Assert.Equal(2, conditionFrame);
    }

    [Fact]
    public void Condition_ChainedStateTransitions()
    {
        var aActive = true;
        var bActive = true;
        var cActive = true;
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => sequence.Add("a:enter"))
                .Condition(() => !aActive, s => s.Parent.ChangeState("b"))
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("b:enter"))
                .Condition(() => !bActive, s => s.Parent.ChangeState("c"))
            .End()
            .State<TestState>("c")
                .Enter(_ => sequence.Add("c:enter"))
                .Condition(() => !cActive, s => s.Parent.ChangeState("a"))
            .End()
            .Build();

        root.ChangeState("a");
        Assert.Equal(new[] { "a:enter" }, sequence);

        aActive = false;
        root.Update(1f);
        Assert.Equal(new[] { "a:enter", "b:enter" }, sequence);

        bActive = false;
        root.Update(1f);
        Assert.Equal(new[] { "a:enter", "b:enter", "c:enter" }, sequence);

        cActive = false;
        root.Update(1f);
        Assert.Equal(new[] { "a:enter", "b:enter", "c:enter", "a:enter" }, sequence);
    }

    [Fact]
    public void Condition_MultipleTransitions_OnlyFirstExecuted()
    {
        var aEntered = 0;
        var bEntered = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() => true, s => s.Parent.ChangeState("a"))
                .Condition(() => true, s => s.Parent.ChangeState("b"))
            .End()
            .State<TestState>("a")
                .Enter(_ => aEntered++)
            .End()
            .State<TestState>("b")
                .Enter(_ => bEntered++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        // Both conditions run, but second overrides first
        Assert.Equal(1, aEntered);
        Assert.Equal(1, bEntered);
    }

}
