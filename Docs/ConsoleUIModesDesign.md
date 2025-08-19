# Console UI Modes: Unified Rendering, Mode Manager, and Network Status Mode

## Introduction

This document proposes and specifies a refactor and feature addition for the console UI system:

- Introduce a unified renderer interface implemented by each console "mode" (Main, System Help, Network Status)
- Extract a dedicated mode manager responsible for selecting active mode, delegating rendering, and handling editor-target behavior
- Rename the current main dashboard renderer from `IConsoleRenderer` to `IMainStatusRenderer` for accuracy
- Promote Network Status into its own dedicated mode (separate from System Help)

The goal is to improve cohesion, readability, testability, and extensibility of the console UI while preserving the current user experience and shortcuts.

## Goals

- Clear separation of concerns: each mode renders its own view end-to-end
- Centralized mode switching and UI orchestration
- Minimal impact to data flow, recovery, and non-UI logic
- Extensible foundation for new modes in the future

## Non-Goals

- Overhauling the visual formatting system (colors/tables) beyond what the modes need
- Changing the core application pipeline or recovery strategy

## Current State (Summary)

- The main status dashboard is implemented by `Utilities/ConsoleRenderer.cs`, behind `Interfaces/IConsoleRenderer.cs`.
- System Help is implemented by `Utilities/SystemHelpRenderer.cs`, returning a large formatted string that the orchestrator writes to the console. It currently embeds Network Troubleshooting output.
- Mode switching is handled inside `Services/ApplicationOrchestrator.cs` using a boolean `_isShowingSystemHelp`, with F1 toggling help and suppressing main dashboard updates while help is shown.

## Proposed Architecture

### Key Concepts

- Console modes are first-class citizens. Each mode:
  - Owns its rendering pipeline
  - Knows which shortcut toggles it
  - Specifies which external editor target is appropriate while active
- A lightweight mode manager coordinates active mode, switching, and per-tick rendering.

### New/Updated Abstractions

1) Console modes enum

```csharp
public enum ConsoleMode
{
    Main,
    SystemHelp,
    NetworkStatus
}
```

2) Unified renderer interface implemented by all modes

```csharp
public interface IConsoleModeRenderer
{
    ConsoleMode Mode { get; }
    string DisplayName { get; }

    // Shortcut the manager can use to wire up toggling (actual key mapping remains in config)
    ShortcutAction ToggleAction { get; }

    // Which config the "Open in editor" action should target while this mode is active
    ExternalEditorTarget EditorTarget { get; }

    // Lifecycle hooks when the mode becomes active/inactive
    void Enter(IConsole console);
    void Exit(IConsole console);

    // Single tick render; implementers should write to console via IConsole
    void Render(ConsoleRenderContext context);

    // Preferred cadence for this renderer; the manager may clamp this
    TimeSpan PreferredUpdateInterval { get; }
}
```

3) Render context (manager → renderer)

```csharp
public sealed class ConsoleRenderContext
{
    public IEnumerable<IServiceStats>? ServiceStats { get; init; }
    public ApplicationConfig ApplicationConfig { get; init; } = default!;
    public UserPreferences UserPreferences { get; init; } = default!;
    public (int Width, int Height) ConsoleSize { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
```

4) Mode manager

```csharp
public interface IConsoleModeManager
{
    ConsoleMode CurrentMode { get; }

    void Toggle(ConsoleMode mode);   // if toggling the current mode, return to Main
    void SetMode(ConsoleMode mode);  // force mode

    void Update(IEnumerable<IServiceStats> stats); // delegate to active renderer
    ExternalEditorTarget CurrentEditorTarget { get; }
    void Clear();
}
```

5) Rename `IConsoleRenderer` → `IMainStatusRenderer`

- Rationale: the existing interface/class are the main dashboard renderer; naming should reflect that this is one of multiple renderers.
- The class `Utilities/ConsoleRenderer.cs` will be renamed to `Utilities/MainStatusRenderer.cs` and updated to implement `IConsoleModeRenderer`.
- For transitional compatibility (tests), `IMainStatusRenderer` can be temporarily aliased where necessary.

### Modes (Renderers)

- Main Status Mode
  - Implementation: rename `ConsoleRenderer` → `MainStatusRenderer`
  - Interface: implements both `IMainStatusRenderer` and `IConsoleModeRenderer`
  - Behavior: builds lines from service stats and writes via `IConsole`, as today
  - ToggleAction: none (default mode)
  - EditorTarget: `TransformationConfig`
  - PreferredUpdateInterval: ~100 ms

- System Help Mode
  - Implementation: extend existing `SystemHelpRenderer` to implement `IConsoleModeRenderer`
  - Behavior: renders configuration and shortcuts; writes directly to console (string build + single write is fine)
  - Remove embedded Network Troubleshooting from help (moved to Network Status Mode)
  - ToggleAction: `ShowSystemHelp`
  - EditorTarget: `ApplicationConfig`
  - PreferredUpdateInterval: larger (e.g., 1–2 s) or on-demand since content is static

- Network Status Mode (New)
  - Implementation: new `NetworkStatusRenderer`
  - Behavior: periodically fetches network status via `IPortStatusMonitorService`; formats via `INetworkStatusFormatter`; outputs to console
  - Caches last snapshot; non-blocking updates
  - ToggleAction: `ShowNetworkStatus`
  - EditorTarget: `ApplicationConfig`
  - PreferredUpdateInterval: ~1–2 s

### Open in Editor Behavior

- While in Main: open Transformation config
- While in System Help or Network Status: open Application config
- The mode manager exposes `CurrentEditorTarget` so `ApplicationOrchestrator` can delegate without knowing active mode details

## Flow & Lifecycle

1) Startup
- DI registers all three renderers and the mode manager
- Mode manager defaults to `ConsoleMode.Main` and calls `Enter` on the main renderer

2) Main Loop (ApplicationOrchestrator)
- On each tick, call `IConsoleModeManager.Update(stats)` instead of directly calling a renderer
- Keyboard shortcuts are registered to call `modeManager.Toggle(SystemHelp)` and `modeManager.Toggle(NetworkStatus)`
- Normal data flow and recovery are unchanged

3) Switching Modes
- `Toggle` calls `Exit` on current renderer, clears console, `Enter` on new renderer
- If toggling to the same active mode, switch back to Main

4) Rendering Cadence
- The manager respects each renderer’s `PreferredUpdateInterval` to throttle calls to `Render`
- For async data (Network Status), the renderer should not block; it may kick off background refreshes and render the last-known snapshot

## DI and Wiring Changes

- Register new types:
  - `IConsoleModeManager` (singleton)
  - `IMainStatusRenderer` (singleton) → `MainStatusRenderer`
  - `IConsoleModeRenderer` implementations for System Help and Network Status (singletons)
- Rename interface `IConsoleRenderer` to `IMainStatusRenderer` (and update references)
- Update `ApplicationOrchestrator` constructor to depend on `IConsoleModeManager` and drop `_isShowingSystemHelp` and direct help rendering
- Keep `ISystemHelpRenderer` as a dependency of `SystemHelpRenderer` implementation
- `NetworkStatusRenderer` depends on `IPortStatusMonitorService` and `INetworkStatusFormatter`

## Configuration Updates

- Extend `ApplicationConfig.GeneralSettings.Shortcuts` with a new optional mapping:

```json
{
  "GeneralSettings": {
    "Shortcuts": {
      "ShowNetworkStatus": "F2" // example; configurable
    }
  }
}
```

## Orchestrator Changes

- Replace:
  - Status updates: `_modeManager.Update(allStats)` instead of `_consoleRenderer.Update(allStats)`
  - Help toggle handling: `_modeManager.Toggle(ConsoleMode.SystemHelp)`
  - New shortcut handling: `_modeManager.Toggle(ConsoleMode.NetworkStatus)`
  - Editor opening logic uses `_modeManager.CurrentEditorTarget`
- Remove `_isShowingSystemHelp` and the direct help string write logic

## Error Handling & Resiliency

- Renderers should fail fast and log; the manager should catch and ensure the app loop continues
- Network Status renderer guards background calls and falls back to last snapshot
- Enter/Exit are best-effort; failures should not crash the application

## Performance Considerations

- Main mode maintains current 100 ms cadence
- Network Status runs at slower cadence to reduce overhead
- Avoids expensive full-screen redraws more often than necessary

## Testing Strategy

- Unit tests:
  - Mode manager: switching, toggling back to Main, cadence handling, editor target resolution
  - MainStatusRenderer: still renders service stats as before
  - SystemHelpRenderer: implements unified interface and renders without network section
  - NetworkStatusRenderer: fetch cadence, caching, formatting integration
- Integration tests:
  - Shortcut-driven mode switching paths register and trigger appropriate actions
  - Editor target changes with mode

## Migration Plan

1) Introduce new abstractions (`ConsoleMode`, `IConsoleModeRenderer`, `IConsoleModeManager`, `ConsoleRenderContext`, `ExternalEditorTarget`)
2) Rename `IConsoleRenderer` → `IMainStatusRenderer`; implement `IConsoleModeRenderer` in the renamed `MainStatusRenderer`
3) Update `SystemHelpRenderer` to implement `IConsoleModeRenderer` and remove network section
4) Add `NetworkStatusRenderer` and wire to `IPortStatusMonitorService` + `INetworkStatusFormatter`
5) Add `IConsoleModeManager` and integrate into the orchestrator
6) Update DI registrations in `ServiceRegistration.cs`
7) Add optional `ShowNetworkStatus` shortcut to config
8) Update docs (`ProjectOverview.md`) to mention modes and manager
9) Update tests accordingly

## Risks & Mitigations

- Risk: Rendering regressions during rename/refactor → Mitigate with incremental commits and tests
- Risk: Console flicker or cursor mishandling across modes → Ensure each renderer fully owns its writes and clears on Enter/Exit
- Risk: Network Status acquisition latency → Use async refresh with cached snapshot

## Acceptance Criteria

- F1 toggles System Help; toggling again returns to Main
- F2 (or configured key) toggles Network Status; toggling again returns to Main
- Main mode renders service stats as today, at ~100 ms cadence
- System Help renders config and shortcuts (no network section)
- Network Status renders firewall/port analysis via the formatter; refreshes approximately every 1–2 seconds
- "Open config in editor" opens Transformation config in Main; opens Application config in Help/Network modes
- All changes are covered by unit tests

## Appendix: Example DI Sketch

```csharp
services.AddSingleton<IMainStatusRenderer, MainStatusRenderer>();
services.AddSingleton<IConsoleModeRenderer>(sp => sp.GetRequiredService<IMainStatusRenderer>());
services.AddSingleton<IConsoleModeRenderer, SystemHelpRenderer>();
services.AddSingleton<IConsoleModeRenderer, NetworkStatusRenderer>();
services.AddSingleton<IConsoleModeManager, ConsoleModeManager>();
```

```csharp
// In ApplicationOrchestrator
_modeManager.Update(allStats);
// Shortcuts
// ShowSystemHelp → () => _modeManager.Toggle(ConsoleMode.SystemHelp)
// ShowNetworkStatus → () => _modeManager.Toggle(ConsoleMode.NetworkStatus)
```


