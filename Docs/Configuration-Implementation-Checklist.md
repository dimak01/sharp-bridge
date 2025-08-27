# Configuration Management & First-Time Setup - Implementation Checklist

## Phase 1: Foundation (Versioning & Migration Infrastructure)
**Goal**: Add versioning support without changing current behavior

### Step 1.1: Add Version Properties to Current DTOs
- [x] Add version property to `ApplicationConfig` class with default value
- [x] Add version property to `UserPreferences` class with default value
- [x] Add current version constants to both classes
- [x] Test: Verify existing configs still load/save correctly with new version fields

### Step 1.2: Create Migration Infrastructure
- [x] Create `IConfigMigrationService` interface
- [x] Create `ConfigMigrationService` implementation with version probing capability
- [x] Create `ConfigLoadResult<T>` model to wrap loaded configs with metadata
- [x] Test: Can probe version from existing configs, returns current version correctly

### Step 1.3: Update ConfigManager to Use Migration Service
- [x] Modify `LoadApplicationConfigAsync()` to use migration service
- [x] Modify `LoadUserPreferencesAsync()` to use migration service
- [x] Keep current behavior but route through new infrastructure
- [x] Register migration service in DI container
- [x] Test: All existing functionality works unchanged

---

## Phase 2: Validation & First-Time Setup
**Goal**: Add validation and setup flow without breaking existing startup

### Step 2.1: Create Validation Infrastructure
- [x] Create `MissingField` enum with relevant field identifiers
- [x] Create `ConfigValidationResult` model
- [x] Create `IConfigValidator` interface
- [x] Create `ConfigValidator` implementation
- [x] Define validation rules for required fields (phone IP, PC host/port logic)
- [x] Test: Can validate current configs, identifies missing fields correctly

### Step 2.2: Create First-Time Setup Service
- [x] Create `IFirstTimeSetupService` interface
- [x] Create console-based `FirstTimeSetupService` implementation
- [x] Implement prompting logic for each missing field type
- [x] Add input validation and error handling for user entries
- [x] Test: Can prompt for and collect missing fields in isolation

### Step 2.3: Integrate Setup into ApplicationOrchestrator
- [x] Add validation check in `InitializeAsync()` before service initialization
- [x] Add first-time setup call when validation fails
- [x] Ensure file watchers start after potential setup saves
- [x] Keep existing behavior as fallback for validation errors
- [x] Register setup service in DI container
- [x] Test: Normal startup unchanged, setup triggers when fields missing

---

## Phase 3: Enhanced Loading (Load-or-Create)
**Goal**: Improve config creation and error handling

### Step 3.1: Enhance ConfigManager Load-or-Create Logic
- [ ] Improve load-or-create logic to handle missing files consistently
- [ ] Add better error handling for corrupted/invalid JSON files
- [ ] Ensure uniform behavior for both `ApplicationConfig` and `UserPreferences`
- [ ] Add logging for config creation and error scenarios
- [ ] Test: Handles missing/corrupted files gracefully, creates defaults when needed

### Step 3.2: Add Required Field Detection
- [ ] Determine strategy for "unset" vs "default" field detection
- [ ] Update DTOs to support unset detection (nullable strings or sentinel values)
- [ ] Update validation to distinguish between user-set and default values
- [ ] Ensure first-time setup only triggers for truly missing fields
- [ ] Test: Can distinguish between user-set and default values correctly

---

## Phase 4: Migration Support (Future-Proofing)
**Goal**: Add support for future config migrations

### Step 4.1: Create Legacy DTO Structure
- [ ] Create `Models/Legacy/` folder structure
- [ ] Create `Models/Legacy/V1/` subfolder for version 1 DTOs
- [ ] Create example legacy DTOs for testing migration functionality
- [ ] Ensure legacy DTOs are internal/separate from current DTOs
- [ ] Test: Can deserialize legacy configs to old DTO structures

### Step 4.2: Implement Migration Pipeline
- [ ] Create migration function interface/pattern
- [ ] Implement example migration (V1→V2) for testing
- [ ] Add migration chain execution logic
- [ ] Add migration validation and error handling
- [ ] Ensure migrations are idempotent and side-effect-free
- [ ] Test: Can migrate old configs to current version successfully

---

## Phase 5: Integration & Polish
**Goal**: Refine the complete flow and user experience

### Step 5.1: File Watcher Integration
- [ ] Ensure file watchers start after potential first-time setup saves
- [ ] Handle runtime validation failures when configs change
- [ ] Add debouncing/suppression for setup-triggered file saves
- [ ] Test runtime config change scenarios with validation
- [ ] Test: No watcher loops, runtime changes handled correctly

### Step 5.2: Error Handling & User Experience
- [ ] Improve error messages throughout the configuration flow
- [ ] Add clear progress indicators during first-time setup
- [ ] Add helpful validation feedback for user input errors
- [ ] Add graceful fallbacks for setup cancellation/failures
- [ ] Add logging for troubleshooting configuration issues
- [ ] Test: User-friendly experience for common error scenarios

---

## Testing Milestones

### After Phase 1
- [x] All existing configuration loading works unchanged
- [x] Version information is correctly read from config files
- [x] Migration infrastructure exists but doesn't affect current behavior

### After Phase 2
- [x] First-time setup can collect missing required fields
- [x] Validation correctly identifies which fields are missing
- [x] Setup integrates with startup flow without breaking existing paths

### After Phase 3
- [ ] Robust handling of missing, corrupted, or invalid config files
- [ ] Consistent load-or-create behavior across both config types
- [ ] Clear distinction between unset and default values

### After Phase 4
- [ ] Migration pipeline can handle legacy configuration formats
- [ ] Future config version changes are supported
- [ ] Legacy DTO handling is isolated and testable

### After Phase 5
- [ ] Complete end-to-end flow works seamlessly
- [ ] File watching integration doesn't cause issues
- [ ] User experience is polished and error-tolerant

---

## Integration Tests

### End-to-End Scenarios
- [ ] Fresh installation with no config files
- [ ] Startup with missing required fields
- [ ] Startup with corrupted config files
- [ ] Runtime config file changes
- [ ] Config file deletion during runtime
- [ ] Migration from legacy config versions

### Error Scenarios
- [ ] Invalid user input during first-time setup
- [ ] Setup cancellation/interruption
- [ ] File system permission errors
- [ ] Malformed JSON in config files
- [ ] Network connectivity issues during setup

---

## Rollback & Risk Mitigation

### Backward Compatibility Checks
- [ ] Existing config files continue to work without modification
- [ ] No breaking changes to public configuration APIs
- [ ] Graceful degradation when new features fail

### Rollback Preparation
- [ ] Feature flags or configuration switches for new functionality
- [ ] Ability to disable first-time setup if needed
- [ ] Fallback to original loading logic in case of issues
- [ ] Clear documentation for reverting changes

---

## Documentation Updates (Post-Implementation)
- [ ] Update main README with first-time setup information
- [ ] Update ProjectOverview.md with new configuration architecture
- [ ] Document new configuration validation rules
- [ ] Update troubleshooting guide with setup-related issues
- [ ] Create developer documentation for adding new config migrations
