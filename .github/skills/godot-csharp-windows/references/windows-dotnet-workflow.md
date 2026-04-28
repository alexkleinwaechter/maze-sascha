# Windows .NET Workflow

Use this workflow when GitHub Copilot is operating in a Godot C# project on Windows.

## Prerequisites

- A .NET-enabled Godot editor is installed.
- The .NET SDK is installed.
- The Godot executable is either on `PATH` or available through an environment variable such as `GODOT4`.
- The project contains `project.godot` and, once C# is initialized, `.csproj` and usually `.sln` files.

## Preferred Command Style

Use PowerShell examples by default.

If `GODOT4` is configured:

```powershell
& $env:GODOT4 --version
& $env:GODOT4 --headless --path $PWD --quit
& $env:GODOT4 --path $PWD --build-solutions
```

If Godot is already on `PATH`:

```powershell
godot --version
godot --headless --path $PWD --quit
godot --path $PWD --build-solutions
```

If the machine exposes `godot-mono` instead:

```powershell
godot-mono --version
godot-mono --headless --path $PWD --quit
godot-mono --path $PWD --build-solutions
```

## Minimal Build Loop

Use the smallest loop that fits the task:

```powershell
dotnet build
```

When Godot needs to regenerate or refresh the solution metadata:

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

## Running The Project

Interactive run:

```powershell
& $env:GODOT4 --path $PWD
```

Headless smoke check for startup/import issues:

```powershell
& $env:GODOT4 --headless --path $PWD --quit --verbose
```

Single-scene run when you need a focused check:

```powershell
& $env:GODOT4 --path $PWD --scene res://scenes/Main.tscn
```

## VS Code Debugging Notes

- Prefer the C# extension in VS Code.
- A common setup uses a build task that runs `dotnet build`.
- A common launch configuration points `program` to the Godot executable and sets `cwd` to the workspace folder.
- If the repository already contains `.vscode/tasks.json` or `.vscode/launch.json`, reuse and patch those instead of replacing them.

## Repository Hygiene

- Commit `.sln` and `.csproj` files for Godot C# projects unless the repo already follows a different rule.
- Do not depend on generated state inside `.godot/` for permanent fixes.
- If C# metadata seems broken, a targeted rebuild is safer than broad cleanup.