using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for state lifecycle (enter, update, exit) ordering and behavior.
/// </summary>
public sealed class StateLifecycleTests
{

    [Fact]
    public void Enter_CalledOnFirstChange()
    {
        var entered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => entered = true)
            .End()
            .Build();

        Assert.False(entered);
        root.ChangeState("test");
        Assert.True(entered);
    }

    [Fact]
    public void Enter_CalledBeforeUpdate()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => sequence.Add("enter"))
                .Update((_, _) => sequence.Add("update"))
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { "enter", "update" }, sequence);
    }

    [Fact]
    public void Enter_CanAccessStateMembers()
    {
        IState? capturedParent = null;

        var root = new StateMachineBuilder()
            .State<TestState>("myState")
                .Enter(s =>
                {
                    capturedParent = s.Parent;
                })
            .End()
            .Build();

        root.ChangeState("myState");

        Assert.NotNull(capturedParent);
    }

    [Fact]
    public void Exit_CalledOnStateChange()
    {
        var exited = false;

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Exit(_ => exited = true)
            .End()
            .State<TestState>("b")
            .End()
            .Build();

        root.ChangeState("a");
        Assert.False(exited);

        root.ChangeState("b");
        Assert.True(exited);
    }

    [Fact]
    public void Exit_CalledBeforeNewEnter()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => sequence.Add("a:enter"))
                .Exit(_ => sequence.Add("a:exit"))
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("b:enter"))
                .Exit(_ => sequence.Add("b:exit"))
            .End()
            .Build();

        root.ChangeState("a");
        root.ChangeState("b");

        Assert.Equal(new[] { "a:enter", "a:exit", "b:enter" }, sequence);
    }

    [Fact]
    public void Exit_NotCalledOnSameStateReentry_ButCalledFirst()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => sequence.Add("enter"))
                .Exit(_ => sequence.Add("exit"))
            .End()
            .Build();

        root.ChangeState("test");
        root.ChangeState("test");

        Assert.Equal(new[] { "enter", "exit", "enter" }, sequence);
    }

    [Fact]
    public void Exit_CascadesToChildren()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Exit(_ => sequence.Add("parent:exit"))
                .State<TestState>("child")
                    .Exit(_ => sequence.Add("child:exit"))
                .End()
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("parent");
        root.ChangeState("other");

        Assert.Contains("parent:exit", sequence);
        Assert.Contains("child:exit", sequence);
    }

    [Fact]
    public void Update_NotCalledBeforeEnter()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Enter(_ => sequence.Add("enter"))
                .Update((_, _) => sequence.Add("update"))
            .End()
            .Build();

        root.Update(1f); // Before ChangeState
        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { "enter", "update" }, sequence);
    }

    [Fact]
    public void Update_CalledEveryFrame()
    {
        var count = 0;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => count++)
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);
        root.Update(1f);
        root.Update(1f);

        Assert.Equal(3, count);
    }

    [Fact]
    public void Update_DelegatedToActiveChild()
    {
        var parentUpdated = false;
        var childUpdated = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .Update((_, _) => parentUpdated = true)
                .State<TestState>("child")
                    .Update((_, _) => childUpdated = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");
        root.Update(1f);

        Assert.False(parentUpdated);
        Assert.True(childUpdated);
    }

    [Fact]
    public void Update_AfterPop_GoesToParent()
    {
        var parentUpdated = false;

        var parent = new TestState();
        var child = new TestState();
        
        parent.SetUpdateAction(_ => parentUpdated = true);
        parent.AddChild(child, "child");
        
        parent.PushState("child");
        parent.PopState();
        parent.Update(1f);

        Assert.True(parentUpdated);
    }

    [Fact]
    public void Condition_EvaluatedDuringUpdate()
    {
        var evaluated = false;

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Condition(() =>
                {
                    evaluated = true;
                    return false;
                }, _ => { })
            .End()
            .Build();

        root.ChangeState("test");
        Assert.False(evaluated);

        root.Update(1f);
        Assert.True(evaluated);
    }

    [Fact]
    public void Condition_AfterUpdate()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("test")
                .Update((_, _) => sequence.Add("update"))
                .Condition(() => { sequence.Add("condition"); return false; }, _ => { })
            .End()
            .Build();

        root.ChangeState("test");
        root.Update(1f);

        Assert.Equal(new[] { "update", "condition" }, sequence);
    }

    [Fact]
    public void FullLifecycle_SingleState()
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
    public void FullLifecycle_ParentChild()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s =>
                {
                    sequence.Add("parent:enter");
                    s.PushState("child");
                })
                .Update((_, _) => sequence.Add("parent:update"))
                .Exit(_ => sequence.Add("parent:exit"))
                .State<TestState>("child")
                    .Enter(_ => sequence.Add("child:enter"))
                    .Update((_, _) => sequence.Add("child:update"))
                    .Exit(_ => sequence.Add("child:exit"))
                .End()
            .End()
            .State<TestState>("other")
            .End()
            .Build();

        root.ChangeState("parent");
        root.Update(1f);
        root.ChangeState("other");

        // Parent exits first, then cascades to children
        Assert.Equal(new[]
        {
            "parent:enter",
            "child:enter",
            "child:update",
            "parent:exit",
            "child:exit"
        }, sequence);
    }

    [Fact]
    public void StateChange_DuringEnter_Allowed()
    {
        var sequence = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(s =>
                {
                    sequence.Add("a:enter");
                    s.Parent.ChangeState("b");
                })
                .Exit(_ => sequence.Add("a:exit"))
            .End()
            .State<TestState>("b")
                .Enter(_ => sequence.Add("b:enter"))
            .End()
            .Build();

        root.ChangeState("a");

        Assert.Equal(new[] { "a:enter", "a:exit", "b:enter" }, sequence);
    }

    [Fact]
    public void PushState_DuringEnter_Allowed()
    {
        var childEntered = false;

        var root = new StateMachineBuilder()
            .State<TestState>("parent")
                .Enter(s => s.PushState("child"))
                .State<TestState>("child")
                    .Enter(_ => childEntered = true)
                .End()
            .End()
            .Build();

        root.ChangeState("parent");

        Assert.True(childEntered);
    }

    [Fact]
    public void MultipleStates_IndependentLifecycles()
    {
        var aLifecycle = new List<string>();
        var bLifecycle = new List<string>();

        var root = new StateMachineBuilder()
            .State<TestState>("a")
                .Enter(_ => aLifecycle.Add("enter"))
                .Update((_, _) => aLifecycle.Add("update"))
                .Exit(_ => aLifecycle.Add("exit"))
            .End()
            .State<TestState>("b")
                .Enter(_ => bLifecycle.Add("enter"))
                .Update((_, _) => bLifecycle.Add("update"))
                .Exit(_ => bLifecycle.Add("exit"))
            .End()
            .Build();

        root.ChangeState("a");
        root.Update(1f);
        root.ChangeState("b");
        root.Update(1f);
        root.ChangeState("a");

        Assert.Equal(new[] { "enter", "update", "exit", "enter" }, aLifecycle);
        Assert.Equal(new[] { "enter", "update", "exit" }, bLifecycle);
    }

}
