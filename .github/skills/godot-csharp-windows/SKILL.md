---
name: godot-csharp-windows
description: "Use when working on a Godot 4.6.x .NET C# project in GitHub Copilot on Windows, especially on machines without a dedicated GPU. Prefer C# over GDScript, use Windows-friendly CLI commands, and favor headless validation, debugging, and export workflows that do not depend on custom engine forks or GPU-heavy tooling."
---

# Godot C# Windows Skill

Use this skill for practical Godot work in GitHub Copilot when the project targets Godot 4.6.x with .NET and C# on Windows.

This skill is intentionally opinionated:

- Treat C# as the default and required scripting language.
- Assume Windows PowerShell for commands unless the repository already uses another shell.
- Prefer workflows that run on a stock Godot .NET editor plus the .NET SDK.
- Prefer headless validation and low-dependency tooling for machines without a dedicated GPU.
- Do not introduce GDScript for gameplay, tools, tests, or examples unless the user explicitly asks for it.

## Non-Negotiables

- Write Godot scripts in C#, not GDScript.
- Translate GDScript examples from docs into C# instead of copying them directly.
- Keep Godot API names in PascalCase in C# code.
- Use `public partial class` for Godot C# classes.
- Keep the C# class name aligned with the file name for attached scripts.
- Rebuild project assemblies after adding exported properties, signals, or new C# scripts so the editor recognizes them.

Read [references/csharp-rules.md](./references/csharp-rules.md) before writing or editing Godot C# scripts.

## Start Here

1. Confirm the project is a Godot .NET project by checking for `project.godot` and one or more `.csproj` or `.sln` files.
2. Inspect the existing scene and script layout before proposing new architecture.
3. Reuse the repository's current naming, folder structure, and signal wiring style.
4. If the task involves exports, read `export_presets.cfg` before changing build settings.
5. If the task involves validation, prefer the cheapest local check that works on Windows without a GPU.

Read [references/windows-dotnet-workflow.md](./references/windows-dotnet-workflow.md) when you need setup, build, debug, or PowerShell command patterns.
Read [references/scene-and-node-architecture.md](./references/scene-and-node-architecture.md) when the task involves building scenes, node hierarchies, UI trees, gameplay composition, signal wiring, or subscene boundaries.

## Default Workflow

### Code Changes

1. Read the relevant `.cs`, `.tscn`, and project files before editing.
2. Keep game logic in C# scripts and avoid scattering behavior into scene metadata or ad hoc editor-only state.
3. For scene-building tasks, start from the owning scene root and design the node structure before writing behavior.
4. Prefer straightforward scene edits over manual `.tscn` rewrites unless there is no safer option.
5. Use typed node access with `GetNode<T>()` or `GetNodeOrNull<T>()` and resolve references in `_Ready()`.
6. Prefer C# events or typed signal helpers over raw string-based connections when possible.

### Scene And Node Work

For actual gameplay and UI construction work, the agent should actively help with scene composition, not just scripts.

- Propose an explicit root node type that matches the scene purpose.
- Design stable node hierarchies for gameplay, UI, cameras, collision, audio, and effects.
- Split reusable parts into subscenes instead of overloading one large scene.
- Attach C# scripts to the nodes that own behavior rather than creating god objects.
- Keep signals and exported references aligned with the node ownership model.

Read [references/scene-and-node-architecture.md](./references/scene-and-node-architecture.md) before designing or restructuring node trees.

### Validation

Use the narrowest validation that matches the change:

- C# code change only: `dotnet build`
- New exported members or signal changes: `dotnet build` and Godot solution build
- Scene or boot path changes: headless Godot smoke run
- Export change: debug export first, then release export if requested

Read [references/validation-and-export.md](./references/validation-and-export.md) before validating or exporting.

## Tooling Priorities

Prefer these in order:

1. Existing repository scripts and tasks
2. `dotnet build` or `dotnet test` when the repo already has a .NET test project
3. Stock Godot CLI with `--headless`, `--path`, `--quit`, and `--build-solutions`
4. Existing export presets in `export_presets.cfg`

## VS Code Debugging Reality Check

For current Godot 4 .NET projects in VS Code, prefer the modern `.NET` debugging path over older Godot-specific Mono debugger integrations.

- Prefer `woberg.godot-dotnet-tools` for generating VS Code launch and task files.
- Prefer `coreclr` launch configurations that run the Godot executable directly.
- Set `godot-dotnet-tools.executablePath` to the exact full path of the real Godot executable, not a wrapper script, alias, or launcher.
- Use a `dotnet build <project>.csproj` prelaunch task.
- Expect working launch profiles to look like `Launch`, `Launch Editor`, and `Attach to Process` using `type: "coreclr"`.
- Treat older `neikeq.godot-csharp-vscode` / `godot-mono` launch flows as legacy. They may still work in some environments, but they are not the preferred default for Godot 4 .NET debugging guidance.
- If a launch starts only a debug helper process but no visible game window, suspect the legacy extension/debugger path first and switch to the `coreclr` launch model.

When the user asks for VS Code debugging help on a Godot 4 .NET project, bias toward the `woberg.godot-dotnet-tools` workflow unless the repository already has a confirmed working alternative.

Avoid pulling in heavy or specialized tooling by default:

- Do not require custom Godot engine forks unless the user explicitly wants them.
- Do not assume PlayGodot is available.
- Do not adopt GdUnit4 as the default test path for C# projects.
- Do not add AI asset-generation or image workflows unless the user asks for them.

## Focused Reference Modules

GitHub Copilot skills do not have a formal nested subskill primitive, but you can structure one skill like a main router with focused reference modules.

Read extra modules only when the task needs them:

- [references/csharp-rules.md](./references/csharp-rules.md) for Godot C# code changes
- [references/windows-dotnet-workflow.md](./references/windows-dotnet-workflow.md) for setup, build, run, and debugging on Windows
- [references/scene-and-node-architecture.md](./references/scene-and-node-architecture.md) for scene roots, node trees, reusable subscenes, and signal wiring
- [references/validation-and-export.md](./references/validation-and-export.md) for validation and packaging work
- [references/asset-generation.md](./references/asset-generation.md) when the user asks for asset generation, concept art, textures, icons, sprites, placeholder art, or import-ready art guidance

## Windows Constraints

- Use PowerShell-friendly commands and path syntax.
- Expect the Godot executable to be either on `PATH`, exposed by an environment variable such as `GODOT4`, or provided by the user.
- Prefer headless commands on low-end or no-GPU machines for import, build, smoke tests, and export.
- Keep validation steps deterministic and cheap before suggesting editor-driven checks.

## C#-Specific Godot Guidance

- Godot C# projects need the .NET SDK installed locally.
- Godot generates and uses `.sln` and `.csproj` files for C# projects; those should usually be committed.
- Everything under `.godot/` is generated state and typically should not be relied on for durable edits.
- When using `Call`, `Set`, or string-based `Connect`, remember those still use Godot's native snake_case names. Prefer generated `MethodName`, `PropertyName`, and `SignalName` constants when available.
- For struct-backed properties such as `Position`, modify a local copy and assign it back.

## What To Avoid

- Do not generate GDScript snippets in a C# task unless the user explicitly asks for a comparison.
- Do not recommend web export for Godot 4 C# projects as a default path.
- Do not assume Android or iOS export is ready; treat mobile C# export as a separate setup task.
- Do not replace an existing repository workflow with a new framework unless there is a clear problem to solve.
- Do not dump all behavior into one root script when the scene should be split into reusable subscenes or child controllers.
- Do not handwave node structure decisions; for game features, propose concrete nodes, ownership, and scene boundaries.

## Reference Files

- [references/csharp-rules.md](./references/csharp-rules.md): core Godot C# rules and pitfalls
- [references/windows-dotnet-workflow.md](./references/windows-dotnet-workflow.md): Windows setup, build, run, and debug workflow
- [references/scene-and-node-architecture.md](./references/scene-and-node-architecture.md): practical Godot scene composition, node hierarchy design, and C# ownership guidance
- [references/validation-and-export.md](./references/validation-and-export.md): validation order, headless smoke tests, and export rules
- [references/asset-generation.md](./references/asset-generation.md): practical asset-generation hints for Windows, low-GPU workflows, and Godot import readiness

## Source Strategy

This skill merges the most useful parts of the downloaded Godot skill repositories while removing workflows that are a poor default fit for this environment:

- From the Codex `godogen` skill: staged workflow, context hygiene, and targeted API lookup mindset
- From the portable `godot` skill: scene-editing caution, export preset reuse, and validation discipline
- From the Godot-Claude-Skills repo: testing/export awareness, but filtered to exclude GDScript-first and custom-fork requirements

Use those ideas as guidance, but keep this skill focused on GitHub Copilot agents, Windows, stock Godot tooling, and C#.