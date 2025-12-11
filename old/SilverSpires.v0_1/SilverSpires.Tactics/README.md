# SilverSpires Tactics – Starter Solution (v0.1)

This package contains:

- `SilverSpires.Tactics.Core` – core rules, world, and character engine (class library).
- `SilverSpires.Tactics.Demo` – small console app showing a basic 1v1 encounter using the core engine.
- `UnityAdapters/SilverSpires.Tactics.UnityAdapter.cs` – example ScriptableObjects and MonoBehaviours for wiring the core engine into a Unity project.

## How to build / run (dotnet CLI)

```bash
cd SilverSpires.Tactics
dotnet build
dotnet run --project SilverSpires.Tactics.Demo
```

You should see a small log of a few combat rounds between two fighters.

## Using with Unity

- Create or open a Unity project.
- Copy `UnityAdapters/SilverSpires.Tactics.UnityAdapter.cs` and `SilverSpires.Tactics.Core` code into your Unity solution (or reference the compiled DLL of `SilverSpires.Tactics.Core`).
- Create ScriptableObjects for races, classes, spells, and encounters using the provided ScriptableObject types.
- Attach `CombatantView` and `EncounterController` to GameObjects to drive encounters in the scene.
