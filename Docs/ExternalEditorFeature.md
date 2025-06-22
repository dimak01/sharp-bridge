# External Editor Feature Implementation Plan

## Overview

This document outlines the design and implementation plan for the external editor feature in Sharp Bridge. This feature will allow users to quickly open transformation configuration files in their preferred external editor using a keyboard shortcut.

## Problem Statement

Currently, users cannot edit transformation configurations from within the Sharp Bridge console application. While the application supports hot-reloading of configuration changes (Alt+K), users must manually navigate to the configuration file, open it in an external editor, make changes, save, and then reload. This workflow is cumbersome and interrupts the user experience.

## Solution

Implement a simple but robust external editor integration that:
- Opens the current transformation configuration file in a user-configured external editor
- Uses a keyboard shortcut (Ctrl+Alt+E) for quick access
- Supports configurable editor commands via application configuration
- Follows existing architectural patterns and design principles

## Design Principles

1. **Consistency**: Follow existing configuration management patterns
2. **Simplicity**: Avoid over-engineering with a minimal, focused implementation
3. **Robustness**: Graceful error handling without crashing the application
4. **Extensibility**: Architecture that can easily support opening other config files in the future

## Architecture Overview

### Component Structure

```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│  ApplicationConfig  │    │ ExternalEditorService│    │ ApplicationOrchestrator│
│  + EditorCommand    │◄───┤  + TryOpenFileAsync  │◄───┤  + Keyboard Shortcut  │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
           │                          │                          │
           ▼                          ▼                          ▼
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│    ConfigManager    │    │   Process.Start()   │    │ KeyboardInputHandler│
│ + LoadAppConfigAsync│    │  (External Editor)  │    │ + Ctrl+Alt+E Handler│
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
```

### Data Flow

1. User presses **Ctrl+Alt+E** during application runtime
2. **KeyboardInputHandler** detects the shortcut and triggers the handler
3. **ApplicationOrchestrator** calls **ExternalEditorService.TryOpenFileAsync()**
4. **ExternalEditorService** reads the **ApplicationConfig.EditorCommand**
5. Service replaces `%f` placeholder with the transformation config file path
6. Service executes the command asynchronously (fire-and-forget)
7. External editor opens with the configuration file
8. User can edit, save, and use existing Alt+K to hot-reload changes

## Implementation Details

### 1. ApplicationConfig Model

**File:** `Models/ApplicationConfig.cs`

```csharp
namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for general application settings
    /// </summary>
    public class ApplicationConfig
    {
        /// <summary>
        /// Command to execute when opening files in external editor.
        /// Use %f as placeholder for file path.
        /// </summary>
        public string EditorCommand { get; set; } = "notepad.exe \"%f\"";
    }
}
```

**Default Configuration:** `Configs/ApplicationConfig.json`

```json
{
  "EditorCommand": "notepad.exe \"%f\""
}
```

### 2. ConfigManager Extensions

**File:** `Utilities/ConfigManager.cs`

Add methods following the existing pattern:

```csharp
private readonly string _applicationConfigFilename = "ApplicationConfig.json";

/// <summary>
/// Gets the path to the Application configuration file.
/// </summary>
public string ApplicationConfigPath => Path.Combine(_configDirectory, _applicationConfigFilename);

/// <summary>
/// Loads the Application configuration from file or creates a default one if it doesn't exist.
/// </summary>
/// <returns>The Application configuration.</returns>
public async Task<ApplicationConfig> LoadApplicationConfigAsync()
{
    return await LoadConfigAsync<ApplicationConfig>(ApplicationConfigPath, () => new ApplicationConfig());
}

/// <summary>
/// Saves the Application configuration to file.
/// </summary>
/// <param name="config">The configuration to save.</param>
/// <returns>A task representing the asynchronous save operation.</returns>
public async Task SaveApplicationConfigAsync(ApplicationConfig config)
{
    await SaveConfigAsync(ApplicationConfigPath, config);
}
```

### 3. ExternalEditorService Interface & Implementation

**File:** `Interfaces/IExternalEditorService.cs`

```csharp
namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service for opening files in external editors
    /// </summary>
    public interface IExternalEditorService
    {
        /// <summary>
        /// Attempts to open a file in the configured external editor
        /// </summary>
        /// <param name="filePath">Path to the file to open</param>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        Task<bool> TryOpenFileAsync(string filePath);
    }
}
```

**File:** `Services/ExternalEditorService.cs`

```csharp
namespace SharpBridge.Services
{
    /// <summary>
    /// Service for opening files in external editors
    /// </summary>
    public class ExternalEditorService : IExternalEditorService
    {
        private readonly ApplicationConfig _config;
        private readonly IAppLogger _logger;

        public ExternalEditorService(ApplicationConfig config, IAppLogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Attempts to open a file in the configured external editor
        /// </summary>
        /// <param name="filePath">Path to the file to open</param>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        public async Task<bool> TryOpenFileAsync(string filePath)
        {
            // Implementation details:
            // 1. Validate file exists
            // 2. Replace %f placeholder with actual file path
            // 3. Parse command and arguments
            // 4. Execute Process.Start() asynchronously
            // 5. Log success/failure
            // 6. Return result for caller feedback
        }
    }
}
```

### 4. Dependency Injection Registration

**File:** `ServiceRegistration.cs`

Add to the existing registration pattern:

```csharp
// Register ApplicationConfig
services.AddSingleton(provider => 
{
    var configManager = provider.GetRequiredService<ConfigManager>();
    return configManager.LoadApplicationConfigAsync().GetAwaiter().GetResult();
});

// Register ExternalEditorService
services.AddSingleton<IExternalEditorService, ExternalEditorService>();
```

### 5. ApplicationOrchestrator Integration

**File:** `Services/ApplicationOrchestrator.cs`

```csharp
// Add to constructor dependencies
private readonly IExternalEditorService _externalEditorService;

// Add to RegisterKeyboardShortcuts() method
_keyboardInputHandler.RegisterShortcut(
    ConsoleKey.E, 
    ConsoleModifiers.Control | ConsoleModifiers.Alt, 
    () => _ = OpenTransformationConfigInEditor(),
    "Open transformation config in external editor"
);

// Add new method
/// <summary>
/// Opens the transformation configuration file in the configured external editor
/// </summary>
private async Task OpenTransformationConfigInEditor()
{
    try
    {
        _logger.Info("Opening transformation config in external editor...");
        
        var success = await _externalEditorService.TryOpenFileAsync(_transformConfigPath);
        
        if (success)
        {
            _logger.Info("External editor launched successfully");
        }
        else
        {
            _logger.Warning("Failed to launch external editor");
        }
    }
    catch (Exception ex)
    {
        _logger.ErrorWithException("Error opening transformation config in external editor", ex);
    }
}
```

### 6. Configuration File Location

The ApplicationConfig will use a fixed filename `ApplicationConfig.json` in the same directory as other configuration files. The ApplicationOrchestrator already has access to the transformation config path via the existing `_transformConfigPath` field, which is set during initialization from the command-line arguments.

## Error Handling Strategy

### Graceful Failure Modes

1. **File Not Found**: Log error, don't crash application
2. **Invalid Command**: Log error with helpful message
3. **Editor Launch Failure**: Log error, continue normal operation
4. **Permission Issues**: Log error with suggested solutions

### Logging Strategy

- **Info**: Successful editor launches
- **Warning**: Failed launches with basic details
- **Error**: Exceptions with full stack traces
- **Debug**: Command parsing and execution details

## Security Considerations

### Input Validation

- Validate file path exists before opening
- Sanitize file paths to prevent injection attacks
- Validate editor command format

### Process Execution

- Use `Process.Start()` with proper argument escaping
- Execute as fire-and-forget (don't wait for editor to close)
- Handle process creation exceptions gracefully

## Testing Strategy

### Unit Tests

1. **ExternalEditorService Tests**
   - Command parsing and placeholder replacement
   - Error handling scenarios
   - File validation logic

2. **ConfigManager Tests**
   - ApplicationConfig loading/saving
   - Default config creation
   - JSON serialization/deserialization

3. **Integration Tests**
   - End-to-end keyboard shortcut flow
   - Configuration loading during startup
   - Error recovery scenarios

### Test Coverage Areas

- Valid editor commands with various argument patterns
- Invalid file paths and missing files
- Malformed configuration files
- Process creation failures
- Keyboard shortcut registration and execution

## Future Extensibility

### Planned Extensions

1. **Multiple Config File Support**
   - Alt+Shift+E: Open application config
   - Alt+Ctrl+P: Open PC config
   - Alt+Ctrl+O: Open phone config

2. **Editor Detection**
   - Auto-detect common editors (VS Code, Notepad++, etc.)
   - Provide editor-specific command templates

3. **Platform-Specific Defaults**
   - Windows: Notepad++, VS Code
   - macOS: TextEdit, VS Code
   - Linux: nano, vim, gedit

### Architecture Considerations

The current design supports these extensions without major refactoring:

- `IExternalEditorService` can be extended with file-type-specific methods
- `ApplicationConfig` can include multiple editor commands
- Keyboard shortcuts can be dynamically registered
- ConfigManager pattern scales to additional config types

## Implementation Timeline

### Phase 1: Core Implementation
1. Create ApplicationConfig model and JSON file
2. Extend ConfigManager with ApplicationConfig support (using fixed filename)
3. Implement ExternalEditorService
4. Add DI registration

### Phase 2: Integration
1. Integrate with ApplicationOrchestrator (using existing `_transformConfigPath` field)
2. Add keyboard shortcut registration  
3. Implement error handling and logging

### Phase 3: Testing & Polish
1. Add comprehensive unit tests
2. Add integration tests
3. Update documentation
4. User acceptance testing

## Configuration Examples

### Windows - Notepad++
```json
{
  "EditorCommand": "\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%f\""
}
```

### Windows - VS Code
```json
{
  "EditorCommand": "code \"%f\""
}
```

### Windows - Default Notepad
```json
{
  "EditorCommand": "notepad.exe \"%f\""
}
```

### Complex Command with Line Number
```json
{
  "EditorCommand": "\"C:\\Program Files\\Sublime Text\\sublime_text.exe\" \"%f\":1"
}
```

## Conclusion

This implementation provides a clean, robust solution for external editor integration while maintaining consistency with Sharp Bridge's existing architecture. The design is simple enough to implement quickly but extensible enough to support future enhancements.

The feature will significantly improve user experience by reducing the friction in the edit-test-reload cycle that is central to transformation configuration development. 