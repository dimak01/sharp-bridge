# Keyboard Shortcuts

Sharp Bridge provides keyboard shortcuts for efficient navigation and control. All shortcuts are context-aware and work from any console mode.

## Available Shortcuts

### Mode Navigation
- **F1** - Toggle System Help Mode
- **F2** - Toggle Network Status Mode
- **Ctrl+C** - Quit application (from any mode)

### Verbosity Controls
- **Alt+T** - Cycle Transformation Engine verbosity (Basic → Normal → Detailed)
- **Alt+P** - Cycle PC Client verbosity (Basic → Normal → Detailed)  
- **Alt+O** - Cycle Phone Client verbosity (Basic → Normal → Detailed)

### Configuration Management
- **Alt+K** - Reload transformation configuration
- **Ctrl+Alt+E** - Open configuration in external editor (context-aware)

## Context-Aware Behavior

### Ctrl+Alt+E (Open Configuration)
The configuration file opened depends on the current console mode:
- **Main Status Mode (default)**: Opens `vts_transforms.json` (transformation rules)
- **System Help Mode (F1)**: Opens `ApplicationConfig.json` (main settings)
- **Network Status Mode (F2)**: Opens `ApplicationConfig.json` (main settings)

## Shortcut Customization

### Configuration File
Shortcuts are defined in `ApplicationConfig.json` under `GeneralSettings.Shortcuts`:

```json
{
  "GeneralSettings": {
    "Shortcuts": {
      "CycleTransformationEngineVerbosity": "Alt+T",
      "CyclePCClientVerbosity": "Alt+P", 
      "CyclePhoneClientVerbosity": "Alt+O",
      "ReloadTransformationConfig": "Alt+K",
      "OpenConfigInEditor": "Ctrl+Alt+E",
      "ShowSystemHelp": "F1",
      "ShowNetworkStatus": "F2"
    }
  }
}
```

### Available Actions
- **CycleTransformationEngineVerbosity** - Cycle transformation engine display verbosity
- **CyclePCClientVerbosity** - Cycle PC client display verbosity
- **CyclePhoneClientVerbosity** - Cycle phone client display verbosity
- **ReloadTransformationConfig** - Reload transformation rules from disk
- **OpenConfigInEditor** - Open appropriate config file in external editor
- **ShowSystemHelp** - Toggle system help mode
- **ShowNetworkStatus** - Toggle network status mode

### Disabling Shortcuts
To disable a shortcut, set its value to an empty string or remove it from the configuration:

```json
{
  "GeneralSettings": {
    "Shortcuts": {
      "CycleTransformationEngineVerbosity": ""
    }
  }
}
```

## Shortcut Format

### Supported Formats
- **Function keys**: `F1`, `F2`, `F3`, etc.
- **Letter keys**: `A`, `B`, `C`, etc.
- **Number keys**: `D1`, `D2`, `D3`, etc.
- **Modifier combinations**: `Ctrl+Alt+E`, `Alt+K`, `Shift+F1`

### Modifier Keys
- **Ctrl** - Control key
- **Alt** - Alt key  
- **Shift** - Shift key
- **None** - No modifiers (for function keys)

## Troubleshooting

### Shortcuts Not Working
1. **Check focus** - Ensure console window has focus
2. **Verify configuration** - Check `ApplicationConfig.json` syntax
3. **Restart application** - Reload configuration if needed
4. **Check conflicts** - Ensure no duplicate shortcut assignments

### Configuration Issues
1. **Invalid format** - Use correct key names and modifier syntax
2. **JSON syntax** - Validate JSON structure
3. **File permissions** - Ensure application can read configuration
4. **Hot reload** - Configuration changes apply immediately

### Editor Not Opening
1. **Check editor path** - Verify `EditorCommand` in `ApplicationConfig.json`
2. **Test manually** - Run editor command from command line
3. **Check permissions** - Ensure application can launch editor
4. **Use absolute paths** - Avoid relative paths in editor command

## Efficiency Tips

### Quick Navigation
1. **Use F1-F2** for fast mode switching
2. **Memorize Alt+K** for quick transformation reloads
3. **Use Ctrl+Alt+E** for configuration editing
4. **Keep Ctrl+C handy** for quick application exit

### Workflow Optimization
1. **Start in Main Status** - Begin monitoring
2. **Switch to System Help** - Check configuration details
3. **Use Network Status** - Troubleshoot connections
4. **Edit configurations** - Use external editor integration
5. **Reload changes** - Apply modifications quickly

### Common Workflows
- **Configuration editing**: F1 → Ctrl+Alt+E → Edit → Save → Alt+K
- **Troubleshooting**: F2 → Check network → F1 → Check config → Main Status → Monitor
- **Verbosity adjustment**: Alt+T/P/O → Cycle through display levels
- **Quick monitoring**: Main Status → Watch parameters → Ctrl+C (when done)