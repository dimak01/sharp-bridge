# TransformationEngine Statistics Implementation Checklist

## Phase 1: Data Models & Core Infrastructure

### Models to Create

- [ ] **Create `Models/TransformationEngineStatus.cs`**
  - [ ] Define enum with values: `Ready`, `Partial`, `ConfigErrorCached`, `NoValidRules`, `NeverLoaded`, `ConfigMissing`
  - [ ] Add XML documentation for each status value

- [ ] **Create `Models/RuleInfo.cs`**
  - [ ] Properties: `string Name`, `string Error`, `string Type` (validation/evaluation)
  - [ ] Constructor for easy creation
  - [ ] XML documentation

- [ ] **Create `Models/TransformationEngineInfo.cs`**
  - [ ] Implement `IFormattableObject`
  - [ ] Properties: `string ConfigFilePath`, `int ValidRulesCount`, `List<RuleInfo> InvalidRules`, `List<RuleInfo> AbandonedRules`
  - [ ] Constructor with parameters
  - [ ] XML documentation

## Phase 2: TransformationEngine Statistics

### Add Statistics Tracking Fields

- [ ] **Add private fields to `TransformationEngine` class**
  - [ ] `private long _totalTransformations = 0;`
  - [ ] `private long _successfulTransformations = 0;`
  - [ ] `private long _failedTransformations = 0;`
  - [ ] `private long _hotReloadAttempts = 0;`
  - [ ] `private long _hotReloadSuccesses = 0;`
  - [ ] `private DateTime _lastSuccessfulTransformation;`
  - [ ] `private DateTime _rulesLoadedTime;`
  - [ ] `private string _lastError;`
  - [ ] `private string _configFilePath;`
  - [ ] `private TransformationEngineStatus _currentStatus = TransformationEngineStatus.NeverLoaded;`
  - [ ] `private readonly List<RuleInfo> _invalidRules = new();`
  - [ ] `private readonly List<RuleInfo> _lastAbandonedRules = new();`

### Implement IServiceStatsProvider

- [ ] **Add `IServiceStatsProvider` to `TransformationEngine` class declaration**

- [ ] **Implement `GetServiceStats()` method**
  - [ ] Create counters dictionary from tracking fields
  - [ ] Create `TransformationEngineInfo` entity with current state
  - [ ] Determine `isHealthy` based on status and recent activity
  - [ ] Return `ServiceStats` with all data

### Update Existing Methods

- [ ] **Update `LoadRulesAsync()` method**
  - [ ] Store `_configFilePath` parameter
  - [ ] Track `_hotReloadAttempts` and `_hotReloadSuccesses`
  - [ ] Populate `_invalidRules` list instead of just counting
  - [ ] Set `_rulesLoadedTime` on successful load
  - [ ] Update `_currentStatus` based on results
  - [ ] Clear `_lastError` on success, set on failure

- [ ] **Update `TransformData()` method**
  - [ ] Increment `_totalTransformations`
  - [ ] Track `_successfulTransformations` vs `_failedTransformations`
  - [ ] Update `_lastSuccessfulTransformation` timestamp
  - [ ] Populate `_lastAbandonedRules` list with evaluation failures
  - [ ] Update `_currentStatus` based on results

## Phase 3: Console UI Integration

### Create Formatter

- [ ] **Create `Utilities/TransformationEngineInfoFormatter.cs`**
  - [ ] Implement `IFormatter` interface
  - [ ] Add constructor taking `IConsole` and `ITableFormatter`
  - [ ] Implement `CurrentVerbosity` property and `CycleVerbosity()` method
  - [ ] Implement `Format(IServiceStats serviceStats)` method
  - [ ] Display rule stats, config info, operational status
  - [ ] Use `ITableFormatter` for invalid rules and abandoned rules tables
  - [ ] Follow patterns from `PCTrackingInfoFormatter`

### Register Formatter

- [ ] **Update `ConsoleRenderer` constructor**
  - [ ] Add `TransformationEngineInfoFormatter` parameter
  - [ ] Store formatter in private field

- [ ] **Update `ConsoleRenderer.RegisterFormatter()` calls**
  - [ ] Register formatter for `TransformationEngineInfo` type in constructor

- [ ] **Update `ConsoleRenderer.Update()` method**
  - [ ] Add special case handling for `TransformationEngineInfoFormatter` like phone/PC formatters

## Phase 4: Orchestrator Integration

### Update ApplicationOrchestrator

- [ ] **Update `UpdateConsoleStatus()` method**
  - [ ] Get stats from transformation engine: `var transformationStats = _transformationEngine.GetServiceStats();`
  - [ ] Add to `allStats` list alongside phone and PC stats

### Add Keyboard Shortcut

- [ ] **Update `RegisterKeyboardShortcuts()` method**
  - [ ] Register Alt+T for transformation engine verbosity cycling
  - [ ] Add shortcut description to help text

- [ ] **Update help text display**
  - [ ] Add "Alt+T for Transformation Engine verbosity" to console footer

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