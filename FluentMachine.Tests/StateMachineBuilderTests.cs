using Xunit;

namespace FluentMachine.Tests;

public sealed class StateMachineBuilderTests
{

    [Fact]
    public void Build_ReturnsRootState()
    {
        var builder = new StateMachineBuilder();

        var root = builder.Build();

        Assert.NotNull(root);
        Assert.IsType<State>(root);
    }

    [Fact]
    public void Build_WithStates_StatesAreChildrenOfRoot()
    {
        IState? capturedParent = null;

        var root = new StateMachineBuilder()
            .State<TestState>()
                .Enter(state => capturedParent = state.Parent)
            .End()
            .Build();

        root.ChangeState(nameof(TestState));

        Assert.Equal(root, capturedParent);
    }

    [Fact]
    public void Build_MultipleTimes_ReturnsSameRoot()
    {
        var builder = new StateMachineBuilder()
            .State("test").End();

        var root1 = builder.Build();
        var root2 = builder.Build();

        Assert.Same(root1, root2);
    }

    [Fact]
    public void State_WithType_UsesTypeNameAsStateName()
    {
        var root = new StateMachineBuilder()
            .State<TestState>()
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState(nameof(TestState)));

        Assert.Null(ex);
    }

    [Fact]
    public void State_WithTypeAndName_UsesProvidedName()
    {
        var root = new StateMachineBuilder()
            .State<TestState>("custom")
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState("custom"));

        Assert.Null(ex);
    }

    [Fact]
    public void State_WithName_CreatesDefaultState()
    {
        var root = new StateMachineBuilder()
            .State("mystate")
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState("mystate"));

        Assert.Null(ex);
    }

    [Fact]
    public void State_EmptyName_WorksCorrectly()
    {
        var root = new StateMachineBuilder()
            .State("")
            .End()
            .Build();

        var ex = Record.Exception(() => root.ChangeState(""));

        Assert.Null(ex);
    }

    [Fact]
    public void FluentChain_MultipleStates_AllAccessible()
    {
        var root = new StateMachineBuilder()
            .State("a").End()
            .State("b").End()
            .State("c").End()
            .Build();

        Assert.Null(Record.Exception(() => root.ChangeState("a")));
        Assert.Null(Record.Exception(() => root.ChangeState("b")));
        Assert.Null(Record.Exception(() => root.ChangeState("c")));
    }

    [Fact]
    public void FluentChain_NestedStates_HierarchyCorrect()
    {
        IState? level1State = null;
        IState? level2Parent = null;

        var root = new StateMachineBuilder()
            .State("level1")
                .Enter(s => { level1State = s; s.PushState("level2"); })
                .State("level2")
                    .Enter(s => level2Parent = s.Parent)
                .End()
            .End()
            .Build();

        root.ChangeState("level1");

        Assert.NotNull(level1State);
        Assert.Equal(root, level1State!.Parent);
        Assert.NotNull(level2Parent);
        Assert.Equal(level1State, level2Parent);
    }

}
