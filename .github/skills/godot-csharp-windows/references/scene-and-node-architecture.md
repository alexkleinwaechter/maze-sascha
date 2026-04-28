# Scene And Node Architecture

Use this guide when the task is actual Godot game development work: building node trees, composing scenes, wiring UI, splitting systems into subscenes, or deciding where C# scripts belong.

The point is not just to write C# code. The point is to shape a Godot scene tree the developer can maintain.

## Core Principle

Design the scene tree first, then attach behavior to the nodes that own it.

For most tasks, the agent should answer questions like these before writing much code:

- What is the root node type of the scene?
- Which children are structural, visual, physical, UI, and audio?
- Which parts should be their own reusable scenes?
- Which node owns the behavior for movement, combat, health, menus, or spawning?
- Which references should be exported versus looked up as child nodes?

## Root Node Selection

Pick the root node based on the scene's job.

Common choices:

- `Node` or `Node3D`: coordinator or generic gameplay scene root
- `Node2D`: 2D gameplay scene root
- `CharacterBody2D` or `CharacterBody3D`: player or enemy root when movement and collisions are central
- `Area2D` or `Area3D`: trigger, pickup, hitbox, sensor, interaction zone
- `RigidBody2D` or `RigidBody3D`: physics-driven object
- `Control`: UI screen root
- `CanvasLayer`: HUD or overlay root

Do not use a plain `Node` root if the scene's main behavior clearly belongs to a stronger type.

## Recommended Composition Pattern

A good default is one scene per gameplay unit or UI screen.

Examples:

### Player Scene

```text
Player(CharacterBody2D)
  Sprite(AnimatedSprite2D)
  CollisionShape(CollisionShape2D)
  Hurtbox(Area2D)
    CollisionShape(CollisionShape2D)
  Camera(Camera2D)
  WeaponAnchor(Marker2D)
  Audio(AudioStreamPlayer2D)
```

Attach the main player C# script to `Player`, not to the sprite or collision node.

### Enemy Scene

```text
Enemy(CharacterBody2D)
  Visuals(Node2D)
    Sprite(AnimatedSprite2D)
  CollisionShape(CollisionShape2D)
  Hitbox(Area2D)
  NavigationAgent(NavigationAgent2D)
```

Keep combat, movement, and health state in the enemy root script. Child nodes provide visuals, sensing, or collision.

### UI Menu Scene

```text
MainMenu(Control)
  Background(TextureRect)
  Content(MarginContainer)
    VBox(VBoxContainer)
      Title(Label)
      StartButton(Button)
      OptionsButton(Button)
      QuitButton(Button)
```

Attach the screen controller to `MainMenu` and wire button signals there.

## Reusable Subscenes

Split a subtree into its own scene when one of these is true:

- it appears multiple times
- it has its own behavior and lifecycle
- it needs independent testing or replacement
- the parent scene is becoming hard to read

Good subscene candidates:

- player
- enemy
- projectile
- pickup
- health bar
- inventory slot
- pause menu
- dialogue box
- damage popup

Do not split scenes prematurely for one-off static decorations.

## Script Ownership

Attach C# scripts to the node that owns the decision-making.

Good ownership examples:

- player movement script on `CharacterBody2D` player root
- enemy AI script on enemy root
- door interaction script on door root
- HUD controller on HUD root
- inventory slot script on reusable slot scene root

Avoid these patterns:

- one root script controlling every node in the level
- logic hidden in visual-only child nodes unless that child is a real reusable component
- unrelated systems mixed into a single monolithic controller

## Node Access Rules

Use child lookups for owned internal structure:

```csharp
private AnimatedSprite2D _sprite = null!;
private CollisionShape2D _collisionShape = null!;

public override void _Ready()
{
    _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
}
```

Use `[Export]` references when the relationship is external, configurable, or scene-dependent:

```csharp
[Export] public NodePath SpawnPointPath;
[Export] public PackedScene ProjectileScene;
```

Rule of thumb:

- own child you created and expect locally: `GetNode<T>()`
- external collaborator or designer-configured dependency: `[Export]`

## Signal Wiring

Prefer signal wiring that matches scene ownership.

Examples:

- `Button.Pressed` handled by the screen controller
- hurtbox `AreaEntered` handled by the player or enemy root
- projectile collision handled by the projectile root

Prefer C# event syntax when available:

```csharp
public override void _Ready()
{
    var startButton = GetNode<Button>("Content/VBox/StartButton");
    startButton.Pressed += OnStartPressed;
}
```

Only use lower-level `Connect()` when the API requires it or when wiring is dynamic.

## 2D Gameplay Heuristics

For most 2D gameplay scenes, the agent should consider these nodes early:

- `Camera2D`
- `CollisionShape2D`
- `AnimatedSprite2D` or `Sprite2D`
- `Area2D` for pickups, hitboxes, or sensors
- `Marker2D` for spawn, muzzle, or attachment points
- `AudioStreamPlayer2D`
- `Timer` for cooldowns and state timing

## 3D Gameplay Heuristics

For most 3D gameplay scenes, consider these early:

- `Camera3D`
- `CollisionShape3D`
- `MeshInstance3D` or imported model root
- `Area3D` for interactions and triggers
- `Marker3D` for sockets and spawn points
- `AudioStreamPlayer3D`
- `NavigationAgent3D` for AI movement

## UI Structure Heuristics

For UI scenes:

- root with `Control` or `CanvasLayer`
- use containers for layout before hand-tuning offsets
- keep decorative backgrounds separate from interactive controls
- isolate reusable widgets as their own scenes
- keep screen controller logic on the screen root

Prefer this order when building UI:

1. choose root
2. define major containers
3. place interactive controls
4. add decorative nodes
5. attach controller script
6. wire button and input signals

## What The Agent Should Propose

When the user asks for a feature, do not jump straight to code. Propose the scene shape.

A good response usually includes:

- root node type
- child node list
- which child nodes are reusable scenes
- where the main C# script lives
- which signals are connected
- what validation should confirm the scene still works

## Avoid These Mistakes

- picking node types by habit instead of behavior
- putting movement logic on a sprite node
- using exported references for every local child instead of a clear owned hierarchy
- building giant level scenes with embedded logic for every feature
- hand-editing `.tscn` files broadly when small structural edits would do
- mixing UI layout logic and gameplay logic in the same controller

## Practical Output Format

When designing a new scene, prefer to present it like this:

```text
Scene: Enemy
Root: CharacterBody2D
Children:
- AnimatedSprite2D Sprite
- CollisionShape2D CollisionShape2D
- Area2D Hurtbox
- NavigationAgent2D NavigationAgent2D
- Timer AttackCooldown

Script ownership:
- EnemyController.cs on root

Signals:
- Hurtbox.AreaEntered -> EnemyController.OnHurtboxAreaEntered
- AttackCooldown.Timeout -> EnemyController.OnAttackCooldownTimeout
```

That level of specificity is what helps a Godot developer, not a generic statement like "create an enemy scene and add logic."