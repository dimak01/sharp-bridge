# Configuration Consolidation Plan

## Overview

This document outlines the plan to consolidate SharpBridge's fragmented configuration system into a cleaner, more maintainable structure. The current system has multiple configuration files and command-line arguments that are no longer necessary, and many settings that were originally configurable are now "figured out" and don't need user configuration.

## Current State Analysis

### Current Configuration Files
1. **VTubeStudioPCConfig.json** - PC client settings (host, port, auth, timeouts)
2. **VTubeStudioPhoneConfig.json** - Phone client settings (IP, ports, intervals)
3. **ApplicationConfig.json** - General app settings (editor, shortcuts)
4. **vts_transforms.json** - Transformation rules (stays separate)
5. **Command-line arguments** for config paths

### Issues Identified
- **Fragmented Configuration**: Settings spread across multiple files
- **Unnecessary Complexity**: Many settings are now "figured out" and don't need user configuration
- **Command-line Arguments**: Unnecessary complexity for config paths
- **No User Preferences**: No way to configure runtime preferences like verbosity, console size, etc.
- **Inconsistent Structure**: Some configs use sections, others don't

## Target State

### Final Configuration Structure
1. **ApplicationConfig.json** - Main application configuration with sections:
   - `PhoneClientSection` - Essential phone settings (IP address only)
   - `PCClientSection` - Essential PC settings (host, port only)
   - `TransformationEngineSection` - Engine settings (config path, etc.)
   - `GeneralSettingsSection` - General app settings (editor, etc.)
   - `KeyboardShortcutsSection` - Keyboard shortcuts
2. **UserPreferences.json** - User preferences (verbosity levels, console size, etc.)
3. **vts_transforms.json** - Transformation rules (unchanged)
4. **No command-line arguments** needed

### Benefits
- **Simplified Configuration**: Single file for all application settings
- **User Preferences**: Separate file for user-specific settings that can be easily reset
- **Better User Experience**: No command-line arguments needed
- **Consistent Structure**: Everything uses sections for extensibility
- **Configurable Paths**: Transformation engine config path is configurable
- **Easy Reset**: User preferences can be wiped without affecting application settings

## Implementation Plan

### Phase 1: Create New Configuration Structure (Low Risk)
**Goal**: Define the new consolidated configuration structure without breaking existing code.

#### Steps:
1. **Create comprehensive section classes**:
   ```csharp
   public class PhoneClientSection
   {
       public string IphoneIpAddress { get; set; } = "192.168.1.178";
       // Only essential settings that users actually need to configure
   }

   public class PCClientSection  
   {
       public string Host { get; set; } = "localhost";
       public int Port { get; set; } = 8001;
       // Only essential settings
   }

   public class TransformationEngineSection
   {
       public string ConfigPath { get; set; } = "Configs/vts_transforms.json";
       public bool EnableHotReload { get; set; } = true;
       public int MaxEvaluationIterations { get; set; } = 10;
   }

   public class GeneralSettingsSection
   {
       public string EditorCommand { get; set; } = "notepad.exe \"%f\"";
       public string LogDirectory { get; set; } = "logs";
       public int LogRetentionDays { get; set; } = 31;
       // Future general settings go here
   }

   public class KeyboardShortcutsSection
   {
       public Dictionary<string, string> Shortcuts { get; set; } = new();
   }
   ```

2. **Create UserPreferences class** with separate verbosity levels:
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
       public double ConsoleUpdateIntervalSeconds { get; set; } = 0.1;
       public bool AutoResizeConsole { get; set; } = true;
       public bool ShowColorCoding { get; set; } = true;
       
       // User-specific preferences that can be easily reset
   }
   ```

3. **Update ApplicationConfig to use all sections**:
   ```csharp
   public class ApplicationConfig
   {
       public PhoneClientSection PhoneClient { get; set; } = new();
       public PCClientSection PCClient { get; set; } = new();
       public TransformationEngineSection TransformationEngine { get; set; } = new();
       public GeneralSettingsSection GeneralSettings { get; set; } = new();
       public KeyboardShortcutsSection KeyboardShortcuts { get; set; } = new();
   }
   ```

### Phase 2: Create Dual-File Configuration Manager (Low Risk)
**Goal**: Update ConfigManager to handle both configuration files while maintaining backward compatibility.

#### Steps:
1. **Update ConfigManager** to handle both files:
   ```csharp
   public class ConfigManager
   {
       public async Task<ApplicationConfig> LoadApplicationConfigAsync()
       public async Task<UserPreferences> LoadUserPreferencesAsync()
       public async Task SaveApplicationConfigAsync(ApplicationConfig config)
       public async Task SaveUserPreferencesAsync(UserPreferences preferences)
       public async Task ResetUserPreferencesAsync() // Wipe and recreate with defaults
   }
   ```

2. **Add configuration validation** for both files
3. **Create migration utility** to convert old configs to new dual-file structure
4. **Add backward compatibility** - if old config files exist, use them; otherwise use new structure

### Phase 3: Update Service Registration (Low Risk)
**Goal**: Modify DI registration to use new configuration structure.

#### Steps:
1. **Modify ServiceRegistration** to load both configuration files
2. **Extract client configs** from ApplicationConfig sections
3. **Pass UserPreferences** to components that need them (ApplicationOrchestrator, ConsoleRenderer)
4. **Update tests** to work with new configuration structure

### Phase 4: Remove Command-Line Arguments (Low Risk)
**Goal**: Eliminate command-line argument parsing and use fixed configuration paths.

#### Steps:
1. **Update Program.cs** to remove command-line parsing
2. **Use TransformationEngine.ConfigPath** from ApplicationConfig.TransformationEngineSection
3. **Remove CommandLineParser** and related classes
4. **Update tests** to remove command-line argument testing

### Phase 5: Update Client Services (Medium Risk)
**Goal**: Modify client services to use configuration from ApplicationConfig sections.

#### Steps:
1. **Update VTubeStudioPhoneClient** to use ApplicationConfig.PhoneClientSection
2. **Update VTubeStudioPCClient** to use ApplicationConfig.PCClientSection
3. **Update TransformationEngine** to use ApplicationConfig.TransformationEngineSection
4. **Update ApplicationOrchestrator** to use UserPreferences
5. **Add configuration validation** for the new structure

### Phase 6: Add User Preferences Management (Low Risk)
**Goal**: Implement runtime preference changes and persistence.

#### Steps:
1. **Add preference change methods** with persistence
2. **Implement preference reset functionality**
3. **Add keyboard shortcuts** for changing preferences
4. **Update console UI** to reflect current preferences
5. **Add preference validation** and error handling

### Phase 7: Remove Old Configuration Files (Low Risk)
**Goal**: Clean up old configuration files and models.

#### Steps:
1. **Remove old configuration models** (VTubeStudioPCConfig, VTubeStudioPhoneClientConfig)
2. **Remove old configuration file loading** from ConfigManager
3. **Update documentation** to reflect new structure
4. **Create migration guide** for users

## File Structure Examples

### ApplicationConfig.json
```json
{
  "PhoneClient": {
    "IphoneIpAddress": "192.168.1.178"
  },
  "PCClient": {
    "Host": "localhost",
    "Port": 8001
  },
  "TransformationEngine": {
    "ConfigPath": "Configs/vts_transforms.json",
    "EnableHotReload": true,
    "MaxEvaluationIterations": 10
  },
  "GeneralSettings": {
    "EditorCommand": "notepad.exe \"%f\"",
    "LogDirectory": "logs",
    "LogRetentionDays": 31
  },
  "KeyboardShortcuts": {
    "CycleTransformationEngineVerbosity": "Alt+T",
    "CyclePCClientVerbosity": "Alt+P",
    "CyclePhoneClientVerbosity": "Alt+O",
    "ReloadTransformationConfig": "Alt+K",
    "OpenConfigInEditor": "Ctrl+Alt+E",
    "ShowSystemHelp": "F1"
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
  "PreferredConsoleHeight": 60,
  "ConsoleUpdateIntervalSeconds": 0.1,
  "AutoResizeConsole": true,
  "ShowColorCoding": true
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

## Timeline

- **Phase 1-2**: 1-2 days (foundation)
- **Phase 3-4**: 1-2 days (DI and command-line removal)
- **Phase 5**: 2-3 days (client service updates)
- **Phase 6**: 1-2 days (preferences management)
- **Phase 7**: 1 day (cleanup)

**Total Estimated Time**: 7-10 days

## Notes

- Each phase should be completed and tested before moving to the next
- User feedback should be gathered during the transition period
- Performance metrics should be monitored throughout the process
- Documentation should be updated incrementally with each phase 