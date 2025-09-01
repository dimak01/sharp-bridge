# Configuration Management & Field-Driven Remediation - Implementation Checklist

## Phase 1: Cleanup and Removal of Old System
**Goal**: Remove old versioning and migration infrastructure that's no longer needed

### Step 1.1: Remove Versioning Infrastructure
- [ ] Remove `Version` properties from all DTOs
- [ ] Remove `IConfigMigrationService` and `ConfigMigrationService`
- [ ] Remove `IConfigMigration` and related interfaces
- [ ] Remove migration chain classes and logic
- [ ] Test: Application still works without versioning

### Step 1.2: Remove Old Validation System
- [ ] Remove `IConfigValidator` and `ConfigValidator` (old system)
- [ ] Remove `MissingField` enum and `ConfigValidationResult` (old system)
- [ ] Remove `IFirstTimeSetupService` and `FirstTimeSetupService` (old system)
- [ ] Update any remaining references to remove old system dependencies
- [ ] Test: No old validation code remains

### Step 1.3: Update Tests
- [ ] Remove tests for old versioning system
- [ ] Remove tests for old validation system
- [ ] Ensure all tests pass after cleanup
- [ ] Test: Test suite is clean and focused

---

## Phase 2: Foundation (Field-Driven Infrastructure)
**Goal**: Implement the core field-driven validation and remediation system

### Step 2.1: Create Core Interfaces and Models
- [ ] Create `ConfigSectionTypes` enum with all section types
- [ ] Create `IConfigSection` interface (marker interface)
- [ ] Create `ConfigFieldState` record for field-level validation
- [ ] Update all existing DTOs to implement `IConfigSection`
- [ ] Test: Verify all DTOs can be cast to `IConfigSection`

### Step 2.2: Create Factory Interfaces
- [ ] Create `IConfigSectionValidator` interface (non-generic)
- [ ] Create `IConfigSectionFirstTimeSetupService` interface (non-generic)
- [ ] Create `IConfigSectionValidatorsFactory` interface
- [ ] Create `IConfigSectionFirstTimeSetupFactory` interface
- [ ] Test: Verify interfaces compile and can be mocked

### Step 2.3: Update ConfigManager Interface
- [ ] Add `LoadSectionAsync<T>()` method to `IConfigManager`
- [ ] Add `SaveSectionAsync<T>()` method to `IConfigManager`
- [ ] Add `GetSectionFieldsAsync<T>()` method to `IConfigManager`
- [ ] Add non-generic versions using `ConfigSectionTypes` enum
- [ ] Test: Verify interface changes compile

---

## Phase 3: Implementation of Field-Driven System
**Goal**: Implement the actual field-driven validation and remediation logic

### Step 3.1: Implement ConfigManager Updates
- [x] Implement `LoadSectionAsync<T>()` methods in `ConfigManager`
- [x] Implement `SaveSectionAsync<T>()` methods in `ConfigManager`
- [x] Implement `GetSectionFieldsAsync<T>()` methods in `ConfigManager`
- [x] Implement non-generic versions using switch expressions
- [x] Test: Verify section loading/saving works correctly

### Step 3.2: Implement Section-Specific Validators
- [x] Create `VTubeStudioPCConfigValidator` implementing `IConfigSectionValidator`
- [x] Create `VTubeStudioPhoneClientConfigValidator` implementing `IConfigSectionValidator`
- [x] Create `GeneralSettingsConfigValidator` implementing `IConfigSectionValidator`
- [x] Create `TransformationEngineConfigValidator` implementing `IConfigSectionValidator`
- [x] Test: Each validator correctly identifies missing/invalid fields

### Step 3.3: Implement Section-Specific Setup Services
- [x] Create `VTubeStudioPCConfigFirstTimeSetup` implementing `IConfigSectionFirstTimeSetupService`
- [x] Create `VTubeStudioPhoneClientConfigFirstTimeSetup` implementing `IConfigSectionFirstTimeSetupService`
- [x] Create `GeneralSettingsConfigFirstTimeSetup` implementing `IConfigSectionFirstTimeSetupService`
- [x] Create `TransformationEngineConfigFirstTimeSetup` implementing `IConfigSectionFirstTimeSetupService`
- [x] Test: Each setup service can fix missing fields correctly

### Step 3.4: Implement Factory Services
- [x] Create `ConfigSectionValidatorsFactory` implementing `IConfigSectionValidatorsFactory`
- [x] Create `ConfigSectionFirstTimeSetupFactory` implementing `IConfigSectionFirstTimeSetupFactory`
- [x] Use DI keyed services or switch-based implementation
- [x] Test: Factories return correct services for each section type

### Step 3.5: Implement Core Remediation Service
- [x] Create `IConfigRemediationService` interface
- [x] Create `ConfigRemediationService` implementation
- [x] Implement type-driven iteration using `ConfigSectionTypes` enum
- [x] Implement section-by-section validation and remediation
- [x] Add proper error handling and logging
- [x] Register service in DI container
- [x] Test: Service compiles and can be resolved from DI

---

## Phase 4: Integration and Refinement
**Goal**: Integrate the field-driven system and refine the user experience

### Step 4.1: Update ConfigRemediationService
- [x] Refactor to use new factory-based approach
- [x] Implement type-driven iteration using `ConfigSectionTypes` enum
- [x] Update to handle section-by-section validation and remediation
- [x] Ensure proper error handling and retry logic
- [ ] Test: Complete flow works end-to-end

### Step 4.2: Update Service Registration
- [x] Register new factory services in DI container
- [x] Register all section-specific validators and setup services
- [x] Update `ConfigRemediationService` registration
- [x] Remove old migration-related registrations
- [x] Test: All services resolve correctly from DI container

### Step 4.3: Refine User Experience
- [ ] Improve progress indicators during remediation
- [ ] Add better error messages and validation feedback
- [ ] Implement graceful cancellation handling
- [ ] Add logging for troubleshooting
- [ ] Test: User experience is polished and error-tolerant

---

## Phase 5: Testing and Validation
**Goal**: Ensure the complete field-driven system works correctly

### Step 5.1: End-to-End Testing
- [ ] Fresh installation with no config files
- [ ] Startup with missing required fields
- [ ] Startup with corrupted config files
- [ ] Runtime config file changes
- [ ] Config file deletion during runtime
- [ ] Test: All scenarios work correctly

### Step 5.2: Error Scenario Testing
- [ ] Invalid user input during remediation
- [ ] Remediation cancellation/interruption
- [ ] File system permission errors
- [ ] Malformed JSON in config files
- [ ] Test: Error handling is robust

---

## Legacy Items (Already Implemented - Review for Removal/Adjustment)

### Migration Infrastructure (Phase 1 from old checklist)
- [x] Add version property to `ApplicationConfig` class with default value
- [x] Add version property to `UserPreferences` class with default value
- [x] Add current version constants to both classes
- [x] Create `IConfigMigrationService` interface
- [x] Create `ConfigMigrationService` implementation with version probing capability
- [x] Create `ConfigLoadResult<T>` model to wrap loaded configs with metadata
- [x] Modify `LoadApplicationConfigAsync()` to use migration service
- [x] Modify `LoadUserPreferencesAsync()` to use migration service
- [x] Register migration service in DI container

### Old Validation System (Phase 2 from old checklist)
- [x] Create `MissingField` enum with relevant field identifiers
- [x] Create `ConfigValidationResult` model
- [x] Create `IConfigValidator` interface
- [x] Create `ConfigValidator` implementation
- [x] Create `IFirstTimeSetupService` interface
- [x] Create console-based `FirstTimeSetupService` implementation
- [x] Add validation check in `InitializeAsync()` before service initialization
- [x] Add first-time setup call when validation fails
- [x] Register setup service in DI container

### Enhanced Loading (Phase 3 from old checklist)
- [x] Improve load-or-create logic to handle missing files consistently
- [x] Add better error handling for corrupted/invalid JSON files
- [x] Ensure uniform behavior for both `ApplicationConfig` and `UserPreferences`
- [x] Add logging for config creation and error scenarios

### Integration and Polish (Phase 5 from old checklist)
- [x] Ensure file watchers start after potential first-time setup saves
- [x] Handle runtime validation failures when configs change
- [x] Add debouncing/suppression for setup-triggered file saves
- [x] Improve error messages throughout the configuration flow
- [x] Add clear progress indicators during first-time setup
- [x] Add helpful validation feedback for user input errors
- [x] Add graceful fallbacks for setup cancellation/failures
- [x] Add logging for troubleshooting configuration issues

---

## Discrepancy Analysis

### Items to Review for Removal
- [ ] **Version properties** - Remove from all DTOs
- [ ] **Migration services** - Remove entire migration infrastructure
- [ ] **Old validation interfaces** - Remove `IConfigValidator`, `ConfigValidator`
- [ ] **Old setup interfaces** - Remove `IFirstTimeSetupService`, `FirstTimeSetupService`
- [ ] **Old validation models** - Remove `MissingField`, `ConfigValidationResult`

### Items to Review for Adjustment
- [ ] **ConfigRemediationService** - Update to use new factory-based approach
- [ ] **Service registration** - Update DI registrations for new system
- [ ] **Tests** - Update all tests to use new approach
- [ ] **Documentation** - Ensure all docs reflect new approach

### Items to Keep (Already Working)
- [x] **ConfigManager core functionality** - Keep load/save methods
- [x] **File watching** - Keep file watcher integration
- [x] **Error handling** - Keep robust error handling patterns
- [x] **Logging** - Keep comprehensive logging
- [x] **Console UI** - Keep console-based user interaction

---

## Next Steps Priority

1. **Phase 1** - Clean up and remove old system
2. **Phase 2** - Create new interfaces and models
3. **Phase 3** - Implement the field-driven system
4. **Phase 4** - Integrate and refine
5. **Phase 5** - Test and validate

**Goal**: Transform from complex versioning system to simple, field-driven remediation system while maintaining all existing functionality.
