# Godot C# Rules

Apply these rules whenever you write or edit Godot gameplay code in this workspace.

## Required Defaults

- Use `using Godot;` and a `public partial class` declaration.
- Match the class name to the `.cs` file name for attached scripts.
- Use PascalCase for Godot methods and properties in C#.
- Resolve node references in `_Ready()`; there is no direct `@onready` equivalent.
- Prefer explicit fields and properties over dynamic access.

## Core Patterns

```csharp
using Godot;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float Speed = 240.0f;
    [Signal] public delegate void DiedEventHandler();

    private AnimatedSprite2D _sprite = null!;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = direction * Speed;
        MoveAndSlide();
    }
}
```

## Signals

- Define signals with `[Signal] public delegate void NameEventHandler(...);`
- Emit with `EmitSignal(SignalName.Name, args...)`
- Prefer event syntax when available: `button.Pressed += OnPressed;`
- Use `await ToSignal(node, SomeNode.SignalName.Done);` for signal waits

## Safe Node Access

- Prefer `GetNode<T>("Path")` when the node must exist.
- Prefer `GetNodeOrNull<T>("Path")` when absence is acceptable.
- Cache repeated lookups in fields instead of resolving the same path every frame.

## Common Pitfalls

### Struct Properties

This fails because `Position` returns a struct copy:

```csharp
Position.X = 100.0f;
```

Use either of these:

```csharp
var position = Position;
position.X = 100.0f;
Position = position;
```

```csharp
Position = Position with { X = 100.0f };
```

### String-Based Godot Calls

Some APIs still expect Godot's snake_case names when accessed as strings.

Prefer:

```csharp
CallDeferred(Node.MethodName.AddChild, child);
```

Instead of:

```csharp
CallDeferred("AddChild", child);
```

## Exported Members

- Use `[Export]` for editor-visible properties.
- After adding or renaming exported members or signals, rebuild the C# project so Godot refreshes editor metadata.
- Prefer strongly typed exported resources such as `PackedScene`, `Texture2D`, `AudioStream`, and `NodePath`.

## Collections

- Use normal .NET collections for internal logic.
- Use `Godot.Collections.Array` and `Godot.Collections.Dictionary` only when Godot API interop requires them.

## Performance Hygiene

- Avoid repeated native property access in tight loops.
- Cache frequently reused node references.
- Avoid unnecessary string-based lookups and marshaling-heavy interop when a typed alternative exists.