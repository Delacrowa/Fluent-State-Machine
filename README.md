# FluentMachine

Fluent API for creating [hierarchical finite state machines](http://aigamedev.com/open/article/hfsm-gist/) in C#.

## Installation

```csharp
using FluentMachine;
```

## Quick Start

```csharp
var rootState = new StateMachineBuilder()
    .State("main")
        .Enter(state => Console.WriteLine("Entered main state"))
        .Update((state, deltaTime) => Console.WriteLine("Updating main state"))
    .End()
    .Build();

rootState.ChangeState("main");
rootState.Update(deltaTime);
```

## Features

### Conditions

```csharp
.State("main")
    .Condition(() => isHungry, state => state.Parent.ChangeState("eating"))
.End()
```

### Nested States

```csharp
.State("combat")
    .State("attack").End()
    .State("defend").End()
.End()
```

### Push/Pop State Stack

```csharp
.Condition(() => shouldPause, state => state.PushState("paused"))
.Condition(() => shouldResume, state => state.Parent.PopState())
```

### Events

```csharp
.Event("damage", state => TakeDamage())
// ...
rootState.TriggerEvent("damage");
```

### Custom States

```csharp
class PlayerState : AbstractState
{
    public int Health { get; set; } = 100;
}

.State<PlayerState>()
    .Update((state, dt) => state.Health -= 1)
.End()
```

## Project Structure

```
FluentMachine.sln
├── FluentMachine/                        # Core library
├── FluentMachine.Tests/                  # Unit tests
├── FluentMachine.Examples.Battle/        # Combat example
└── FluentMachine.Examples.SharkLife/     # Simple example
```

## Compatibility

- **FluentMachine** - .NET Standard 2.1 (Unity 6 compatible)
- **Tests & Examples** - .NET 9.0

## License

MIT
