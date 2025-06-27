# Shortcut Data Structure Redesign

## Overview

This document outlines the design for replacing the current tuple-based shortcut system with a proper `Shortcut` class and enhanced status tracking. The design emphasizes clean separation of concerns, robust error handling, and excellent debugging support.

## Current System Analysis

### What Works Well
- **Nullable tuple approach**: `Dictionary<ShortcutAction, (ConsoleKey, ConsoleModifiers)?>` with `null` = disabled
- **ShortcutParser formatting**: `FormatShortcut()` handles proper display formatting
- **Configuration-driven approach**: JSON-based shortcut customization
- **Graceful degradation**: Invalid shortcuts are disabled rather than crashing

### What Needs Improvement
- **Tuple limitations**: No extensibility, comparison relies on structural equality hack
- **Lost debugging info**: Invalid shortcut strings are discarded
- **Fragile status display**: String parsing of error messages for status
- **Mixed responsibilities**: Some formatting logic scattered across components

## Design Principles

1. **Clean separation of concerns**: Configuration management ≠ display formatting
2. **Preserve debugging information**: Keep invalid strings for user troubleshooting
3. **Type safety**: Use enums and structured data over magic strings
4. **Minimal breaking changes**: Extend existing interfaces, don't replace them
5. **Single responsibility**: Each component does one thing well

## Core Design

### 1. Shortcut Class

```csharp
public class Shortcut
{
    public ConsoleKey Key { get; }
    public ConsoleModifiers Modifiers { get; }
    
    public Shortcut(ConsoleKey key, ConsoleModifiers modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }
    
    // Convenience method for KeyboardInputHandler
    public static Shortcut FromKeyInfo(ConsoleKeyInfo keyInfo) => 
        new(keyInfo.Key, keyInfo.Modifiers);
}
```

**Design decisions:**
- **Immutable**: Properties are read-only after construction
- **Clean POCO**: No equality methods, no ToString() override
- **Minimal API**: Only essential functionality
- **No formatting responsibility**: Use `ShortcutParser.FormatShortcut()` for display

### 2. Comparison Strategy

```csharp
public class ShortcutComparer : IEqualityComparer<Shortcut>
{
    public static readonly ShortcutComparer Instance = new();
    
    public bool Equals(Shortcut? x, Shortcut? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Key == y.Key && x.Modifiers == y.Modifiers;
    }
    
    public int GetHashCode(Shortcut obj) => HashCode.Combine(obj.Key, obj.Modifiers);
}
```

**Usage:**
```csharp
private readonly Dictionary<Shortcut, (Action Action, string Description)> _shortcuts = 
    new(ShortcutComparer.Instance);
```

### 3. Status Tracking

```csharp
public enum ShortcutStatus
{
    Active,              // Valid shortcut, ready to use
    Invalid,             // Parsing failed, original string preserved
    ExplicitlyDisabled   // User set to "None"/"Disabled"
}
```

**Status definitions:**
- **Active**: Shortcut parsed successfully and registered
- **Invalid**: Parsing failed (bad format, conflicts, etc.)
- **ExplicitlyDisabled**: User intentionally disabled with "None"/"Disabled"

### 4. Enhanced Interface

```csharp
public interface IShortcutConfigurationManager
{
    // Core functionality
    Dictionary<ShortcutAction, Shortcut?> GetMappedShortcuts();
    Dictionary<ShortcutAction, string> GetIncorrectShortcuts();
    void LoadFromConfiguration(ApplicationConfig config);
    Dictionary<ShortcutAction, Shortcut> GetDefaultShortcuts();
    
    // Status tracking
    ShortcutStatus GetShortcutStatus(ShortcutAction action);
}
```

**Method purposes:**
- **`GetMappedShortcuts()`**: Returns active shortcuts (`null` = disabled)
- **`GetIncorrectShortcuts()`**: Returns original invalid strings for debugging
- **`GetDefaultShortcuts()`**: Returns default `Shortcut` objects (not strings)
- **`GetShortcutStatus()`**: Returns enum status (no formatting)

## Implementation Details

### 1. Internal Data Structure

```csharp
public class ShortcutConfigurationManager
{
    private readonly IShortcutParser _parser;
    private readonly IAppLogger _logger;
    
    // Core storage
    private readonly Dictionary<ShortcutAction, Shortcut?> _mappedShortcuts = new();
    
    // Debugging support
    private readonly Dictionary<ShortcutAction, string> _incorrectShortcuts = new();
    private readonly HashSet<ShortcutAction> _explicitlyDisabled = new();
}
```

### 2. Configuration Loading

```csharp
public void LoadFromConfiguration(ApplicationConfig config)
{
    _mappedShortcuts.Clear();
    _incorrectShortcuts.Clear();
    _explicitlyDisabled.Clear();

    var defaultShortcuts = GetDefaultShortcuts();
    var configShortcuts = config?.Shortcuts;
    var usedCombinations = new Dictionary<(ConsoleKey, ConsoleModifiers), ShortcutAction>();

    foreach (var action in Enum.GetValues<ShortcutAction>())
    {
        var actionName = action.ToString();
        
        // Determine source: config vs defaults vs missing
        string? shortcutString;
        if (configShortcuts == null)
        {
            // No config - use defaults
            var defaultShortcut = defaultShortcuts[action];
            shortcutString = _parser.FormatShortcut(defaultShortcut.Key, defaultShortcut.Modifiers);
        }
        else
        {
            // Config exists - only use explicitly defined shortcuts
            shortcutString = configShortcuts.TryGetValue(actionName, out var configValue) ? configValue : null;
        }

        if (string.IsNullOrWhiteSpace(shortcutString))
        {
            _mappedShortcuts[action] = null;
            _explicitlyDisabled.Add(action);
            continue;
        }

        var parsed = _parser.ParseShortcut(shortcutString);
        if (parsed == null)
        {
            _mappedShortcuts[action] = null;
            _incorrectShortcuts[action] = shortcutString;
            continue;
        }

        var (key, modifiers) = parsed.Value;
        var combination = (key, modifiers);

        // Simple conflict resolution: first valid wins
        if (usedCombinations.ContainsKey(combination))
        {
            _mappedShortcuts[action] = null;
            _incorrectShortcuts[action] = shortcutString; // Treat as invalid
            continue;
        }

        // Success
        var shortcut = new Shortcut(key, modifiers);
        _mappedShortcuts[action] = shortcut;
        usedCombinations[combination] = action;
    }
}
```

### 3. Status Method

```csharp
public ShortcutStatus GetShortcutStatus(ShortcutAction action)
{
    if (_mappedShortcuts[action] != null)
        return ShortcutStatus.Active;
        
    if (_incorrectShortcuts.ContainsKey(action))
        return ShortcutStatus.Invalid;
        
    return ShortcutStatus.ExplicitlyDisabled;
}
```

### 4. Default Shortcuts

```csharp
public Dictionary<ShortcutAction, Shortcut> GetDefaultShortcuts()
{
    return new Dictionary<ShortcutAction, Shortcut>
    {
        [ShortcutAction.CycleTransformationEngineVerbosity] = new(ConsoleKey.T, ConsoleModifiers.Alt),
        [ShortcutAction.CyclePCClientVerbosity] = new(ConsoleKey.P, ConsoleModifiers.Alt),
        [ShortcutAction.CyclePhoneClientVerbosity] = new(ConsoleKey.O, ConsoleModifiers.Alt),
        [ShortcutAction.ReloadTransformationConfig] = new(ConsoleKey.K, ConsoleModifiers.Alt),
        [ShortcutAction.OpenConfigInEditor] = new(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt),
        [ShortcutAction.ShowSystemHelp] = new(ConsoleKey.F1, ConsoleModifiers.None)
    };
}
```

## Integration Points

### 1. KeyboardInputHandler Updates

```csharp
public class KeyboardInputHandler : IKeyboardInputHandler
{
    // Replace tuple-based dictionary
    private readonly Dictionary<Shortcut, (Action Action, string Description)> _shortcuts = 
        new(ShortcutComparer.Instance);
        
    public void CheckForKeyboardInput()
    {
        if (!Console.KeyAvailable) return;
        
        try
        {
            var keyInfo = Console.ReadKey(true);
            var pressed = Shortcut.FromKeyInfo(keyInfo);
            
            if (_shortcuts.TryGetValue(pressed, out var shortcut))
            {
                _logger.Debug("Executing shortcut: {0}", _shortcutParser.FormatShortcut(pressed.Key, pressed.Modifiers));
                shortcut.Action();
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorWithException("Error processing keyboard input", ex);
        }
    }
    
    public void RegisterShortcut(ConsoleKey key, ConsoleModifiers modifiers, Action action, string description)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be null or whitespace", nameof(description));

        var shortcut = new Shortcut(key, modifiers);
        _shortcuts[shortcut] = (action, description);
        
        _logger.Debug("Registered shortcut: {0} - {1}", _shortcutParser.FormatShortcut(key, modifiers), description);
    }
    
    public (ConsoleKey Key, ConsoleModifiers Modifiers, string Description)[] GetRegisteredShortcuts()
    {
        return _shortcuts.Select(s => (s.Key.Key, s.Key.Modifiers, s.Value.Description)).ToArray();
    }
}
```

**Note**: Need to inject `IShortcutParser` into `KeyboardInputHandler` for logging.

### 2. ApplicationOrchestrator Updates

```csharp
private void RegisterKeyboardShortcuts()
{
    var mappedShortcuts = _shortcutConfigurationManager.GetMappedShortcuts();

    foreach (var (action, shortcut) in mappedShortcuts)
    {
        if (shortcut == null)
        {
            _logger.Debug("Skipping disabled shortcut for action: {0}", action);
            continue;
        }

        var actionMethod = GetActionMethod(action);
        var description = GetActionDescription(action);

        if (actionMethod != null)
        {
            _keyboardInputHandler.RegisterShortcut(shortcut.Key, shortcut.Modifiers, actionMethod, description);
            _logger.Debug("Registered shortcut {0} for action: {1}", 
                _shortcutParser.FormatShortcut(shortcut.Key, shortcut.Modifiers), action);
        }
    }
}
```

**Note**: Need to inject `IShortcutParser` into `ApplicationOrchestrator` for logging.

### 3. SystemHelpRenderer Updates

```csharp
public string RenderKeyboardShortcuts(int consoleWidth)
{
    var builder = new StringBuilder();
    var mappedShortcuts = _shortcutConfigurationManager.GetMappedShortcuts();

    var shortcutRows = new List<ShortcutDisplayRow>();

    foreach (var (action, shortcut) in mappedShortcuts)
    {
        var row = new ShortcutDisplayRow
        {
            Action = GetActionDisplayName(action),
            Shortcut = GetShortcutDisplayString(action),
            Status = GetStatusDisplayString(_shortcutConfigurationManager.GetShortcutStatus(action))
        };
        shortcutRows.Add(row);
    }

    // Sort by action name for consistent display
    shortcutRows = shortcutRows.OrderBy(r => r.Action).ToList();

    // Create table columns
    var columns = new List<ITableColumn<ShortcutDisplayRow>>
    {
        new TextColumn<ShortcutDisplayRow>("Action", r => r.Action, 30, 50),
        new TextColumn<ShortcutDisplayRow>("Shortcut", r => r.Shortcut, 12, 20),
        new TextColumn<ShortcutDisplayRow>("Status", r => r.Status, 15, 40)
    };

    // Use TableFormatter to create the shortcuts table
    _tableFormatter.AppendTable(
        builder,
        "KEYBOARD SHORTCUTS:",
        shortcutRows,
        columns,
        targetColumnCount: 1,
        consoleWidth: consoleWidth,
        singleColumnBarWidth: 20
    );

    return builder.ToString();
}

private string GetShortcutDisplayString(ShortcutAction action)
{
    var mappedShortcuts = _shortcutConfigurationManager.GetMappedShortcuts();
    var incorrectShortcuts = _shortcutConfigurationManager.GetIncorrectShortcuts();
    
    if (mappedShortcuts[action] != null)
    {
        var shortcut = mappedShortcuts[action]!;
        return _shortcutParser.FormatShortcut(shortcut.Key, shortcut.Modifiers);
    }
    
    if (incorrectShortcuts.TryGetValue(action, out var invalidString))
    {
        return $"{invalidString} (Invalid)";
    }
    
    return "None";
}

private string GetStatusDisplayString(ShortcutStatus status)
{
    return status switch
    {
        ShortcutStatus.Active => "✓ Active",
        ShortcutStatus.Invalid => "✗ Invalid format",
        ShortcutStatus.ExplicitlyDisabled => "✗ Disabled",
        _ => "✗ Unknown"
    };
}
```

## Conflict Resolution Strategy

### Simple First-Wins Approach
1. **Parse shortcuts in enum order**: Predictable precedence
2. **First valid shortcut wins**: Subsequent duplicates become invalid
3. **Clear user feedback**: Show original invalid string with "(Invalid)" suffix
4. **No complex tracking**: Eliminates need for detailed conflict analysis

### Example Scenarios

**Duplicate shortcuts:**
```json
{
  "CycleTransformationEngineVerbosity": "Alt+T",
  "CyclePCClientVerbosity": "Alt+T"
}
```
Result:
- First: `Alt+T` (Active)
- Second: `Alt+T (Invalid)` (Invalid)

**Invalid format:**
```json
{
  "CycleTransformationEngineVerbosity": "InvalidShortcut"
}
```
Result: `InvalidShortcut (Invalid)` (Invalid)

## Benefits

### 1. Clean Architecture
- **Configuration manager**: Manages shortcuts, returns status enums
- **ShortcutParser**: Handles all formatting (existing functionality)
- **SystemHelpRenderer**: Converts enums to display strings
- **KeyboardInputHandler**: Uses Shortcut objects for lookup

### 2. Excellent Debugging
- **Preserved invalid strings**: Users see exactly what they typed wrong
- **Clear status indicators**: Visual feedback about shortcut state
- **Contextual error messages**: Status explains why shortcut is disabled

### 3. Type Safety
- **Enum-based status**: Compile-time safety, easy to extend
- **Structured data**: Shortcut class provides clear contract
- **No magic strings**: All status handling uses enums

### 4. Extensibility
- **Easy to add status types**: Just extend enum and display switch
- **Easy to add shortcut properties**: Extend Shortcut class
- **Easy to change display**: Modify only SystemHelpRenderer

## Migration Path

### Phase 1: Add Infrastructure
1. Create `Shortcut` class and `ShortcutComparer`
2. Add `ShortcutStatus` enum
3. Update `IShortcutConfigurationManager` interface

### Phase 2: Update Configuration Manager
1. Add internal tracking fields
2. Implement new methods
3. Update configuration loading logic

### Phase 3: Update Display Layer
1. Modify SystemHelpRenderer to use new methods
2. Remove old status parsing logic
3. Add enum-to-string conversion

### Phase 4: Update Input Handler
1. Replace tuple dictionary with Shortcut dictionary
2. Update registration and lookup logic
3. Add ShortcutParser dependency for logging

### Phase 5: Update ApplicationOrchestrator
1. Use new GetMappedShortcuts() return type
2. Add ShortcutParser dependency for logging
3. Update shortcut registration calls

## Testing Strategy

### Unit Tests
- **Shortcut class**: Construction, FromKeyInfo() method
- **ShortcutComparer**: Equality and hash code behavior
- **Status methods**: All status scenarios with proper enum returns
- **Display methods**: Proper formatting using ShortcutParser

### Integration Tests
- **Configuration loading**: Various config scenarios and conflict resolution
- **Keyboard input**: End-to-end shortcut execution with Shortcut objects
- **Help display**: Complete table rendering with all status types

### Edge Cases
- **Empty configurations**: Proper fallback to defaults
- **Invalid JSON**: Graceful degradation
- **Conflicting shortcuts**: First-wins behavior verification
- **Special keys**: Function keys, arrows, etc.

## Conclusion

This design provides a clean, extensible foundation for shortcut management. The key improvements are:

1. **Proper separation of concerns**: Configuration vs. formatting vs. display
2. **Enhanced debugging**: Preserved invalid strings with clear status indicators
3. **Type safety**: Enum-based status system
4. **Clean data structures**: Focused Shortcut class with custom comparer
5. **Leveraged existing functionality**: Uses ShortcutParser for all formatting

The approach maintains backward compatibility while providing a solid foundation for future enhancements. 