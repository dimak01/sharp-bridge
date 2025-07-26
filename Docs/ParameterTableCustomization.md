# Parameter Table Customization Feature

## Overview

This feature adds customizable column display for the PC tracking parameter table in the console UI. Users can configure which columns to show via user preferences, allowing them to focus on the information most relevant to their needs.

## Implementation Plan

### 1. Enum Definition

```csharp
public enum ParameterTableColumn
{
    ParameterName,  // Parameter name with color coding
    ProgressBar,    // Visual progress bar representation  
    Value,          // Raw numeric value
    Range,          // Weight and min/default/max information
    Expression      // Transformation expression
}
```

### 2. Configuration Structure

Add to `UserPreferences.json`:

```json
{
  "PhoneClientVerbosity": "Normal",
  "PCClientVerbosity": "Normal",
  "TransformationEngineVerbosity": "Normal",
  "PreferredConsoleWidth": 150,
  "PreferredConsoleHeight": 60,
  "PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value", "Range", "Expression"]
}
```

### 3. Interface Definition

```csharp
// Interfaces/IParameterTableConfigurationManager.cs
public interface IParameterTableConfigurationManager
{
    /// <summary>
    /// Gets the currently configured parameter table columns
    /// </summary>
    /// <returns>Array of parameter table columns to display</returns>
    ParameterTableColumn[] GetParameterTableColumns();

    /// <summary>
    /// Gets the default parameter table columns used when no configuration is provided
    /// </summary>
    /// <returns>Array of default parameter table columns</returns>
    ParameterTableColumn[] GetDefaultParameterTableColumns();

    /// <summary>
    /// Loads parameter table configuration from user preferences
    /// </summary>
    /// <param name="userPreferences">User preferences containing column configuration</param>
    void LoadFromUserPreferences(UserPreferences userPreferences);

    /// <summary>
    /// Gets the display string for a column (for debugging/help purposes)
    /// </summary>
    /// <param name="column">The parameter table column</param>
    /// <returns>Human-readable column name for display</returns>
    string GetColumnDisplayName(ParameterTableColumn column);
}
```

### 4. Model Updates

```csharp
// In UserPreferences.cs
public class UserPreferences
{
    // ... existing properties ...
    
    /// <summary>
    /// Customizable columns for PC parameter table display
    /// </summary>
    public ParameterTableColumn[] PCParameterTableColumns { get; set; } = Array.Empty<ParameterTableColumn>();
}
```

### 5. Configuration Manager Implementation

```csharp
// Utilities/ParameterTableConfigurationManager.cs
public class ParameterTableConfigurationManager : IParameterTableConfigurationManager
{
    private readonly IAppLogger _logger;
    private ParameterTableColumn[] _currentColumns;

    public ParameterTableConfigurationManager(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentColumns = GetDefaultParameterTableColumns();
    }

    public ParameterTableColumn[] GetParameterTableColumns()
    {
        return _currentColumns;
    }

    public ParameterTableColumn[] GetDefaultParameterTableColumns()
    {
        return new[]
        {
            ParameterTableColumn.ParameterName,
            ParameterTableColumn.ProgressBar,
            ParameterTableColumn.Value,
            ParameterTableColumn.Range,
            ParameterTableColumn.Expression
        };
    }

    public void LoadFromUserPreferences(UserPreferences userPreferences)
    {
        if (userPreferences?.PCParameterTableColumns == null || 
            userPreferences.PCParameterTableColumns.Length == 0)
        {
            _logger.Debug("No parameter table columns configured in user preferences, using defaults");
            _currentColumns = GetDefaultParameterTableColumns();
            return;
        }

        // Validate and filter columns
        var validColumns = new List<ParameterTableColumn>();
        var invalidColumns = new List<string>();

        foreach (var column in userPreferences.PCParameterTableColumns)
        {
            if (Enum.IsDefined(typeof(ParameterTableColumn), column))
            {
                validColumns.Add(column);
            }
            else
            {
                invalidColumns.Add(column.ToString());
            }
        }

        if (invalidColumns.Count > 0)
        {
            _logger.Warning("Invalid parameter table columns found: {0}. These will be ignored.", 
                string.Join(", ", invalidColumns));
        }

        if (validColumns.Count == 0)
        {
            _logger.Warning("No valid parameter table columns found, using defaults");
            _currentColumns = GetDefaultParameterTableColumns();
        }
        else
        {
            _currentColumns = validColumns.ToArray();
            _logger.Debug("Loaded {0} parameter table columns from user preferences", validColumns.Count);
        }
    }

    public string GetColumnDisplayName(ParameterTableColumn column)
    {
        return column switch
        {
            ParameterTableColumn.ParameterName => "Parameter Name",
            ParameterTableColumn.ProgressBar => "Progress Bar",
            ParameterTableColumn.Value => "Value",
            ParameterTableColumn.Range => "Range",
            ParameterTableColumn.Expression => "Expression",
            _ => column.ToString()
        };
    }
}
```

### 6. Service Registration

```csharp
// ServiceRegistration.cs
services.AddSingleton<IParameterTableConfigurationManager, ParameterTableConfigurationManager>();
```

### 7. Formatter Logic Updates

Update the `PCTrackingInfoFormatter.cs` constructor and `AppendParameters` method:

```csharp
// In PCTrackingInfoFormatter constructor
public PCTrackingInfoFormatter(IConsole console, ITableFormatter tableFormatter, 
    IParameterColorService colorService, IShortcutConfigurationManager shortcutManager, 
    UserPreferences userPreferences, IParameterTableConfigurationManager columnConfigManager)
{
    // ... existing initialization ...
    _columnConfigManager = columnConfigManager;
    
    // Load initial column configuration
    _columnConfigManager.LoadFromUserPreferences(userPreferences);
}

// In AppendParameters method
private void AppendParameters(StringBuilder builder, PCTrackingInfo trackingInfo)
{
    var parameters = trackingInfo.Parameters.ToList();
    var parametersToShow = parameters.OrderBy(p => p.Id).ToList();

    // Get column configuration from the manager
    var columnConfig = _columnConfigManager.GetParameterTableColumns();

    // Column definitions mapping
    var columnMap = new Dictionary<ParameterTableColumn, ITableColumnFormatter<TrackingParam>>
    {
        [ParameterTableColumn.ParameterName] = new TextColumnFormatter<TrackingParam>("Parameter", param => _colorService.GetColoredCalculatedParameterName(param.Id), minWidth: 8),
        [ParameterTableColumn.ProgressBar] = new ProgressBarColumnFormatter<TrackingParam>("", param => CalculateNormalizedValue(param, trackingInfo), minWidth: 6, maxWidth: 20, _tableFormatter),
        [ParameterTableColumn.Value] = new NumericColumnFormatter<TrackingParam>("Value", param => param.Value, "0.##", minWidth: 6, padLeft: true),
        [ParameterTableColumn.Range] = new TextColumnFormatter<TrackingParam>("Width x Range", param => FormatCompactRange(param, trackingInfo), minWidth: 12, maxWidth: 25),
        [ParameterTableColumn.Expression] = new TextColumnFormatter<TrackingParam>("Expression", param => _colorService.GetColoredExpression(FormatExpression(param, trackingInfo)), minWidth: 15, maxWidth: 90)
    };

    // Filter columns based on configuration
    var columns = columnConfig
        .Where(col => columnMap.ContainsKey(col))
        .Select(col => columnMap[col])
        .ToList();

    // Use existing table formatter logic
    var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : PARAMETER_DISPLAY_COUNT_NORMAL;
    _tableFormatter.AppendTable(builder, "=== Parameters ===", parametersToShow, columns, 2, _console.WindowWidth, 20, singleColumnLimit);

    builder.AppendLine();
    builder.AppendLine($"Total Parameters: {parameters.Count}");
}
```

## Available Columns

| Column Name | Description | Default Display |
|-------------|-------------|-----------------|
| `ParameterName` | Parameter name with color coding | ✓ |
| `ProgressBar` | Visual progress bar representation | ✓ |
| `Value` | Raw numeric value | ✓ |
| `Range` | Weight and min/default/max information | ✓ |
| `Expression` | Transformation expression | ✓ |

## Configuration Examples

### Minimal Display
```json
{
  "PCParameterTableColumns": ["ParameterName", "Value"]
}
```

### Debug Mode
```json
{
  "PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value", "Range", "Expression"]
}
```

### Performance Focus
```json
{
  "PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value"]
}
```

## Benefits

- **Type Safety**: Enum prevents invalid column names
- **IntelliSense**: IDE will suggest valid column names
- **Consistency**: Standardized column naming across codebase
- **Extensibility**: Easy to add new columns to enum
- **Backward Compatible**: Defaults to current behavior if no configuration provided
- **Simple Configuration**: Just array of enum values
- **User Preferences Integration**: Leverages existing UserPreferences infrastructure
- **Graceful Degradation**: Invalid columns are logged and ignored
- **Natural Persistence**: Defaults are automatically saved to JSON on first load

## Implementation Notes

- Invalid column names in configuration are ignored (graceful degradation)
- If no configuration is provided, defaults to current column set
- Existing table formatting logic remains unchanged
- Column width calculations handled by existing `TableFormatter`
- Configuration changes apply immediately via existing UserPreferences save/load system
- Defaults are automatically propagated to saved JSON when first loaded
- Parameter table column configuration is displayed in the system help screen (F2) showing current columns, their order, and status (Default/Custom)

## System Help Integration

The parameter table column configuration is now integrated into the system help screen (accessible via F2). The help screen displays:

- **Current Column Configuration**: Shows all currently active columns in their display order
- **Column Status**: Indicates whether each column is part of the default configuration or a custom selection
- **Order Information**: Displays the position of each column in the table
- **Consistent Formatting**: Uses the same table formatting as other help sections

This provides users with immediate visibility into their current column configuration without needing to check the configuration files directly.

## Future Enhancements

- **Presets**: Quick presets like "Minimal", "Standard", "Debug"
- **Dynamic Shortcuts**: Keyboard shortcuts to toggle specific columns
- **New Column Types**: Additional columns like change rate, timestamps, etc.
- **Hot-Reload**: File watcher support for real-time configuration changes

---

## Action Items Checklist

### Phase 1: Core Infrastructure
- [x] **1.1** Create `ParameterTableColumn` enum in `Models/` directory
- [x] **1.2** Add `PCParameterTableColumns` property to `UserPreferences.cs`
- [x] **1.3** Create `IParameterTableConfigurationManager` interface
- [x] **1.4** Implement `ParameterTableConfigurationManager` class
- [x] **1.5** Add service registration in `ServiceRegistration.cs`

### Phase 2: Integration
- [x] **2.1** Update `PCTrackingInfoFormatter` constructor to accept `IParameterTableConfigurationManager`
- [x] **2.2** Add private field `_columnConfigManager` to `PCTrackingInfoFormatter`
- [x] **2.3** Update `PCTrackingInfoFormatter` constructor to load initial configuration
- [x] **2.4** Modify `AppendParameters` method to use configuration manager
- [x] **2.5** Update `ServiceRegistration.cs` to inject the new dependency
- [x] **2.6** Fix all test files to use updated constructor signature
- [x] **2.7** Verify all tests pass successfully

### Phase 3: Testing
- [x] **3.1** Create unit tests for `ParameterTableConfigurationManager`
- [x] **3.2** Test default column loading when no configuration provided
- [x] **3.3** Test invalid column handling and logging
- [x] **3.4** Test valid column configuration loading
- [x] **3.5** Update existing `PCTrackingInfoFormatter` tests to include new dependency
- [x] **3.6** Test formatter with different column configurations

### Phase 4: Configuration & Documentation
- [ ] **4.1** Update `Configs/UserPreferences.json` with example configuration
- [ ] **4.2** Test default propagation to saved JSON
- [ ] **4.3** Update README.md with new configuration option
- [ ] **4.4** Add configuration examples to documentation
- [ ] **4.5** Test end-to-end functionality with different column combinations
- [x] **4.6** Add parameter table column information to system help screen

### Phase 5: Validation & Polish
- [ ] **5.1** Verify graceful degradation with invalid configurations
- [ ] **5.2** Test UserPreferences save/load with column configurations
- [ ] **5.3** Verify IntelliSense works for enum values
- [ ] **5.4** Test with existing transformation rules
- [ ] **5.5** Performance testing with different column combinations
- [ ] **5.6** Final code review and cleanup

### Notes:
- **Dependencies**: Each phase should be completed before moving to the next
- **Testing**: Unit tests should be written alongside implementation
- **Backward Compatibility**: Ensure existing functionality remains unchanged
- **Error Handling**: Invalid configurations should be logged but not crash the application 