# Console UI Modes Implementation Checklist

## Core types
- [ ] Create `ConsoleMode` enum: `Main`, `SystemHelp`, `NetworkStatus`
- [ ] Create `ExternalEditorTarget` enum: `TransformationConfig`, `ApplicationConfig`, `None`
- [ ] Add `ConsoleRenderContext` with:
  - [ ] `IEnumerable<IServiceStats>? ServiceStats`
  - [ ] `ApplicationConfig`
  - [ ] `UserPreferences`
  - [ ] `(int Width, int Height) ConsoleSize`
  - [ ] `CancellationToken`

## Unified renderer interface
- [ ] Add `IConsoleModeRenderer` with:
  - [ ] `ConsoleMode Mode`, `string DisplayName`, `ShortcutAction ToggleAction`, `ExternalEditorTarget EditorTarget`
  - [ ] `void Enter(IConsole console)`, `void Exit(IConsole console)`, `void Render(ConsoleRenderContext context)`
  - [ ] `TimeSpan PreferredUpdateInterval`

## Rename main renderer
- [ ] Rename interface `IConsoleRenderer` → `IMainStatusRenderer`
- [ ] Rename file/class `Utilities/ConsoleRenderer.cs` → `Utilities/MainStatusRenderer.cs`
- [ ] Make `MainStatusRenderer` implement `IMainStatusRenderer` and `IConsoleModeRenderer`

## System Help renderer
- [ ] Update `Utilities/SystemHelpRenderer.cs` to implement `IConsoleModeRenderer`
- [ ] Remove network troubleshooting section from help (moved to Network Status mode)
- [ ] Implement `Enter/Exit/Render`; write the built string to `IConsole`

## Network Status renderer (new)
- [ ] Add `Utilities/NetworkStatusRenderer.cs` implementing `IConsoleModeRenderer`
- [ ] Wire dependencies: `IPortStatusMonitorService`, `INetworkStatusFormatter`
- [ ] Implement behavior: async refresh, cache last snapshot, non-blocking
- [ ] Set `PreferredUpdateInterval` ~ 1–2s

## Mode manager
- [ ] Add `IConsoleModeManager` and `ConsoleModeManager`:
  - [ ] Track `CurrentMode`, `CurrentEditorTarget`
  - [ ] Implement `Toggle(ConsoleMode mode)` (same mode toggles back to `Main`)
  - [ ] Implement `SetMode(ConsoleMode mode)`, `Update(IEnumerable<IServiceStats> stats)`, `Clear()`
  - [ ] Respect each renderer’s `PreferredUpdateInterval`

## Orchestrator edits (`Services/ApplicationOrchestrator.cs`)
- [ ] Inject `IConsoleModeManager`
- [ ] Remove `_isShowingSystemHelp` and direct help rendering
- [ ] Replace status updates with `_modeManager.Update(allStats)`
- [ ] Map shortcuts:
  - [ ] `ShowSystemHelp` → `_modeManager.Toggle(ConsoleMode.SystemHelp)`
  - [ ] `ShowNetworkStatus` → `_modeManager.Toggle(ConsoleMode.NetworkStatus)`
- [ ] Make `OpenConfigInEditor` choose target via `_modeManager.CurrentEditorTarget`

## Shortcuts and config
- [ ] Add `ShowNetworkStatus` to `ShortcutAction` enum
- [ ] In `Configs/ApplicationConfig.json`, add mapping for `ShowNetworkStatus` (e.g., `"F2"`)
- [ ] Ensure `ShortcutConfigurationManager` recognizes the new action (enum-driven)

## DI wiring (`ServiceRegistration.cs`)
- [ ] Register `IMainStatusRenderer` → `MainStatusRenderer`
- [ ] Register all `IConsoleModeRenderer` implementations (Main, System Help, Network Status)
- [ ] Register `IConsoleModeManager`
- [ ] Remove usages/registrations of old `IConsoleRenderer`

## Tests
- [ ] Update/rename tests that referenced `IConsoleRenderer` → `IMainStatusRenderer`
- [ ] Add unit tests for:
  - [ ] Mode switching and toggling back to `Main`
  - [ ] Editor target per mode
  - [ ] Cadence handling in `ConsoleModeManager`
  - [ ] `NetworkStatusRenderer` snapshot/refresh behavior
- [ ] Ensure existing dashboard rendering tests still pass

## Docs
- [ ] Update `Docs/ProjectOverview.md` and `README.md` to mention:
  - [ ] Console modes and their shortcuts
  - [ ] Network Status as a separate mode
- [ ] Keep `Docs/ConsoleUIModesDesign.md` as the architectural reference

## Logging and UX
- [ ] Log mode transitions (enter/exit)
- [ ] Ensure each renderer clears or fully overwrites on `Enter`
- [ ] Optional: show current mode in header line

## Nice-to-have (post-MVP)
- [ ] Add a "Cycle Modes" shortcut to rotate: Main → Help → Network → Main
- [ ] Persist last active mode in user preferences (optional)
