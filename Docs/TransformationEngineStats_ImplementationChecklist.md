# TransformationEngine Statistics Implementation Checklist

## Phase 1: Data Models & Core Infrastructure

### Models to Create

- [x] **Create `Models/TransformationEngineStatus.cs`**
  - [x] Define enum with values: `Ready`, `Partial`, `ConfigErrorCached`, `NoValidRules`, `NeverLoaded`, `ConfigMissing`
  - [x] Add XML documentation for each status value

- [x] **Create `Models/RuleInfo.cs`**
  - [x] Properties: `string Name`, `string Error`, `string Type` (validation/evaluation)
  - [x] Constructor for easy creation
  - [x] XML documentation

- [x] **Create `Models/TransformationEngineInfo.cs`**
  - [x] Implement `IFormattableObject`
  - [x] Properties: `string ConfigFilePath`, `int ValidRulesCount`, `List<RuleInfo> InvalidRules`, `List<RuleInfo> AbandonedRules`
  - [x] Constructor with parameters
  - [x] XML documentation

## Phase 2: TransformationEngine Statistics

### Add Statistics Tracking Fields

- [x] **Add private fields to `TransformationEngine` class**
  - [x] `private long _totalTransformations = 0;`
  - [x] `private long _successfulTransformations = 0;`
  - [x] `private long _failedTransformations = 0;`
  - [x] `private long _hotReloadAttempts = 0;`
  - [x] `private long _hotReloadSuccesses = 0;`
  - [x] `private DateTime _lastSuccessfulTransformation;`
  - [x] `private DateTime _rulesLoadedTime;`
  - [x] `private string _lastError;`
  - [x] `private string _configFilePath;`
  - [x] `private TransformationEngineStatus _currentStatus = TransformationEngineStatus.NeverLoaded;`
  - [x] `private readonly List<RuleInfo> _invalidRules = new();`
  - [x] `private readonly List<RuleInfo> _lastAbandonedRules = new();`

### Implement IServiceStatsProvider

- [x] **Add `IServiceStatsProvider` to `TransformationEngine` class declaration**

- [x] **Implement `GetServiceStats()` method**
  - [x] Create counters dictionary from tracking fields
  - [x] Create `TransformationEngineInfo` entity with current state
  - [x] Determine `isHealthy` based on status and recent activity
  - [x] Return `ServiceStats` with all data

### Update Existing Methods

- [x] **Update `LoadRulesAsync()` method**
  - [x] Store `_configFilePath` parameter
  - [x] Track `_hotReloadAttempts` and `_hotReloadSuccesses`
  - [x] Populate `_invalidRules` list instead of just counting
  - [x] Set `_rulesLoadedTime` on successful load
  - [x] Update `_currentStatus` based on results
  - [x] Clear `_lastError` on success, set on failure

- [x] **Update `TransformData()` method**
  - [x] Increment `_totalTransformations`
  - [x] Track `_successfulTransformations` vs `_failedTransformations`
  - [x] Update `_lastSuccessfulTransformation` timestamp
  - [x] Populate `_lastAbandonedRules` list with evaluation failures
  - [x] Update `_currentStatus` based on results

## Phase 3: Console UI Integration

### Create Formatter

- [x] **Create `Utilities/TransformationEngineInfoFormatter.cs`**
  - [x] Implement `IFormatter` interface
  - [x] Add constructor taking `IConsole` and `ITableFormatter`
  - [x] Implement `CurrentVerbosity` property and `CycleVerbosity()` method
  - [x] Implement `Format(IServiceStats serviceStats)` method
  - [x] Display rule stats, config info, operational status
  - [x] Use `ITableFormatter` for invalid rules and abandoned rules tables
  - [x] Follow patterns from `PCTrackingInfoFormatter`

### Register Formatter

- [x] **Update `ConsoleRenderer` constructor**
  - [x] Add `TransformationEngineInfoFormatter` parameter
  - [x] Register formatter for `TransformationEngineInfo` type in constructor

- [x] **Update `ServiceRegistration.cs`**
  - [x] Add `TransformationEngineInfoFormatter` as singleton service registration

- [x] **Update `ConsoleRenderer.Update()` method**
  - [x] Add special case handling for `TransformationEngineInfoFormatter` like phone/PC formatters

## Phase 4: Orchestrator Integration

### Update ApplicationOrchestrator

- [x] **Update `UpdateConsoleStatus()` method**
  - [x] Get stats from transformation engine: `var transformationStats = _transformationEngine.GetServiceStats();`
  - [x] Add to `allStats` list alongside phone and PC stats

### Add Keyboard Shortcut

- [x] **Update `RegisterKeyboardShortcuts()` method**
  - [x] Register Alt+T for transformation engine verbosity cycling
  - [x] Add shortcut description to help text

- [x] **Update help text display**
  - [x] Add "Alt+T for Transformation Engine verbosity" to console footer

## Phase 5: Testing & Integration

### Unit Tests

- [ ] **Create `Tests/Utilities/TransformationEngineInfoFormatterTests.cs`**
  - [ ] Test formatter with different verbosity levels
  - [ ] Test table display for invalid and abandoned rules
  - [ ] Test error handling and edge cases
  - [ ] Follow patterns from `PCTrackingInfoFormatterTests.cs`

- [ ] **Update `Tests/Services/TransformationEngineTests.cs`**
  - [ ] Test `GetServiceStats()` method returns correct data
  - [ ] Test statistics tracking in various scenarios
  - [ ] Test status updates during rule loading and transformation

### Integration Testing

- [ ] **Test console display**
  - [ ] Verify transformation engine stats appear in console
  - [ ] Test Alt+T verbosity cycling works
  - [ ] Test stats update correctly during operation

- [ ] **Test hot-reload scenarios**
  - [ ] Test Alt+K with valid config updates stats correctly
  - [ ] Test Alt+K with invalid config shows appropriate errors
  - [ ] Test graceful degradation with broken configs

### Documentation

- [ ] **Update keyboard shortcuts documentation**
  - [ ] Add Alt+T to any help text or documentation
  - [ ] Update console UI section in project documentation if needed

---

## Dependencies & Notes

- **Phase 1 must complete** before Phase 2 (models needed for implementation)
- **Phase 2 must complete** before Phase 3 (stats provider needed for formatter)
- **Phase 3 must complete** before Phase 4 (formatter needed for display)
- **Phases 1-4 should complete** before Phase 5 (need working implementation to test)

## Acceptance Criteria

✅ **Feature Complete When:**
- Transformation engine statistics display in console UI
- Alt+T cycles through verbosity levels (Basic → Normal → Detailed)
- Invalid and abandoned rules show in tables
- Hot-reload attempts and successes are tracked
- Status reflects current engine health (Ready/Partial/Error states)
- All existing functionality continues to work unchanged 