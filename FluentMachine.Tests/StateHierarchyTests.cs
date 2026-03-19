using Xunit;

namespace FluentMachine.Tests;

/// <summary>
/// Tests for state hierarchy management.
/// </summary>
public sealed class StateHierarchyTests
{

    [Fact]
    public void AddChild_SetsParentCorrectly()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child, "child");

        Assert.Same(parent, child.Parent);
    }

    [Fact]
    public void AddChild_WithoutName_UsesTypeName()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child);

        parent.PushState(nameof(TestState));
        // No exception = success
    }

    [Fact]
    public void NewState_HasEmptyParent()
    {
        var state = new TestState();

        Assert.Same(State.Empty, state.Parent);
    }

    [Fact]
    public void MultipleChildren_AllHaveSameParent()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();
        var child3 = new TestState();

        parent.AddChild(child1, "c1");
        parent.AddChild(child2, "c2");
        parent.AddChild(child3, "c3");

        Assert.Same(parent, child1.Parent);
        Assert.Same(parent, child2.Parent);
        Assert.Same(parent, child3.Parent);
    }

    [Fact]
    public void PushState_AddsToStack()
    {
        var parent = new TestState();
        var child = new TestState();
        var entered = false;

        child.SetEnterAction(() => entered = true);
        parent.AddChild(child, "child");

        parent.PushState("child");

        Assert.True(entered);
    }

    [Fact]
    public void PushState_Multiple_BuildsStack()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();

        var sequence = new List<string>();
        child1.SetEnterAction(() => sequence.Add("c1:enter"));
        child2.SetEnterAction(() => sequence.Add("c2:enter"));

        parent.AddChild(child1, "c1");
        parent.AddChild(child2, "c2");

        parent.PushState("c1");
        parent.PushState("c2");

        Assert.Equal(new[] { "c1:enter", "c2:enter" }, sequence);
    }

    [Fact]
    public void PopState_RemovesFromStack()
    {
        var parent = new TestState();
        var child = new TestState();
        var exited = false;

        child.SetExitAction(() => exited = true);
        parent.AddChild(child, "child");

        parent.PushState("child");
        parent.PopState();

        Assert.True(exited);
    }

    [Fact]
    public void PopState_ReturnsToPrevious()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();

        var updateTarget = "";
        child1.SetUpdateAction(_ => updateTarget = "c1");
        child2.SetUpdateAction(_ => updateTarget = "c2");

        parent.AddChild(child1, "c1");
        parent.AddChild(child2, "c2");

        parent.PushState("c1");
        parent.PushState("c2");

        parent.Update(1f);
        Assert.Equal("c2", updateTarget);

        parent.PopState();
        parent.Update(1f);
        Assert.Equal("c1", updateTarget);
    }

    [Fact]
    public void PopState_OnEmptyStack_Throws()
    {
        var state = new TestState();

        Assert.Throws<ApplicationException>(() => state.PopState());
    }

    [Fact]
    public void ChangeState_ExitsPrevious_EntersNew()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();

        var sequence = new List<string>();
        child1.SetEnterAction(() => sequence.Add("c1:enter"));
        child1.SetExitAction(() => sequence.Add("c1:exit"));
        child2.SetEnterAction(() => sequence.Add("c2:enter"));
        child2.SetExitAction(() => sequence.Add("c2:exit"));

        parent.AddChild(child1, "c1");
        parent.AddChild(child2, "c2");

        parent.ChangeState("c1");
        parent.ChangeState("c2");

        Assert.Equal(new[] { "c1:enter", "c1:exit", "c2:enter" }, sequence);
    }

    [Fact]
    public void ChangeState_ToNonExistent_Throws()
    {
        var state = new TestState();

        var ex = Assert.Throws<ApplicationException>(() => state.ChangeState("nonexistent"));

        Assert.Contains("not in the list", ex.Message);
    }

    [Fact]
    public void ChangeState_ToSameState_ExitsAndReenters()
    {
        var parent = new TestState();
        var child = new TestState();

        var enterCount = 0;
        var exitCount = 0;
        child.SetEnterAction(() => enterCount++);
        child.SetExitAction(() => exitCount++);

        parent.AddChild(child, "child");

        parent.ChangeState("child");
        parent.ChangeState("child");
        parent.ChangeState("child");

        Assert.Equal(3, enterCount);
        Assert.Equal(2, exitCount);
    }

    [Fact]
    public void Exit_CascadesToAllActiveChildren()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();

        var exits = new List<string>();
        parent.SetExitAction(() => exits.Add("parent"));
        child1.SetExitAction(() => exits.Add("c1"));
        child2.SetExitAction(() => exits.Add("c2"));

        parent.AddChild(child1, "c1");
        parent.AddChild(child2, "c2");

        parent.PushState("c1");
        parent.PushState("c2");

        parent.Exit();

        Assert.Contains("parent", exits);
        Assert.Contains("c1", exits);
        Assert.Contains("c2", exits);
    }

    [Fact]
    public void Exit_WithNoChildren_OnlyExitsSelf()
    {
        var state = new TestState();
        var exited = false;

        state.SetExitAction(() => exited = true);
        state.Exit();

        Assert.True(exited);
    }

    [Fact]
    public void Update_OnlyUpdatesDeepestActive()
    {
        var parent = new TestState();
        var child = new TestState();

        var parentUpdated = false;
        var childUpdated = false;
        parent.SetUpdateAction(_ => parentUpdated = true);
        child.SetUpdateAction(_ => childUpdated = true);

        parent.AddChild(child, "child");
        parent.PushState("child");

        parent.Update(1f);

        Assert.False(parentUpdated);
        Assert.True(childUpdated);
    }

    [Fact]
    public void Update_AfterPop_UpdatesPreviousState()
    {
        var parent = new TestState();
        var child = new TestState();

        var parentUpdated = false;
        parent.SetUpdateAction(_ => parentUpdated = true);

        parent.AddChild(child, "child");
        parent.PushState("child");
        parent.PopState();

        parentUpdated = false;
        parent.Update(1f);

        Assert.True(parentUpdated);
    }

    [Fact]
    public void TriggerEvent_PropagatesTo_DeepestActiveChild()
    {
        var parent = new TestState();
        var child = new TestState();

        var parentReceived = false;
        var childReceived = false;
        parent.SetEvent("test", _ => parentReceived = true);
        child.SetEvent("test", _ => childReceived = true);

        parent.AddChild(child, "child");
        parent.PushState("child");

        parent.TriggerEvent("test");

        Assert.False(parentReceived);
        Assert.True(childReceived);
    }

    [Fact]
    public void TriggerEvent_AfterPop_GoesToParent()
    {
        var parent = new TestState();
        var child = new TestState();

        var parentReceived = false;
        parent.SetEvent("test", _ => parentReceived = true);

        parent.AddChild(child, "child");
        parent.PushState("child");
        parent.PopState();

        parent.TriggerEvent("test");

        Assert.True(parentReceived);
    }

    [Fact]
    public void DeepHierarchy_5Levels_WorksCorrectly()
    {
        var levels = new TestState[5];
        var enterSequence = new List<int>();

        for (var i = 0; i < 5; i++)
        {
            var level = i;
            levels[i] = new TestState();
            levels[i].SetEnterAction(() => enterSequence.Add(level));
        }

        // Build hierarchy: 0 -> 1 -> 2 -> 3 -> 4
        for (var i = 0; i < 4; i++)
        {
            levels[i].AddChild(levels[i + 1], $"L{i + 1}");
        }

        // Navigate down
        levels[0].Enter();
        levels[0].ChangeState("L1");
        levels[1].ChangeState("L2");
        levels[2].ChangeState("L3");
        levels[3].ChangeState("L4");

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, enterSequence);
    }

    [Fact]
    public void DeepHierarchy_UpdateReachesBottom()
    {
        var levels = new TestState[5];
        var updateLevel = -1;

        for (var i = 0; i < 5; i++)
        {
            var level = i;
            levels[i] = new TestState();
            levels[i].SetUpdateAction(_ => updateLevel = level);
        }

        for (var i = 0; i < 4; i++)
        {
            levels[i].AddChild(levels[i + 1], $"L{i + 1}");
        }

        levels[0].PushState("L1");
        levels[1].PushState("L2");
        levels[2].PushState("L3");
        levels[3].PushState("L4");

        levels[0].Update(1f);

        Assert.Equal(4, updateLevel);
    }

}
