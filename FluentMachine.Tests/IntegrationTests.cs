using Xunit;

namespace FluentMachine.Tests;

public sealed class IntegrationTests
{

    [Fact]
    public void TrafficLight_CompleteCycle()
    {
        var states = new List<string>();

        var root = new StateMachineBuilder()
            .State("Red")
                .Enter(_ => states.Add("Red"))
            .End()
            .State("Yellow")
                .Enter(_ => states.Add("Yellow"))
            .End()
            .State("Green")
                .Enter(_ => states.Add("Green"))
            .End()
            .Build();

        root.ChangeState("Red");
        root.ChangeState("Green");
        root.ChangeState("Yellow");
        root.ChangeState("Red");

        Assert.Equal(new[] { "Red", "Green", "Yellow", "Red" }, states);
    }

    [Fact]
    public void GameCharacter_StateTransitions()
    {
        var history = new List<string>();

        var root = new StateMachineBuilder()
            .State("Idle")
                .Enter(_ => history.Add("Enter:Idle"))
                .Exit(_ => history.Add("Exit:Idle"))
            .End()
            .State("Walking")
                .Enter(_ => history.Add("Enter:Walking"))
                .Exit(_ => history.Add("Exit:Walking"))
            .End()
            .State("Jumping")
                .Enter(_ => history.Add("Enter:Jumping"))
                .Exit(_ => history.Add("Exit:Jumping"))
            .End()
            .Build();

        root.ChangeState("Idle");
        root.ChangeState("Walking");
        root.ChangeState("Jumping");
        root.ChangeState("Idle");

        Assert.Equal(new[]
        {
            "Enter:Idle",
            "Exit:Idle", "Enter:Walking",
            "Exit:Walking", "Enter:Jumping",
            "Exit:Jumping", "Enter:Idle"
        }, history);
    }

    [Fact]
    public void NestedMenu_Navigation()
    {
        var history = new List<string>();

        var root = new StateMachineBuilder()
            .State("MainMenu")
                .Enter(_ => history.Add("Main"))
                .Event("openOptions", s => s.PushState("Options"))
                .State("Options")
                    .Enter(_ => history.Add("Options"))
                    .Event("openAudio", s => s.PushState("Audio"))
                    .Event("openVideo", s => s.PushState("Video"))
                    .State("Audio")
                        .Enter(_ => history.Add("Audio"))
                    .End()
                    .State("Video")
                        .Enter(_ => history.Add("Video"))
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("MainMenu");
        Assert.Contains("Main", history);

        root.TriggerEvent("openOptions");
        Assert.Contains("Options", history);

        root.TriggerEvent("openAudio");
        Assert.Contains("Audio", history);
    }

    [Fact]
    public void EventDrivenStateMachine_WorksCorrectly()
    {
        var damage = 0;
        var healed = 0;

        var root = new StateMachineBuilder()
            .State("Combat")
                .Event("Damage", _ => damage += 10)
                .Event("Heal", _ => healed += 5)
            .End()
            .Build();

        root.ChangeState("Combat");

        root.TriggerEvent("Damage");
        root.TriggerEvent("Damage");
        root.TriggerEvent("Heal");

        Assert.Equal(20, damage);
        Assert.Equal(5, healed);
    }

    [Fact]
    public void ConditionBasedTransitions_WorkCorrectly()
    {
        var health = 100;
        var currentState = "";

        var root = new StateMachineBuilder()
            .State("Alive")
                .Enter(_ => currentState = "Alive")
                .Condition(() => health <= 0, s => s.Parent.ChangeState("Dead"))
            .End()
            .State("Dead")
                .Enter(_ => currentState = "Dead")
            .End()
            .Build();

        root.ChangeState("Alive");
        Assert.Equal("Alive", currentState);

        root.Update(1f);
        Assert.Equal("Alive", currentState);

        health = 0;
        root.Update(1f);
        Assert.Equal("Dead", currentState);
    }

    [Fact]
    public void ThreeLevelHierarchy_EventPropagation()
    {
        var eventReceived = "";

        var root = new StateMachineBuilder()
            .State("L1")
                .Event("test", _ => eventReceived = "L1")
                .Event("goL2", s => s.PushState("L2"))
                .State("L2")
                    .Event("test", _ => eventReceived = "L2")
                    .Event("goL3", s => s.PushState("L3"))
                    .State("L3")
                        .Event("test", _ => eventReceived = "L3")
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("L1");
        root.TriggerEvent("test");
        Assert.Equal("L1", eventReceived);

        root.TriggerEvent("goL2");
        root.TriggerEvent("test");
        Assert.Equal("L2", eventReceived);

        root.TriggerEvent("goL3");
        root.TriggerEvent("test");
        Assert.Equal("L3", eventReceived);
    }

    [Fact]
    public void ThreeLevelHierarchy_UpdatePropagation()
    {
        var updateHistory = new List<string>();

        var root = new StateMachineBuilder()
            .State("L1")
                .Update((_, _) => updateHistory.Add("L1"))
                .Event("goL2", s => s.PushState("L2"))
                .State("L2")
                    .Update((_, _) => updateHistory.Add("L2"))
                    .Event("goL3", s => s.PushState("L3"))
                    .Event("back", s => s.Parent.PopState())
                    .State("L3")
                        .Update((_, _) => updateHistory.Add("L3"))
                        .Event("back", s => s.Parent.PopState())
                    .End()
                .End()
            .End()
            .Build();

        root.ChangeState("L1");
        root.Update(1f);
        Assert.Equal("L1", updateHistory.Last());

        root.TriggerEvent("goL2");
        root.Update(1f);
        Assert.Equal("L2", updateHistory.Last());

        root.TriggerEvent("goL3");
        root.Update(1f);
        Assert.Equal("L3", updateHistory.Last());

        root.TriggerEvent("back"); // Go back to L2
        root.Update(1f);
        Assert.Equal("L2", updateHistory.Last());
    }

    [Fact]
    public void EnemyAI_PatrolChaseAttack()
    {
        var playerDistance = 100f;
        var currentState = "";
        var attackCount = 0;

        var root = new StateMachineBuilder()
            .State("Patrol")
                .Enter(_ => currentState = "Patrol")
                .Condition(() => playerDistance < 50, s => s.Parent.ChangeState("Chase"))
            .End()
            .State("Chase")
                .Enter(_ => currentState = "Chase")
                .Condition(() => playerDistance >= 50, s => s.Parent.ChangeState("Patrol"))
                .Condition(() => playerDistance < 10, s => s.Parent.ChangeState("Attack"))
            .End()
            .State("Attack")
                .Enter(_ => currentState = "Attack")
                .Update((_, _) => attackCount++)
                .Condition(() => playerDistance >= 10, s => s.Parent.ChangeState("Chase"))
            .End()
            .Build();

        root.ChangeState("Patrol");
        Assert.Equal("Patrol", currentState);

        root.Update(1f);
        Assert.Equal("Patrol", currentState);

        playerDistance = 30;
        root.Update(1f);
        Assert.Equal("Chase", currentState);

        playerDistance = 5;
        root.Update(1f);
        Assert.Equal("Attack", currentState);

        root.Update(1f);
        root.Update(1f);
        Assert.Equal(2, attackCount);

        playerDistance = 15;
        root.Update(1f);
        Assert.Equal("Chase", currentState);
    }

}
