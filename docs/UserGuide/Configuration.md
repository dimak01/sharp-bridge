# Configuration Management

Sharp Bridge uses a modular configuration system with hot-reload capabilities. All configuration files are automatically monitored for changes and applied without restarting the application.

## Configuration Files

### ApplicationConfig.json
Main application settings with hot-reload support:
- **GeneralSettings** - Editor command, keyboard shortcuts
- **PhoneClient** - iPhone IP address, ports, connection settings
- **PCClient** - Host, port, discovery settings
- **TransformationEngine** - Config path, evaluation settings

### UserPreferences.json
User-specific display preferences:
- **Verbosity Levels** - Console output detail for each component
- **Display Settings** - Console dimensions, parameter display options
- **UI Preferences** - Color schemes, formatting options

### Parameter Transformations Config JSON
Transformation rules for parameter mapping:
- **Parameter Definitions** - Name, function, min/max values, defaults
- **Mathematical Expressions** - How iPhone tracking data maps to VTube Studio parameters
- **Validation Rules** - Parameter constraints and validation

## Editing Configuration

### Using External Editor
- **Press `Ctrl+Alt+E`** to open the relevant configuration file in your external editor
  - **Main Status Mode**: Opens `Parameter Transformations Config JSON` (transformation rules)
  - **System Help Mode**: Opens `ApplicationConfig.json` (main application settings)
  - **Network Status Mode**: Opens `ApplicationConfig.json` (main application settings)

<img src="../Resources/editor-workflow.gif" alt="External Editor Workflow" width="100%">

### Manual Editing
- **Edit configuration files directly** in your preferred text editor
- **Save changes** - The application will automatically detect and apply changes
- **Watch the console** for confirmation messages about configuration updates

## Applying Changes

### Hot Reload Behavior
- **Application config changes**: Applied immediately (hot reload)
- **User preferences changes**: Applied immediately (hot reload)
- **Transformation rules changes**: Press `Alt+K` to reload

### Change Detection
The application monitors configuration files using file watchers:
- **Automatic detection** - Changes are detected within seconds
- **Validation** - Invalid configurations are rejected with error messages
- **Fallback behavior** - Invalid configurations fall back to last working configuration in memory
- **UI indicator** - Shows whether the latest configuration is loaded ("Up to Date: Yes/No")

## Configuration Validation

### Automatic Validation
- **JSON syntax** - Ensures valid JSON format
- **Required fields** - Verifies all necessary configuration options are present
- **Value ranges** - Validates numeric values are within acceptable ranges
- **File paths** - Confirms referenced files exist and are accessible

### Error Handling
- **Invalid JSON** - Falls back to last working configuration in memory
- **Missing files** - Application creates default configurations automatically
- **Invalid values** - Specific error messages indicate what needs to be corrected
- **UI feedback** - "Up to Date: No" indicator shows when using cached configuration

## Common Configuration Tasks

### Changing iPhone IP Address
1. **Open System Help Mode** (F1)
2. **Press `Ctrl+Alt+E`** to edit ApplicationConfig.json
3. **Update `PhoneClient.IphoneIpAddress`** with new IP
4. **Save file** - Changes applied immediately

### Modifying Transformation Rules
1. **Open Main Status Mode** (default mode)
2. **Press `Ctrl+Alt+E`** to edit Parameter Transformations Config JSON
3. **Edit parameter definitions** as needed
4. **Press `Alt+K`** to reload transformations

### Adjusting Display Preferences
1. **Open System Help Mode** (F1)
2. **Press `Ctrl+Alt+E`** to edit ApplicationConfig.json
3. **Modify `UserPreferences`** section
4. **Save file** - Changes applied immediately

### Setting Up External Editor
1. **Open System Help Mode** (F1)
2. **Press `Ctrl+Alt+E`** to edit ApplicationConfig.json
3. **Update `GeneralSettings.EditorCommand`** with your preferred editor
4. **Save file** - New editor will be used for future `Ctrl+Alt+E` commands

## Configuration Backup

### Configuration State Management
- **In-memory fallback** - Invalid configurations fall back to last working configuration in memory
- **UI indicators** - "Up to Date: Yes/No" shows whether latest config is loaded
- **Cached rules** - Transformation rules fall back to cached version if loading fails
- **Save-time backups** - Application creates temporary backups during save operations


## Troubleshooting Configuration Issues

### Common Problems
- **Changes not applying** - Check file permissions, ensure editor saves properly
- **Invalid JSON errors** - Use a JSON validator to check syntax
- **File not found errors** - Verify file paths in configuration
- **Permission denied** - Run application as administrator if needed

### Recovery Steps
1. **Check console messages** for specific error details
2. **Verify file syntax** using a JSON validator
3. **Restore from backup** if configuration is corrupted
4. **Restart application** if hot reload fails

## Advanced Configuration

For advanced configuration topics, see the dedicated documentation:

### **[Transformations](AdvancedFeatures/Transformations.md)**
- **Parameter mapping** - How to create custom parameter transformations
- **Mathematical expressions** - Complex formulas and advanced techniques
- **Expression examples** - Common patterns and best practices

### **[External Editor](AdvancedFeatures/ExternalEditor.md)**
- **Editor integration** - Setting up your preferred text editor
- **Hot reload** - Automatic configuration updates
- **File monitoring** - How the application detects changes

### **[Keyboard Shortcuts](AdvancedFeatures/KeyboardShortcuts.md)**
- **Shortcut customization** - Modifying keyboard shortcuts
- **Mode navigation** - Efficient console navigation
- **Workflow optimization** - Power user techniques
