# Validation And Export

Use this order when validating Godot C# work on Windows.

## Validation Order

1. `dotnet build` for script and compile issues
2. Godot solution build when exported members, signals, or generated bindings changed
3. Headless Godot smoke run for project startup or scene-loading changes
4. Export only when the task explicitly involves packaging

## Cheap Validation Commands

```powershell
dotnet build
```

```powershell
& $env:GODOT4 --path $PWD --build-solutions
```

```powershell
& $env:GODOT4 --headless --path $PWD --quit --verbose
```

Use `--headless` by default on machines without a dedicated GPU or when running non-interactive checks.

## When To Rebuild Solutions

Run a Godot solution build after:

- adding a first C# script to a project
- renaming an attached C# class or file
- adding or changing `[Export]` members
- adding or changing `[Signal]` declarations
- seeing stale editor metadata after a successful `dotnet build`

## Export Rules

- Read `export_presets.cfg` before changing export behavior.
- Reuse existing preset names exactly.
- Prefer debug export before release export when validating a packaging change.
- Keep export outputs outside the source tree unless the repository already uses a committed build folder.
- Make sure export templates are installed before treating an export failure as a project bug.

## Windows Export Examples

```powershell
& $env:GODOT4 --headless --path $PWD --export-debug "Windows Desktop" .\build\game.exe
```

```powershell
& $env:GODOT4 --headless --path $PWD --export-release "Windows Desktop" .\build\game.exe
```

## Platform Constraints For This Skill

- Do not default to Web export for Godot 4 C# projects.
- Treat Android and iOS C# export as explicit setup tasks, not a baseline capability.
- Do not add custom engine forks or automation-only exporters unless the user asks for that path.

## Test Strategy Guidance

- If the repository already has a .NET test project, use `dotnet test`.
- If it does not, do not introduce a GDScript-first test framework by default.
- For gameplay or scene regressions without an existing automated test harness, prefer a compile check plus a headless smoke run and then propose narrower follow-up testing only if needed.