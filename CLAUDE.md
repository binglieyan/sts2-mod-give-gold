# GiveGold - Slay the Spire 2 Co-op Mod

A mod for Slay the Spire 2 that lets players give gold to teammates in multiplayer runs via a clickable top-bar gold UI.

## Tech Stack
- **Godot 4.7.2 Mono** (C#, .NET 10.0) — UI layer, exported as `.pck`
- **Harmony** — runtime patching of STS2 game code
- **BepInEx.AssemblyPublicizer** — access internal STS2 types
- **Slay the Spire 2** — referenced assemblies at `$(Sts2Path)` (Steam install)

## Project Structure

```
GiveGoldInitializer.cs    → Mod entry point ([ModInitializer], Harmony bootstrap)
GiveGoldService.cs        → Public API facade (lifecycle, panel orchestration)
Core/
  GiveGoldTypes.cs        → GiveTarget and GiveResult record structs
  GiveGoldValidator.cs    → State checks (in-run, multiplayer, not-in-combat, targets); owns the
                           GetConnectedPlayerIds() compat seam
  GiveGoldExecutor.cs     → Gold transfer logic (send/receive, sync + SFX playback)
  Sts2ApiCompat.cs        → Cross-version API shim (beta PlayerIds / stable ConnectedPlayerIds);
                           deletable once the new API reaches stable
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
- **Amount validation**: `1 ≤ amount ≤ 1,000,000` is enforced on both the send and receive paths (`GiveGoldExecutor.ValidateAmount`); gold additions clamp at `int.MaxValue` and remote deductions clamp at 0, so a tampered packet can't overflow or negative anyone's gold. Whether the sender can actually afford it is only enforced on the sender's own client (other clients' copies may lag, and rejecting there would desync)
- **Network**: Gold is directly modified on `Player.Gold` (sync, no async). The sender applies both halves locally (sender −amount, target +amount), then broadcasts a reliable message; if sending fails both halves are rolled back. Messages are deduplicated by request ID, so the sender's own echo is dropped. Every other client (the target *and* any third player) applies the same two-sided change on receipt — only the target additionally plays tiered SFX via `SfxCmd.Play()` — so all clients' gold copies stay converged

## Beta API Compatibility

- The beta build moved `RunLobby` to `MegaCrit.Sts2.Core.Multiplayer.Game.Lobby` and renamed `ConnectedPlayerIds` → `PlayerIds` (stable still has the old field/namespace)
- `GiveGoldValidator.GetConnectedPlayerIds()` is the single seam all callers read connected player ids through; it delegates to `Core/Sts2ApiCompat.cs` (reflection, new-first/old-fallback)
- **When the new API reaches stable**: delete `Core/Sts2ApiCompat.cs` and change the seam body to `return RunManager.Instance?.RunLobby?.PlayerIds;` — no other call sites change (full recipe in the compat file header)

## Build Output

- Post-build: DLL + `GiveGold.json` (mod manifest) auto-copied to `$(Sts2Path)\mods\`; the game only loads a local mod when its manifest sits next to the DLL
- Post-publish: Godot `.pck` auto-exported to `$(Sts2Path)\mods\GiveGold.pck`
- The `.pck` is **not mounted at runtime** (`"has_pck": false`; `ModManager.TryLoadMod` skips the whole PCK branch). Verified 2026-11-13 on v0.107.1 (stable) and v0.113.0 (beta): with no `.pck` present, the DLL still loads and `GiveGoldPanel` instantiates fine. Keep shipping it for forward compatibility, but don't rely on it for assets
- Requires `$(Sts2Path)` and `$(GodotPath)` configured in `.csproj` or set as env vars

## Coding Conventions

- `#nullable enable` on all files
- File-scoped namespaces (`namespace GiveGold.Core;`)
- `internal` by default; only service API and types are `public`
- Gold transfers are fully synchronous — no `async`/`await`/`Task<>`. Gold is modified directly on `Player.Gold` rather than through async command methods
- All user-facing strings go through `GiveGoldLoc.Get(key, args...)`
- Godot UI built entirely in code (no `.tscn` scenes)
- UI styling via `GameTheme` static helpers (`MakeLabel`, `MakeButton`, `MakePanelStyle`, etc.); panels call `ApplyFontRecursive(this)` for CJK font coverage
