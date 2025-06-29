# Configuration Consolidation Plan

## Current Implementation Status

**Overall Progress: Pre-work Phase COMPLETE (4/4 tasks complete) ✅**

**Pre-work Tasks:**
- ✅ **Task 1 COMPLETE**: Remove unused Configure methods 
- ✅ **Task 2 COMPLETE**: Remove inappropriate Save method calls
- ✅ **Task 3 COMPLETE**: Eliminate command-line arguments and simplify TransformationEngine interface
- ✅ **Task 4 COMPLETE**: No additional pre-work items were discovered

**Main Phases:** Ready to begin! 🚀

---

## Overview

This document outlines the plan to consolidate SharpBridge's fragmented configuration system into a cleaner, more maintainable structure. The current system has multiple configuration files and command-line arguments that are no longer necessary, and many settings that were originally configurable are now "figured out" and don't need user configuration.

**Key Insight**: Instead of creating duplicate "section" classes, we'll reuse our existing, tested configuration classes (`VTubeStudioPCConfig`, `VTubeStudioPhoneClientConfig`, etc.) as sections within a consolidated `ApplicationConfig`. This minimizes code changes while achieving all consolidation benefits.

## Current State Analysis

### Current Configuration Files
1. **VTubeStudioPCConfig.json** - PC client settings (host, port, auth, timeouts)
2. **VTubeStudioPhoneConfig.json** - Phone client settings (IP, ports, intervals)
3. **ApplicationConfig.json** - General app settings (editor, shortcuts)
4. **TransformationEngineConfig.json** - Engine settings (config path, max iterations)
5. **vts_transforms.json** - Transformation rules (stays separate)

### Issues Identified
- **Fragmented Configuration**: Settings spread across multiple files
- **Unnecessary Complexity**: Many settings are now "figured out" and don't need user configuration
- **No User Preferences**: No way to configure runtime preferences like verbosity, console size, etc.
- **Inconsistent Structure**: Some configs use sections, others don't

## Target State

### Final Configuration Structure
1. **ApplicationConfig.json** - Main application configuration with sections:
   - `GeneralSettings` - General app settings (editor, shortcuts) - reuses current ApplicationConfig
   - `PhoneClient` - Phone client settings - reuses existing VTubeStudioPhoneClientConfig
   - `PCClient` - PC client settings - reuses existing VTubeStudioPCConfig  
   - `TransformationEngine` - Engine settings (config path, max iterations) - new TransformationEngineConfig
2. **UserPreferences.json** - User preferences (verbosity levels, console size, etc.)
3. **vts_transforms.json** - Transformation rules (unchanged)

### Benefits
- **Simplified Configuration**: Single file for all application settings
- **Reuses Existing Classes**: Leverages existing, tested configuration classes as sections
- **Preserves Hot Reload**: Existing ConfigManager interface maintained for seamless hot reload functionality
- **User Preferences**: Separate file for user-specific settings that can be easily reset
- **Better User Experience**: Simplified configuration management
- **Consistent Structure**: Everything uses sections for extensibility
- **Minimal Code Changes**: Existing config classes become sections without duplication
- **No Runtime Config Saves**: Clean separation - only user preferences saved at runtime
- **Easy Reset**: User preferences can be wiped without affecting application settings

## Implementation Plan

### Pre-work: Housekeeping Tasks (Low Risk)
**Goal**: Clean up existing inappropriate configuration usage and simplify TransformationEngine interface before starting the main refactoring.

#### Steps:
1. **✅ COMPLETED - Remove unused Configure methods** from ServiceRegistration.cs:
   - ✅ Removed `ConfigureVTubeStudioPhoneClient` method (lines 189-205)
   - ✅ Removed `ConfigureVTubeStudioPC` method (lines 211-228)
   - ✅ These methods inappropriately saved config changes back to files during startup
2. **✅ COMPLETED - Remove inappropriate Save method calls** (verified none exist in production code)
3. **✅ COMPLETED - Simplify TransformationEngine interface and centralize config paths**:
   - ✅ Created `TransformationEngineConfig` class with `ConfigPath` and `MaxEvaluationIterations` properties
   - ✅ Updated `ITransformationEngine.LoadRulesAsync()` to be parameterless (gets path from injected config)
   - ✅ Updated `TransformationEngine` implementation to use config and parameterless LoadRulesAsync()
   - ✅ Updated `ApplicationOrchestrator` to eliminate config path parameter and storage (removed `_transformConfigPath` field, parameterless `InitializeAsync`)
   - ✅ Clean up path-passing through multiple service layers (config paths now centralized in Program.cs)
4. **✅ COMPLETED - Additional pre-work items discovered during refactoring** (no additional items were needed)

### Phase 1: Create New Configuration Structure (Low Risk)
**Goal**: Define the new consolidated configuration structure by reusing existing classes.

#### Steps:
1. **Rename ApplicationConfig to GeneralSettingsConfig**:
   ```csharp
   // Rename Models/ApplicationConfig.cs to Models/GeneralSettingsConfig.cs
   public class GeneralSettingsConfig
   {
       public string EditorCommand { get; set; } = "notepad.exe \"%f\"";
       public Dictionary<string, string> Shortcuts { get; set; } = new();
   }
   ```

2. **Create new ApplicationConfig that aggregates existing classes**:
   ```csharp
   public class ApplicationConfig
   {
       public GeneralSettingsConfig GeneralSettings { get; set; } = new();
       public VTubeStudioPhoneClientConfig PhoneClient { get; set; } = new();
       public VTubeStudioPCConfig PCClient { get; set; } = new();
       public TransformationEngineConfig TransformationEngine { get; set; } = new();
   }
   ```

3. **Create UserPreferences class** with separate verbosity levels:
   ```csharp
   public class UserPreferences
   {
       // Verbosity levels for each section
       public VerbosityLevel PhoneClientVerbosity { get; set; } = VerbosityLevel.Normal;
       public VerbosityLevel PCClientVerbosity { get; set; } = VerbosityLevel.Normal;
       public VerbosityLevel TransformationEngineVerbosity { get; set; } = VerbosityLevel.Normal;
       
       // Console preferences
       public int PreferredConsoleWidth { get; set; } = 150;
       public int PreferredConsoleHeight { get; set; } = 60;
   }
   ```

### Phase 2: Update Configuration Manager (Low Risk)
**Goal**: Update ConfigManager to read from consolidated ApplicationConfig.json while maintaining existing interface for hot reload compatibility.

#### Steps:
1. **Update ConfigManager** to read from consolidated file but keep existing methods:
   ```csharp
   public class ConfigManager
   {
       // Keep existing interface for hot reload compatibility
       public async Task<VTubeStudioPCConfig> LoadPCConfigAsync()
       public async Task<VTubeStudioPhoneClientConfig> LoadPhoneConfigAsync()
       public async Task<GeneralSettingsConfig> LoadGeneralSettingsAsync() // Renamed from current LoadApplicationConfigAsync
       public async Task<TransformationEngineConfig> LoadTransformationConfigAsync() // Existing method
       
       // New methods for consolidated config
       public async Task<ApplicationConfig> LoadApplicationConfigAsync() // Returns full consolidated config
       public async Task<UserPreferences> LoadUserPreferencesAsync()
       
       // Only save user preferences at runtime - no config saves
       public async Task SaveUserPreferencesAsync(UserPreferences preferences)
       public async Task ResetUserPreferencesAsync() // Wipe and recreate with defaults
       
       // Keep existing Save methods for load/save symmetry (unused in production)
   }
   ```

2. **Update existing Load methods** to read from consolidated ApplicationConfig.json:
   ```csharp
   public async Task<VTubeStudioPCConfig> LoadPCConfigAsync()
   {
       var appConfig = await LoadApplicationConfigAsync();
       return appConfig.PCClient;
   }
   
   public async Task<GeneralSettingsConfig> LoadGeneralSettingsAsync()
   {
       var appConfig = await LoadApplicationConfigAsync();
       return appConfig.GeneralSettings;
   }
   ```

3. **Add configuration validation** for both files
4. **Create migration utility** to convert old configs to new dual-file structure
5. **Add backward compatibility** - if old config files exist, use them; otherwise use new structure

### Phase 3: Update Service Registration (Low Risk)
**Goal**: Minimal changes to DI registration since existing ConfigManager interface is preserved.

#### Steps:
1. **ServiceRegistration remains largely unchanged** - services continue to get their familiar config types:
   ```csharp
   // These registrations work exactly as before
   services.AddSingleton(provider =>
   {
       var configManager = provider.GetRequiredService<ConfigManager>();
       return configManager.LoadPCConfigAsync().GetAwaiter().GetResult();  // Now reads from consolidated file
   });
   
   services.AddSingleton(provider =>
   {
       var configManager = provider.GetRequiredService<ConfigManager>();
       return configManager.LoadPhoneConfigAsync().GetAwaiter().GetResult(); // Now reads from consolidated file
   });
   ```

2. **Add UserPreferences registration**:
   ```csharp
   services.AddSingleton(provider =>
   {
       var configManager = provider.GetRequiredService<ConfigManager>();
       return configManager.LoadUserPreferencesAsync().GetAwaiter().GetResult();
   });
   ```

3. **Update TransformationEngine registration** to include TransformationEngineConfig dependency (already created in pre-work)
4. **Update ApplicationOrchestrator registration** to include UserPreferences dependency and remove config path parameter
5. **Update tests** to work with new configuration structure

### Phase 4: Update Client Services (Low Risk)
**Goal**: Services continue using their existing config classes, now loaded from consolidated ApplicationConfig.

#### Steps:
1. **VTubeStudioPhoneClient** - No changes needed (still receives `VTubeStudioPhoneClientConfig`)
2. **VTubeStudioPCClient** - No changes needed (still receives `VTubeStudioPCConfig`)
3. **TransformationEngine** - Already updated in pre-work (now uses `TransformationEngineConfig` and parameterless `LoadRulesAsync()`)
4. **Update ApplicationOrchestrator** to use UserPreferences (config path handling already removed in pre-work)
5. **Add configuration validation** for the new structure

### Phase 5: Add User Preferences Management (Low Risk)
**Goal**: Implement runtime preference changes and persistence.

#### Steps:
1. **Add preference change methods** with persistence
2. **Implement preference reset functionality**
3. **Add keyboard shortcuts** for changing preferences
4. **Update console UI** to reflect current preferences
5. **Add preference validation** and error handling

### Phase 6: Remove Old Configuration Files (Low Risk)
**Goal**: Clean up old configuration files and unused loading methods.

#### Steps:
1. **Remove old separate configuration files** (VTubeStudioPCConfig.json, VTubeStudioPhoneConfig.json, TransformationEngineConfig.json)
2. **Update ConfigManager to remove individual file paths** (keep load/save methods for API symmetry - they'll work with consolidated file)
3. **Update documentation** to reflect new structure
4. **Create migration guide** for users

## File Structure Examples

### ApplicationConfig.json
```json
{
  "GeneralSettings": {
    "EditorCommand": "\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%f\"",
    "Shortcuts": {
      "CycleTransformationEngineVerbosity": "Alt+{",
      "CyclePCClientVerbosity": "Alt+K",
      "CyclePhoneClientVerbosity": "Alt+C",
      "ReloadTransformationConfig": "Alt+Q",
      "OpenConfigInEditor": "Ctrl+Alt+W",
      "ShowSystemHelp": "F2"
    }
  },
  "PhoneClient": {
    "IphoneIpAddress": "192.168.1.178",
    "IphonePort": 21412,
    "LocalPort": 28964,
    "RequestIntervalSeconds": 3,
    "SendForSeconds": 4,
    "ReceiveTimeoutMs": 100
  },
  "PCClient": {
    "Host": "localhost",
    "Port": 8001,
    "PluginName": "SharpBridge",
    "PluginDeveloper": "SharpBridge Developer",
    "TokenFilePath": "auth_token.txt",
    "ConnectionTimeoutMs": 5000,
    "ReconnectionDelayMs": 2000,
    "UsePortDiscovery": true
  },
  "TransformationEngine": {
    "ConfigPath": "Configs/vts_transforms.json",
    "MaxEvaluationIterations": 10
  }
}
```

### UserPreferences.json
```json
{
  "PhoneClientVerbosity": "Normal",
  "PCClientVerbosity": "Normal",
  "TransformationEngineVerbosity": "Normal",
  "PreferredConsoleWidth": 150,
  "PreferredConsoleHeight": 60
}
```

## Risk Mitigation Strategy

1. **Backward Compatibility**: Each phase maintains compatibility with existing configuration
2. **Incremental Testing**: Each phase includes comprehensive testing
3. **Rollback Plan**: Each phase can be reverted if issues arise
4. **Documentation**: Clear migration guides for each phase

## Migration Strategy

### For Existing Users
1. **Automatic Migration**: ConfigManager will automatically migrate old configs to new format
2. **Backward Compatibility**: Old config files will continue to work during transition
3. **Migration Guide**: Clear documentation on new configuration structure
4. **Reset Capability**: Easy way to reset user preferences if needed

### For New Users
1. **Default Configuration**: New installations get clean, well-structured configs
2. **Simplified Setup**: No command-line arguments needed
3. **Clear Documentation**: Easy-to-understand configuration structure

## Future Extensibility

The section-based approach makes it easy to add new configuration areas:

1. **New Sections**: Simply add new section classes to ApplicationConfig
2. **New Preferences**: Add properties to UserPreferences
3. **Validation**: Each section can have its own validation logic
4. **Documentation**: Each section can be documented independently

## Success Criteria

- [ ] All configuration consolidated into two files
- [ ] No command-line arguments required
- [ ] User preferences can be easily reset
- [ ] Backward compatibility maintained during transition
- [ ] All existing functionality preserved
- [ ] Comprehensive test coverage
- [ ] Updated documentation and migration guides
- [ ] Performance not degraded
- [ ] Error handling improved


## Notes

- Each phase should be completed and tested before moving to the next
- User feedback should be gathered during the transition period
- Performance metrics should be monitored throughout the process
- Documentation should be updated incrementally with each phase

## Architecture Decisions

### Decision 1: Preserve ConfigManager Interface

**Key Decision**: Instead of changing the ConfigManager interface to only expose a single `LoadApplicationConfigAsync()` method, we preserve the existing individual load methods (`LoadPCConfigAsync()`, `LoadPhoneConfigAsync()`, etc.) to maintain hot reload compatibility.

**Benefits**:
- ✅ **Hot Reload Preserved**: Existing hot reload mechanisms continue working unchanged
- ✅ **No Service Changes**: Client services continue using familiar config types without modification
- ✅ **Lower Risk**: Minimal changes to DI registration and service dependencies
- ✅ **Backward Compatibility**: Migration path is seamless for existing code

**Implementation**: The individual load methods now read from the consolidated `ApplicationConfig.json` file internally and extract the appropriate sections, while maintaining the same external interface.