# Stage 0 Setup Notes

## Current structure

This repository is already laid out for the phased shooter demo:

```text
res://
  asset/
  scene/
    main/
    player/
    bullet/
    enemy/
    ui/
  script/
    common/
```

This keeps scene files and scene-specific scripts close together:

```text
res://scene/main/Main.tscn
res://scene/main/Main.cs
res://scene/player/Player.tscn
res://scene/player/Player.cs
```

That layout matches the project plan and scales cleanly for later phases.

## AI-created script skeletons

The following minimal C# files are now in place:

- `res://scene/main/Main.cs`
- `res://scene/player/Player.cs`
- `res://scene/bullet/Bullet.cs`
- `res://scene/enemy/Enemy.cs`

They are intentionally small. Phase 0 should only verify that Godot can load and bind C# scripts cleanly. Gameplay logic will be added in later phases.

## Godot project settings to confirm

For Phase 0, the main items to verify in Godot are:

1. `Project Settings > Application > Run > Main Scene`
   Set to `res://scene/main/Main.tscn`
2. `Project Settings > Dotnet > Assembly Name`
   Keep it as `test_godot` unless you want the assembly renamed
3. `Project > Reload Current Project` after first C# attachment if Godot asks to generate C# solution files
4. Scene root for `Main.tscn`
   Keep `Node2D` unless Phase 1 gives you a reason to change it

## Expected C# side effects

When Godot finishes its C# setup, it may generate files such as:

- `test_godot.csproj`
- `.godot/mono/`
- NuGet or Roslyn cache files

That is normal. Those generated files do not need to be hand-written first.

## Phase 0 verification checklist

1. Open the project in Godot
2. Open `Main.tscn`
3. Confirm `Main` is bound to `Main.cs`
4. Run the project
5. Check the output panel for `Main scene loaded.`

If that message appears without C# compile errors, Phase 0 is complete from the AI side.
