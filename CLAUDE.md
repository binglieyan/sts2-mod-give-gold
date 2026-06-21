# GiveGold - Slay the Spire 2 Co-op Mod

A mod for Slay the Spire 2 that lets players give gold to teammates in multiplayer runs via a clickable top-bar gold UI.

## Tech Stack
- **Godot 4.6.2 Mono** (C#, .NET 10.0) — UI layer, exported as `.pck`
- **Harmony** — runtime patching of STS2 game code
- **BepInEx.AssemblyPublicizer** — access internal STS2 types
- **Slay the Spire 2** — referenced assemblies at `$(Sts2Path)` (Steam install)

## Project Structure

```
GiveGoldInitializer.cs    → Mod entry point ([ModInitializer], Harmony bootstrap)
GiveGoldService.cs        → Public API facade (lifecycle, panel orchestration)
Core/
  GiveGoldTypes.cs        → GiveTarget and GiveResult record structs
  GiveGoldValidator.cs    → State checks (in-run, multiplayer, not-in-combat, targets)
  GiveGoldExecutor.cs     → Gold transfer logic (send/receive, sync + SFX playback)
  GiveGoldRequestDedup.cs → Deduplication ring buffer (ConcurrentDictionary, max 1000)
  GiveGoldLoc.cs          → Localization (zh/en fallback strings)
Network/
  GiveGoldNetworkHandler.cs   → Registers/deregisters network message handler
  Messages/
	GiveGoldRequestMessage.cs → Custom INetMessage (broadcast, reliable, PacketWriter/Reader)
Ui/
  GameTheme.cs            → Game-native theme (StsColors, fonts, sounds, animations)
  GiveGoldPanel.cs        → Godot Control panel (target picker, amount input, send/close)
Integration/
  GiveGoldBootstrapPatch.cs → Harmony patches hooking NGame._Ready, NRun._Ready,
							   NClickableControl._GuiInput (NTopBarGold click), RunManager.CleanUp
```

## Key Mod Behaviors

- **Entry**: `GiveGoldInitializer.Initialize()` is called by STS2 mod loader; sets up Harmony patches and localization
- **Lifecycle**: `NGame._Ready` → global init; `NRun._Ready` → attach network handler; `RunManager.CleanUp` → detach+cleanup
- **UI trigger**: Click the gold display in the top bar → toggle the GiveGold panel
- **Constraints**: Panel only opens when in a multiplayer run, not in combat, with at least one connected teammate
- **Network**: Gold is directly modified on `Player.Gold` (sync, no async). A reliable broadcast message is sent after local deduction. If sending fails, the deduction is rolled back. Messages are deduplicated by request ID and applied on both sender and receiver sides. Incoming gold plays tiered SFX via `SfxCmd.Play()` based on amount thresholds

## Build Output

- Post-build: DLL auto-copied to `$(Sts2Path)\mods\`
- Post-publish: Godot `.pck` auto-exported to `$(Sts2Path)\mods\GiveGold.pck`
- Requires `$(Sts2Path)` and `$(GodotPath)` configured in `.csproj` or set as env vars

## Coding Conventions

- `#nullable enable` on all files
- File-scoped namespaces (`namespace GiveGold.Core;`)
- `internal` by default; only service API and types are `public`
- Gold transfers are fully synchronous — no `async`/`await`/`Task<>`. Gold is modified directly on `Player.Gold` rather than through async command methods
- All user-facing strings go through `GiveGoldLoc.Get(key, args...)`
- Godot UI built entirely in code (no `.tscn` scenes)
- UI styling via `GameTheme` static helpers (`MakeLabel`, `MakeButton`, `MakePanelStyle`, etc.); panels call `ApplyFontRecursive(this)` for CJK font coverage
