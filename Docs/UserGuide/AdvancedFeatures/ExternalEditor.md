# External Editor Integration

Sharp Bridge integrates with external text editors to provide a seamless configuration editing experience. This feature allows you to use your preferred editor while maintaining hot-reload capabilities.

## How It Works

### Editor Launch Process
1. **Press `Ctrl+Alt+E`** in the appropriate console mode
2. **Application determines** which configuration file to open
3. **External editor launches** with the configuration file loaded
4. **File monitoring** detects changes when you save
5. **Hot reload** applies changes automatically

### Mode-Specific Behavior
- **Main Status Mode (default)**: Opens `vts_transforms.json` (transformation rules)
- **System Help Mode (F1)**: Opens `ApplicationConfig.json` (main settings)
- **Network Status Mode (F2)**: Opens `ApplicationConfig.json` (main settings)

## Configuring Your Editor

### Editor Command Format
The editor command is configured in `ApplicationConfig.json`:
```json
{
  "GeneralSettings": {
    "EditorCommand": "notepad.exe \"%f\""
  }
}
```

### Supported Variables
- **%f** - Full path to the configuration file
- **%d** - Directory containing the configuration file
- **%n** - Filename without extension
- **%e** - File extension

### Popular Editor Configurations

#### Notepad (Default)
```json
"EditorCommand": "notepad.exe \"%f\""
```

#### Notepad++
```json
"EditorCommand": "\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%f\""
```

#### Visual Studio Code
```json
"EditorCommand": "code \"%f\""
```

#### Sublime Text
```json
"EditorCommand": "\"C:\\Program Files\\Sublime Text\\sublime_text.exe\" \"%f\""
```

#### Vim (if available)
```json
"EditorCommand": "vim \"%f\""
```

## Hot Reload System

### How Hot Reload Works
1. **File monitoring** - Application watches configuration files for changes
2. **Change detection** - Modifications are detected within seconds
3. **Validation** - Configuration is validated before applying
4. **Application** - Valid changes are applied immediately
5. **Rollback** - Invalid changes are rejected and previous state restored

### Supported Files
- **ApplicationConfig.json** - Main application settings
- **UserPreferences.json** - User display preferences
- **vts_transforms.json** - Transformation rules (requires Alt+K to reload)

### Hot Reload Behavior
- **Application config** - Applied immediately upon save
- **User preferences** - Applied immediately upon save
- **Transformation rules** - Requires manual reload with Alt+K

## File Monitoring

### Monitoring System
- **File watchers** - Monitor configuration files for changes
- **Change detection** - Detects file modifications, renames, and moves
- **Error handling** - Gracefully handles file access issues
- **Recovery** - Automatically recovers from monitoring failures

### Monitoring Features
- **Real-time detection** - Changes detected within seconds
- **Multiple files** - Monitors all configuration files simultaneously
- **Error recovery** - Automatically restarts monitoring on failures
- **Performance optimized** - Minimal impact on application performance

## Error Handling

### Common Issues
- **Editor not found** - Check editor path and availability
- **Permission denied** - Ensure application has necessary permissions
- **Invalid configuration** - Check JSON syntax and structure
- **File locked** - Ensure editor releases file after saving

### Troubleshooting Steps
1. **Test editor manually** - Run editor command from command line
2. **Check file permissions** - Ensure application can read/write files
3. **Verify JSON syntax** - Use a JSON validator to check configuration
4. **Restart application** - Reload configuration if needed

## Best Practices

### Editor Setup
- **Use absolute paths** - Avoid relative paths in editor command
- **Quote file paths** - Always quote file paths in editor command
- **Test configuration** - Verify editor launches correctly
- **Choose appropriate editor** - Select editor with JSON support

### Configuration Editing
- **Save frequently** - Save changes to trigger hot reload
- **Validate syntax** - Use editor's JSON validation features
- **Test changes** - Verify changes work as expected
- **Backup configurations** - Keep copies of working configurations

### Performance
- **Close editor when done** - Release file locks promptly
- **Avoid simultaneous edits** - Don't edit multiple files simultaneously
- **Use efficient editors** - Choose editors that don't lock files unnecessarily
- **Monitor resource usage** - Watch for high CPU or memory usage

## Advanced Features

### Custom Editor Commands
You can create custom editor commands for specific scenarios:

#### Editor with specific settings
```json
"EditorCommand": "\"C:\\Program Files\\Notepad++\\notepad++.exe\" -n\"%f\""
```

#### Editor with workspace
```json
"EditorCommand": "code -n \"%f\""
```

#### Editor with specific profile
```json
"EditorCommand": "\"C:\\Program Files\\Sublime Text\\sublime_text.exe\" --project \"%d\\project.sublime-project\" \"%f\""
```

### Batch Editing
- **Edit multiple files** - Use external editor for complex changes
- **Version control** - Track configuration changes over time
- **Collaborative editing** - Share configurations with team members
- **Automated editing** - Use scripts for bulk configuration changes

## Troubleshooting

### Editor Won't Launch
1. **Check path** - Verify editor executable exists
2. **Test command** - Run editor command manually
3. **Check permissions** - Ensure application can launch processes
4. **Use full path** - Avoid relying on PATH environment variable

### Changes Not Applying
1. **Check file monitoring** - Ensure file watchers are active
2. **Verify file save** - Confirm editor actually saved the file
3. **Check JSON syntax** - Validate configuration format
4. **Manual reload** - Use Alt+K for transformation rules

### Performance Issues
1. **Check editor** - Some editors may lock files unnecessarily
2. **Monitor resources** - Watch for high CPU or memory usage
3. **Close unused editors** - Release file locks when done
4. **Use efficient editors** - Choose lightweight editors for simple edits

## Integration Tips

### Workflow Optimization
1. **Set up editor** - Configure your preferred editor
2. **Learn shortcuts** - Master Ctrl+Alt+E for quick editing
3. **Use hot reload** - Take advantage of automatic updates
4. **Monitor changes** - Watch console for configuration updates

### Editor Features
- **JSON validation** - Use editor's JSON validation features
- **Syntax highlighting** - Enable JSON syntax highlighting
- **Auto-completion** - Use editor's auto-completion for configuration
- **Search and replace** - Use editor's search features for bulk changes
