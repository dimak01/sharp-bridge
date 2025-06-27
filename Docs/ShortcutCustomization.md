# Shortcut Customization Design Document

## Overview

This document outlines the design and implementation plan for making keyboard shortcuts configurable in Sharp Bridge. Currently, all keyboard shortcuts are hardcoded, which can lead to conflicts with users' global shortcuts and limits customization options.

## Goals

1. **Eliminate hardcoded keyboard shortcuts** - Make all shortcuts configurable through application configuration
2. **Enable user customization** - Allow users to define their own key combinations via JSON configuration
3. **Maintain robustness** - Implement graceful degradation when invalid shortcuts are specified
4. **Provide helpful feedback** - Include a help system to show current mappings and configuration issues

## Design Principles

- **Graceful degradation** - Invalid shortcuts are disabled rather than causing application failure
- **Configuration-driven** - All shortcuts defined in `ApplicationConfig.json`
- **No runtime conflicts** - Simple validation with helpful feedback rather than complex conflict resolution
- **Backward compatibility** - Default shortcuts match current hardcoded values
- **User-friendly feedback** - F1 help system shows current state and any issues

## Current State Analysis

### Hardcoded Shortcuts (to be eliminated)
Currently hardcoded in `ApplicationOrchestrator.RegisterKeyboardShortcuts()`:
- `Alt+T` - Cycle Transformation Engine verbosity
- `Alt+P` - Cycle PC client verbosity  
- `Alt+O` - Cycle Phone client verbosity
- `Alt+K` - Reload transformation configuration
- `Ctrl+Alt+E` - Open transformation config in external editor

### Well-Abstracted Components (to be leveraged)
- `KeyboardInputHandler` - Already provides excellent abstraction layer
- `IKeyboardInputHandler` interface - Clean contract for keyboard processing
- Dynamic shortcut registration - Runtime mapping of keys to actions

## Architecture Components

### 1. Shortcut Action Enumeration
```csharp
public enum ShortcutAction
{
    CycleTransformationEngineVerbosity,
    CyclePCClientVerbosity,
    CyclePhoneClientVerbosity,
    ReloadTransformationConfig,
    OpenConfigInEditor,
    ShowShortcutHelp  // F1 help system
}
```

### 2. Shortcut Parser Interface
```csharp
public interface IShortcutParser
{
    (ConsoleKey Key, ConsoleModifiers Modifiers) ParseShortcut(string shortcutString);
    string FormatShortcut(ConsoleKey key, ConsoleModifiers modifiers);
    bool IsValidShortcut(string shortcutString);
}
```

**Supported Format Examples:**
- `"Alt+T"` - Single modifier + key
- `"Ctrl+Alt+E"` - Multiple modifiers + key
- `"F1"` - Function keys without modifiers
- `"None"` or `"Disabled"` - Explicitly disable shortcut

### 3. Shortcut Configuration Manager Interface
```csharp
public interface IShortcutConfigurationManager
{
    Dictionary<ShortcutAction, (ConsoleKey Key, ConsoleModifiers Modifiers)?> GetMappedShortcuts();
    List<string> GetConfigurationIssues();
    void LoadFromConfiguration(ApplicationConfig config);
}
```

**Responsibilities:**
- Load shortcuts from configuration
- Parse and validate shortcut strings
- Handle graceful degradation for invalid shortcuts
- Track configuration issues for F1 help display

### 4. Updated Application Configuration
```json
{
  "EditorCommand": "notepad.exe \"%f\"",
  "Shortcuts": {
    "CycleTransformationEngineVerbosity": "Alt+T",
    "CyclePCClientVerbosity": "Alt+P",
    "CyclePhoneClientVerbosity": "Alt+O", 
    "ReloadTransformationConfig": "Alt+K",
    "OpenConfigInEditor": "Ctrl+Alt+E",
    "ShowShortcutHelp": "F1"
  }
}
```

## Implementation Phases

### Phase 1: Remove Hardcoding
**Goal:** Eliminate all hardcoded shortcuts while maintaining current functionality

**Tasks:**
1. Create `ShortcutAction` enumeration
2. Implement `IShortcutParser` and basic implementation
3. Implement `IShortcutConfigurationManager` and basic implementation
4. Update `ApplicationConfig` model to include `Shortcuts` dictionary
5. Modify `ApplicationOrchestrator.RegisterKeyboardShortcuts()` to use configuration
6. Update console footer in `ConsoleRenderer` to display dynamic shortcuts
7. Add default shortcuts to `ApplicationConfig.json`

**Success Criteria:**
- No hardcoded `ConsoleKey` or `ConsoleModifiers` values in orchestrator
- Application behaves identically to before with default configuration
- Console footer shows actual mapped shortcuts

### Phase 2: Add Customization & Help System
**Goal:** Enable user customization with helpful feedback

**Tasks:**
1. Add comprehensive validation to shortcut parsing
2. Implement graceful degradation for invalid shortcuts
3. Create F1 help system showing:
   - Current shortcut mappings
   - Any configuration issues/disabled shortcuts
   - Usage instructions
4. Add configuration validation during application startup
5. Add logging for shortcut parsing issues

**Success Criteria:**
- Users can modify `ApplicationConfig.json` shortcuts successfully
- Invalid shortcuts are ignored gracefully (no crashes)
- F1 displays current mappings and any issues
- Clear feedback when shortcuts are disabled due to parsing errors

## Error Handling Strategy

### Graceful Degradation Approach
- **Invalid shortcuts** → Disabled (set to `null` in mapping)
- **Duplicate shortcuts** → Last one wins, others disabled
- **Reserved key combinations** → Disabled with warning
- **Parse failures** → Disabled with specific error message

### User Feedback
- **Console startup** → Log warnings for invalid shortcuts
- **F1 help system** → Show disabled shortcuts with reasons
- **No exceptions** → Application continues running normally

### Example Error Scenarios
1. `"Alt+InvalidKey"` → Disabled, F1 shows "Invalid key name 'InvalidKey'"
2. `"BadFormat"` → Disabled, F1 shows "Could not parse shortcut format"
3. Missing shortcut in config → Uses `null` (disabled)

## Help System Design

### F1 Key Behavior
1. **Display current shortcut mappings** in formatted table
2. **Show disabled shortcuts** with explanatory messages
3. **Provide usage instructions** for customization
4. **Return to normal view** with any key press

### Help Content Structure
```
=== Sharp Bridge Keyboard Shortcuts ===

Active Shortcuts:
  Alt+T    Cycle Transformation Engine verbosity
  Alt+P    Cycle PC client verbosity  
  Alt+O    Cycle Phone client verbosity
  Alt+K    Reload transformation configuration
  F1       Show this help

Disabled Shortcuts:
  [None] - All shortcuts loaded successfully!
  
To customize shortcuts, edit Configs/ApplicationConfig.json
Press any key to return...
```

## Service Registration

New services to register in `ServiceRegistration.cs`:
```csharp
services.AddSingleton<IShortcutParser, ShortcutParser>();
services.AddSingleton<IShortcutConfigurationManager, ShortcutConfigurationManager>();
```

## Testing Strategy

### Unit Tests Required
- `ShortcutParser` - All parsing scenarios (valid/invalid formats)
- `ShortcutConfigurationManager` - Configuration loading and validation
- `ApplicationOrchestrator` - Dynamic shortcut registration
- Integration test - End-to-end shortcut customization

### Test Scenarios
- Valid shortcut formats (`"Alt+T"`, `"Ctrl+Alt+E"`, `"F1"`)
- Invalid formats (`"BadFormat"`, `"Alt+InvalidKey"`)
- Duplicate shortcuts in configuration
- Missing shortcuts (graceful defaults)
- Configuration file parsing errors

## Future Considerations (Explicitly Out of Scope)

The following features were considered but are **not included** in this implementation:

### Phase 3 Features (Not Implemented)
- **Runtime shortcut modification** - Changing shortcuts without editing JSON
- **Shortcut profiles** - Predefined sets like "Developer", "Minimal"
- **Advanced conflict detection** - Complex resolution algorithms
- **Import/export configurations** - Sharing shortcut setups
- **Gesture support** - Mouse gestures alongside keyboard shortcuts

### Rationale
These features add significant complexity without proportional benefit for Sharp Bridge's console-focused use case. The current design provides sufficient customization while maintaining simplicity.

## Success Metrics

- ✅ Zero hardcoded keyboard shortcuts in application code
- ✅ Users can successfully customize shortcuts via JSON configuration
- ✅ Application provides clear feedback for configuration issues
- ✅ F1 help system shows current state and any problems
- ✅ Graceful degradation prevents application crashes from bad configuration
- ✅ Console footer dynamically reflects actual shortcut mappings

## Implementation Notes

- Start with simple parser implementation - can be refined later
- Focus on robustness over feature richness
- Maintain existing user experience as default behavior
- Ensure comprehensive error logging for troubleshooting
- Design interfaces to support future enhancements if needed 