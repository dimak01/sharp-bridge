# Console UI Modes Implementation Checklist

## Core types
- [x] Create `ConsoleMode` enum: `Main`, `SystemHelp`, `NetworkStatus`
- [x] Add `ConsoleRenderContext` with:
  - [x] `IEnumerable<IServiceStats>? ServiceStats`
  - [x] `ApplicationConfig`
  - [x] `UserPreferences`
  - [x] `(int Width, int Height) ConsoleSize`
  - [x] `CancellationToken`

## Unified renderer interface
- [x] Add `IConsoleModeRenderer` with:
  - [x] `ConsoleMode Mode`, `string DisplayName`, `ShortcutAction ToggleAction`
  - [x] `void Enter(IConsole console)`, `void Exit(IConsole console)`, `void Render(ConsoleRenderContext context)`
  - [x] `TimeSpan PreferredUpdateInterval`
  - [x] `Task<bool> TryOpenInExternalEditorAsync()`

## Rename main renderer
- [x] Rename interface `IConsoleRenderer` → `IMainStatusRenderer`
- [x] Rename file/class `Utilities/ConsoleRenderer.cs` → `Utilities/MainStatusRenderer.cs`
- [x] Make `MainStatusRenderer` implement `IMainStatusRenderer` and `IConsoleModeRenderer`

## System Help renderer
- [x] Update `Utilities/SystemHelpRenderer.cs` to implement `IConsoleModeRenderer`
- [ ] Remove network troubleshooting section from help (moved to Network Status mode)
- [x] Implement `Enter/Exit/Render`; write the built string to `IConsole`
- [x] Implement `TryOpenInExternalEditorAsync` to open Application config

## Network Status renderer (new)
- [x] Add `Utilities/NetworkStatusRenderer.cs` implementing `IConsoleModeRenderer`
- [x] Wire dependencies: `IPortStatusMonitorService`, `INetworkStatusFormatter`
- [x] Implement behavior: async refresh, cache last snapshot, non-blocking
- [x] Set `PreferredUpdateInterval` ~ 1–2s
- [x] Implement `TryOpenInExternalEditorAsync` to open Application config

## Mode manager
- [x] Add `IConsoleModeManager` and `ConsoleModeManager`:
  - [x] Track `CurrentMode`
  - [x] Implement `Toggle(ConsoleMode mode)` (same mode toggles back to `Main`)
  - [x] Implement `SetMode(ConsoleMode mode)`, `Update(IEnumerable<IServiceStats> stats)`, `Clear()`
  - [x] Respect each renderer's `PreferredUpdateInterval`
  - [x] Expose a method to forward "Open in editor" to the active renderer (`TryOpenActiveModeInEditorAsync`)

## Orchestrator edits (`Services/ApplicationOrchestrator.cs`)
- [x] Inject `IConsoleModeManager`
- [x] Remove `_isShowingSystemHelp` and direct help rendering
- [x] Replace status updates with `_modeManager.Update(allStats)`
- [x] Map shortcuts:
  - [x] `ShowSystemHelp` → `_modeManager.Toggle(ConsoleMode.SystemHelp)`
  - [x] `ShowNetworkStatus` → `_modeManager.Toggle(ConsoleMode.NetworkStatus)`
- [x] Make `OpenConfigInEditor` call the manager to forward to the active renderer

## Shortcuts and config
- [x] Add `ShowNetworkStatus` to `ShortcutAction` enum
- [x] In `Configs/ApplicationConfig.json`, add mapping for `ShowNetworkStatus` (e.g., `"F2"`)
- [ ] Ensure `ShortcutConfigurationManager` recognizes the new action (enum-driven)

## DI wiring (`ServiceRegistration.cs`)
- [x] Register `IMainStatusRenderer` → `MainStatusRenderer`
- [x] Register all `IConsoleModeRenderer` implementations (Main, System Help, Network Status)
- [x] Register `IConsoleModeManager`
- [x] Remove usages/registrations of old `IConsoleRenderer`

## Tests
- [ ] Update/rename tests that referenced `IConsoleRenderer` → `IMainStatusRenderer`
- [ ] Add unit tests for:
  - [ ] Mode switching and toggling back to `Main`
  - [ ] Forwarding of "Open in editor" to active renderer
  - [ ] Cadence handling in `ConsoleModeManager`
  - [ ] `NetworkStatusRenderer` snapshot/refresh behavior
- [ ] Ensure existing dashboard rendering tests still pass

## Docs
- [ ] Update `Docs/ProjectOverview.md` and `README.md` to mention:
  - [ ] Console modes and their shortcuts
  - [ ] Network Status as a separate mode
- [ ] Add a short addendum doc noting editor handling is per-renderer (manager just forwards)

## Logging and UX
- [ ] Log mode transitions (enter/exit)
- [ ] Ensure each renderer clears or fully overwrites on `Enter`
- [ ] Optional: show current mode in header line

## Nice-to-have (post-MVP)
- [ ] Add a "Cycle Modes" shortcut to rotate: Main → Help → Network → Main
- [ ] Persist last active mode in user preferences (optional)
