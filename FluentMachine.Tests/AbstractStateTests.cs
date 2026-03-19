using Moq;
using Xunit;

namespace FluentMachine.Tests;

public sealed class AbstractStateTests
{

    [Fact]
    public void AddChild_WithName_SetsParentCorrectly()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child, "child");

        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void AddChild_WithoutName_UsesTypeName()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child);

        parent.PushState(nameof(TestState));
        // No exception means it worked
    }

    [Fact]
    public void AddChild_DuplicateName_ThrowsApplicationException()
    {
        var parent = new TestState();
        var child1 = new TestState();
        var child2 = new TestState();

        parent.AddChild(child1, "same");

        Assert.Throws<ApplicationException>(() => parent.AddChild(child2, "same"));
    }

    [Fact]
    public void AddChild_EmptyName_AddsSuccessfully()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child, "");

        parent.PushState("");
        // No exception means it worked
    }

    [Fact]
    public void Enter_InvokesEnterAction()
    {
        var state = new TestState();
        var called = false;
        state.SetEnterAction(() => called = true);

        state.Enter();

        Assert.True(called);
    }

    [Fact]
    public void Enter_WithNoAction_DoesNotThrow()
    {
        var state = new TestState();

        var ex = Record.Exception(() => state.Enter());

        Assert.Null(ex);
    }

    [Fact]
    public void Exit_InvokesExitAction()
    {
        var state = new TestState();
        var called = false;
        state.SetExitAction(() => called = true);

        state.Exit();

        Assert.True(called);
    }

    [Fact]
    public void Exit_WithActiveChildren_ExitsAllChildren()
    {
        var parent = new TestState();
        var child1 = new Mock<IState>();
        var child2 = new Mock<IState>();

        parent.AddChild(child1.Object, "c1");
        parent.AddChild(child2.Object, "c2");
        parent.PushState("c1");
        parent.PushState("c2");

        parent.Exit();

        child1.Verify(s => s.Exit(), Times.Once());
        child2.Verify(s => s.Exit(), Times.Once());
    }

    [Fact]
    public void Exit_WithNoChildren_DoesNotThrow()
    {
        var state = new TestState();

        var ex = Record.Exception(() => state.Exit());

        Assert.Null(ex);
    }

    [Fact]
    public void Update_InvokesUpdateAction_WithDeltaTime()
    {
        var state = new TestState();
        var receivedDelta = -1f;
        state.SetUpdateAction(dt => receivedDelta = dt);

        state.Update(0.016f);

        Assert.Equal(0.016f, receivedDelta);
    }

    [Fact]
    public void Update_WithActiveChild_UpdatesOnlyChild()
    {
        var parent = new TestState();
        var child = new Mock<IState>();
        var parentUpdated = false;

        parent.SetUpdateAction(_ => parentUpdated = true);
        parent.AddChild(child.Object, "child");
        parent.PushState("child");

        parent.Update(1f);

        Assert.False(parentUpdated);
        child.Verify(s => s.Update(1f), Times.Once());
    }

    [Fact]
    public void Update_WithNoActiveChild_UpdatesSelf()
    {
        var state = new TestState();
        var updated = false;
        state.SetUpdateAction(_ => updated = true);

        state.Update(1f);

        Assert.True(updated);
    }

    [Fact]
    public void Update_ExecutesConditions()
    {
        var state = new TestState();
        var conditionExecuted = false;
        state.SetCondition(() => true, () => conditionExecuted = true);

        state.Update(1f);

        Assert.True(conditionExecuted);
    }

    [Fact]
    public void Update_ZeroDeltaTime_WorksCorrectly()
    {
        var state = new TestState();
        var receivedDelta = -1f;
        state.SetUpdateAction(dt => receivedDelta = dt);

        state.Update(0f);

        Assert.Equal(0f, receivedDelta);
    }

    [Fact]
    public void Update_NegativeDeltaTime_PassedThrough()
    {
        var state = new TestState();
        var receivedDelta = 0f;
        state.SetUpdateAction(dt => receivedDelta = dt);

        state.Update(-1f);

        Assert.Equal(-1f, receivedDelta);
    }

    [Fact]
    public void ChangeState_ToValidChild_EntersNewState()
    {
        var parent = new TestState();
        var child = new Mock<IState>();
        parent.AddChild(child.Object, "child");

        parent.ChangeState("child");

        child.Verify(s => s.Enter(), Times.Once());
    }

    [Fact]
    public void ChangeState_FromExistingState_ExitsPreviousState()
    {
        var parent = new TestState();
        var child1 = new Mock<IState>();
        var child2 = new Mock<IState>();
        parent.AddChild(child1.Object, "c1");
        parent.AddChild(child2.Object, "c2");

        parent.ChangeState("c1");
        parent.ChangeState("c2");

        child1.Verify(s => s.Exit(), Times.Once());
        child2.Verify(s => s.Enter(), Times.Once());
    }

    [Fact]
    public void ChangeState_ToNonExistent_ThrowsApplicationException()
    {
        var parent = new TestState();

        Assert.Throws<ApplicationException>(() => parent.ChangeState("nonexistent"));
    }

    [Fact]
    public void ChangeState_ToSameState_ExitsAndReenters()
    {
        var parent = new TestState();
        var enterCount = 0;
        var exitCount = 0;
        var child = new TestState();
        child.SetEnterAction(() => enterCount++);
        child.SetExitAction(() => exitCount++);
        parent.AddChild(child, "child");

        parent.ChangeState("child");
        parent.ChangeState("child");

        Assert.Equal(2, enterCount);
        Assert.Equal(1, exitCount);
    }

    [Fact]
    public void PushState_ValidChild_EntersState()
    {
        var parent = new TestState();
        var child = new Mock<IState>();
        parent.AddChild(child.Object, "child");

        parent.PushState("child");

        child.Verify(s => s.Enter(), Times.Once());
    }

    [Fact]
    public void PushState_NonExistent_ThrowsApplicationException()
    {
        var parent = new TestState();

        Assert.Throws<ApplicationException>(() => parent.PushState("nonexistent"));
    }

    [Fact]
    public void PushState_Multiple_BuildsStack()
    {
        var parent = new TestState();
        var child1 = new Mock<IState>();
        var child2 = new Mock<IState>();
        parent.AddChild(child1.Object, "c1");
        parent.AddChild(child2.Object, "c2");

        parent.PushState("c1");
        parent.PushState("c2");

        child1.Verify(s => s.Enter(), Times.Once());
        child2.Verify(s => s.Enter(), Times.Once());
    }

    [Fact]
    public void PopState_WithActiveChild_ExitsChild()
    {
        var parent = new TestState();
        var child = new Mock<IState>();
        parent.AddChild(child.Object, "child");
        parent.PushState("child");

        parent.PopState();

        child.Verify(s => s.Exit(), Times.Once());
    }

    [Fact]
    public void PopState_WithNoChildren_ThrowsApplicationException()
    {
        var parent = new TestState();

        Assert.Throws<ApplicationException>(() => parent.PopState());
    }

    [Fact]
    public void PopState_ReturnsToPreviousState()
    {
        var parent = new TestState();
        var child1 = new Mock<IState>();
        var child2 = new TestState();
        parent.AddChild(child1.Object, "c1");
        parent.AddChild(child2, "c2");

        parent.PushState("c1");
        parent.PushState("c2");
        parent.PopState();
        parent.Update(1f);

        child1.Verify(s => s.Update(1f), Times.Once());
    }

    [Fact]
    public void SetCondition_TruePredicate_ExecutesAction()
    {
        var state = new TestState();
        var executed = false;
        state.SetCondition(() => true, () => executed = true);

        state.Update(1f);

        Assert.True(executed);
    }

    [Fact]
    public void SetCondition_FalsePredicate_DoesNotExecuteAction()
    {
        var state = new TestState();
        var executed = false;
        state.SetCondition(() => false, () => executed = true);

        state.Update(1f);

        Assert.False(executed);
    }

    [Fact]
    public void SetCondition_MultipleConditions_AllEvaluated()
    {
        var state = new TestState();
        var count = 0;
        state.SetCondition(() => true, () => count++);
        state.SetCondition(() => true, () => count++);
        state.SetCondition(() => false, () => count++);

        state.Update(1f);

        Assert.Equal(2, count);
    }

    [Fact]
    public void SetCondition_PredicateChanges_RespondsCorrectly()
    {
        var state = new TestState();
        var shouldExecute = false;
        var count = 0;
        state.SetCondition(() => shouldExecute, () => count++);

        state.Update(1f);
        Assert.Equal(0, count);

        shouldExecute = true;
        state.Update(1f);
        Assert.Equal(1, count);
    }

    [Fact]
    public void SetEvent_TriggerEvent_ExecutesAction()
    {
        var state = new TestState();
        var triggered = false;
        state.SetEvent("test", _ => triggered = true);

        state.TriggerEvent("test");

        Assert.True(triggered);
    }

    [Fact]
    public void TriggerEvent_NonExistent_DoesNotThrow()
    {
        var state = new TestState();

        var ex = Record.Exception(() => state.TriggerEvent("nonexistent"));

        Assert.Null(ex);
    }

    [Fact]
    public void TriggerEvent_WithArgs_PassesArgs()
    {
        var state = new TestState();
        var receivedArgs = new TestEventArgs();
        state.SetEvent<TestEventArgs>("test", args => receivedArgs = args);
        var sentArgs = new TestEventArgs { TestString = "hello" };

        state.TriggerEvent("test", sentArgs);

        Assert.Equal("hello", receivedArgs.TestString);
    }

    [Fact]
    public void TriggerEvent_WithActiveChild_DelegatesToChild()
    {
        var parent = new TestState();
        var child = new Mock<IState>();
        parent.AddChild(child.Object, "child");
        parent.PushState("child");

        parent.TriggerEvent("test", EventArgs.Empty);

        child.Verify(s => s.TriggerEvent("test", EventArgs.Empty), Times.Once());
    }

    [Fact]
    public void TriggerEvent_WrongEventArgsType_ThrowsApplicationException()
    {
        var state = new TestState();
        state.SetEvent<OtherTestEventArgs>("test", _ => { });

        Assert.Throws<ApplicationException>(() => state.TriggerEvent("test", new TestEventArgs()));
    }

    [Fact]
    public void TriggerEvent_WithoutArgs_UsesEmptyArgs()
    {
        var state = new TestState();
        EventArgs? received = null;
        state.SetEvent("test", args => received = args);

        state.TriggerEvent("test");

        Assert.Equal(EventArgs.Empty, received);
    }

    [Fact]
    public void Parent_DefaultValue_IsStateEmpty()
    {
        var state = new TestState();

        Assert.Equal(State.Empty, state.Parent);
    }

    [Fact]
    public void Parent_AfterAddChild_IsSetCorrectly()
    {
        var parent = new TestState();
        var child = new TestState();

        parent.AddChild(child, "child");

        Assert.Equal(parent, child.Parent);
    }

}
