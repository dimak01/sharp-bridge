# Application Config Hot Reload Implementation Checklist

## Overview
This document outlines the implementation plan for adding hot reload functionality to the application configuration (`ApplicationConfig.json`), allowing individual services to detect changes to their specific configuration sections and automatically invalidate themselves for recovery.

## Architecture Summary
- **Multiple FileSystemChangeWatcher instances**: One for transformation rules, one for application config
- **Service-level config management**: Each service subscribes to app config changes and manages its own section
- **Health-based recovery**: Services flag themselves as unhealthy when config changes, triggering existing recovery mechanisms
- **Selective reloading**: Only affected services restart, maintaining other connections

## Implementation Checklist

### Phase 1: Infrastructure Setup ✅
- [x] **Modify ServiceRegistration.cs**
  - [x] Register multiple named `IFileChangeWatcher` instances
  - [x] Add "TransformationRules" watcher instance
  - [x] Add "ApplicationConfig" watcher instance
  - [x] Ensure proper DI registration with unique identifiers

### Phase 2: Service Integration ✅
- [x] **VTubeStudioPhoneClient**
  - [x] Inject `IFileChangeWatcher` app config watcher
  - [x] Add `_configChanged` flag
  - [x] Subscribe to `FileChanged` events
  - [x] Implement `OnApplicationConfigChanged` handler
  - [ ] Add config comparison logic using `ConfigComparers.PhoneClientConfigsEqual`
  - [ ] Update internal config when changes detected
  - [x] Set `_configChanged = true` when config changes
  - [x] Modify `GetServiceStats()` to include config change in health calculation
  - [ ] Add `MarkConfigChanged()` and `ResetConfigChanged()` methods

- [x] **VTubeStudioPCClient**
  - [x] Inject `IFileChangeWatcher` app config watcher
  - [x] Add `_configChanged` flag
  - [x] Subscribe to `FileChanged` events
  - [x] Implement `OnApplicationConfigChanged` handler
  - [ ] Add config comparison logic using `ConfigComparers.PCClientConfigsEqual`
  - [ ] Update internal config when changes detected
  - [x] Set `_configChanged = true` when config changes
  - [x] Modify `GetServiceStats()` to include config change in health calculation
  - [ ] Add `MarkConfigChanged()` and `ResetConfigChanged()` methods

- [ ] **ShortcutConfigurationManager**
  - [ ] Inject `IFileChangeWatcher` app config watcher
  - [ ] Subscribe to `FileChanged` events
  - [ ] Implement `OnApplicationConfigChanged` handler
  - [ ] Add config comparison logic using `ConfigComparers.GeneralSettingsEqual`
  - [ ] Call `LoadFromConfiguration()` when general settings change
  - [ ] No health flags needed (not an IServiceStatsProvider)

### Phase 3: Configuration Comparers ✅
- [x] **Create ConfigComparers static class**
  - [x] `PhoneClientConfigsEqual(VTubeStudioPhoneClientConfig? x, VTubeStudioPhoneClientConfig? y)`
  - [x] `PCClientConfigsEqual(VTubeStudioPCConfig? x, VTubeStudioPCConfig? y)`
  - [x] `GeneralSettingsEqual(GeneralSettingsConfig? x, GeneralSettingsConfig? y)`
  - [x] `TransformationEngineConfigsEqual(TransformationEngineConfig? x, TransformationEngineConfig? y)`
  - [x] Explicit property-by-property comparison (no implicit reference type comparison)
  - [x] Handle null values gracefully
  - [x] Include all relevant properties in each comparison

### Phase 4: External Editor Service Enhancement ✅
- [ ] **Extend IExternalEditorService**
  - [ ] Add `TryOpenApplicationConfigAsync()` method
  - [ ] Update interface documentation

- [ ] **Extend ExternalEditorService**
  - [ ] Implement `TryOpenApplicationConfigAsync()`
  - [ ] Use same `%f` placeholder replacement logic
  - [ ] Use same error handling and logging patterns
  - [ ] Load `ApplicationConfig.json` path from `ConfigManager`

- [ ] **Add new ShortcutAction**
  - [ ] Add `OpenApplicationConfigInEditor` to `ShortcutAction` enum
  - [ ] Update `ShortcutConfigurationManager` default shortcuts
  - [ ] Add description attribute for help screen

### Phase 5: Context-Aware Shortcut Behavior ✅
- [ ] **Modify ApplicationOrchestrator**
  - [ ] Update `GetActionMethod()` to handle context-aware behavior
  - [ ] Check `_isShowingSystemHelp` flag for context detection
  - [ ] Call `OpenTransformationConfigInEditor()` when on transformation screen
  - [ ] Call `OpenApplicationConfigInEditor()` when on system help screen
  - [ ] Update shortcut registration logic

- [ ] **Update System Help Screen**
  - [ ] Change shortcut description to "Open Application Config in Editor"
  - [ ] Ensure description only shows on system help screen

### Phase 6: File Watching Integration ✅
- [ ] **Start file watching**
  - [ ] Ensure app config watcher starts watching `ApplicationConfig.json`
  - [ ] Verify watcher is properly disposed when services are disposed
  - [ ] Test file change detection and event handling

### Phase 7: Testing ✅
- [ ] **Unit Tests**
  - [ ] Test config comparers with various scenarios
  - [ ] Test service config change detection
  - [ ] Test health flag setting/resetting
  - [ ] Test external editor service for app config
  - [ ] Test context-aware shortcut behavior
  - [ ] Test file watching integration

- [ ] **Integration Tests**
  - [ ] Test end-to-end config change workflow
  - [ ] Test selective service reloading
  - [ ] Test recovery system integration
  - [ ] Test multiple simultaneous config changes

### Phase 8: Documentation & Cleanup ✅
- [ ] **Update documentation**
  - [ ] Update `ProjectOverview.md` with new hot reload capabilities
  - [ ] Update `UserGuide.md` with new shortcut behavior
  - [ ] Add examples of config change scenarios

- [ ] **Code cleanup**
  - [ ] Remove any temporary implementation artifacts
  - [ ] Ensure consistent error handling patterns
  - [ ] Verify logging is comprehensive and useful
  - [ ] Check for any memory leaks in event subscriptions

## Key Design Principles
1. **Minimal blast radius**: Changes isolated to individual services
2. **Reuse existing patterns**: Follow transformation engine hot reload approach
3. **Leverage existing infrastructure**: Use health flags and recovery system
4. **Explicit equality comparison**: No implicit reference type comparison
5. **Context-aware behavior**: Same shortcut, different behavior based on screen
6. **Comprehensive testing**: Unit and integration test coverage

## Success Criteria
- [ ] Application config changes trigger selective service reloading
- [ ] Only affected services restart, others continue running
- [ ] Context-aware shortcut opens correct config file
- [ ] Health system properly detects and recovers from config changes
- [ ] All existing functionality remains intact
- [ ] Comprehensive test coverage achieved
- [ ] Documentation updated and accurate

## Notes
- This implementation follows the existing transformation engine hot reload pattern
- Services become self-managing for their configuration sections
- No central orchestration needed - existing recovery system handles restarts
- File watching infrastructure is reused with multiple instances
- Context-aware shortcuts provide intuitive user experience 