# Parameter Table Customization Feature

## Overview

This feature adds customizable column display for the PC tracking parameter table in the console UI. Users can configure which columns to show via user preferences, allowing them to focus on the information most relevant to their needs.

## Implementation Plan

### 1. Enum Definition

```csharp
using System.ComponentModel;

namespace SharpBridge.Models
{
    /// <summary>
    /// Defines the available columns for the PC tracking parameter table.
    /// </summary>
    public enum ParameterTableColumn
    {
        [Description("Parameter Name")]
        ParameterName,
        [Description("Progress Bar")]
        ProgressBar,
        [Description("Value")]
        Value,
        [Description("Range")]
        Range,
        [Description("Expression")]
        Expression
    }
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
using System.Collections.Generic;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Manages the configuration for columns displayed in the PC parameter table.
    /// </summary>
    public interface IParameterTableConfigurationManager
    {
        /// <summary>
        /// Gets the currently configured parameter table columns.
        /// This will be either the user-defined columns or the default columns if not specified.
        /// </summary>
        /// <returns>An array of <see cref="ParameterTableColumn"/> representing the configured columns.</returns>
        ParameterTableColumn[] GetParameterTableColumns();

        /// <summary>
        /// Gets the default set of parameter table columns.
        /// </summary>
        /// <returns>An array of <see cref="ParameterTableColumn"/> representing the default columns.</returns>
        ParameterTableColumn[] GetDefaultParameterTableColumns();

        /// <summary>
        /// Loads the parameter table column configuration from user preferences.
        /// If the preferences are null or empty, default columns are loaded.
        /// Invalid column names in preferences are ignored with a warning.
        /// </summary>
        /// <param name="userPreferences">The user preferences object containing column configuration.</param>
        void LoadFromUserPreferences(UserPreferences userPreferences);

        /// <summary>
        /// Gets the human-readable display name for a given <see cref="ParameterTableColumn"/>.
        /// </summary>
        /// <param name="column">The parameter table column enum value.</param>
        /// <returns>The display name of the column.</returns>
        string GetColumnDisplayName(ParameterTableColumn column);
    }
}
```

### 4. Model Updates

```csharp
// In UserPreferences.cs
using System;
using SharpBridge.Models;

namespace SharpBridge.Models
{
    public class UserPreferences
    {
        public PCClientVerbosity PCClientVerbosity { get; set; } = PCClientVerbosity.Normal;

        /// <summary>
        /// Gets or sets the preferred order and visibility of columns in the PC parameter table.
        /// If empty, the default columns will be used by the ParameterTableConfigurationManager.
        /// </summary>
        public ParameterTableColumn[] PCParameterTableColumns { get; set; } = Array.Empty<ParameterTableColumn>();
    }
}
```

### 5. Configuration Manager Implementation

```csharp
// Utilities/ParameterTableConfigurationManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    public class ParameterTableConfigurationManager : IParameterTableConfigurationManager
    {
        private ParameterTableColumn[] _currentColumns;
        private readonly IConsole _console;

        public ParameterTableConfigurationManager(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _currentColumns = GetDefaultParameterTableColumns(); // Initialize with defaults
        }

        /// <inheritdoc />
        public ParameterTableColumn[] GetParameterTableColumns()
        {
            return _currentColumns;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void LoadFromUserPreferences(UserPreferences userPreferences)
        {
            if (userPreferences == null)
            {
                _console.LogWarning("User preferences are null. Loading default parameter table columns.");
                _currentColumns = GetDefaultParameterTableColumns();
                return;
            }

            if (userPreferences.PCParameterTableColumns == null || !userPreferences.PCParameterTableColumns.Any())
            {
                _console.LogInfo("No custom parameter table columns found in user preferences. Loading default columns.");
                _currentColumns = GetDefaultParameterTableColumns();
                return;
            }

            var validColumns = new List<ParameterTableColumn>();
            var allPossibleColumns = Enum.GetValues(typeof(ParameterTableColumn)).Cast<ParameterTableColumn>().ToList();

            foreach (var column in userPreferences.PCParameterTableColumns)
            {
                if (allPossibleColumns.Contains(column))
                {
                    validColumns.Add(column);
                }
                else
                {
                    _console.LogWarning($"Invalid parameter table column '{column}' found in user preferences. Ignoring.");
                }
            }

            if (!validColumns.Any())
            {
                _console.LogWarning("All specified parameter table columns in user preferences were invalid. Loading default columns.");
                _currentColumns = GetDefaultParameterTableColumns();
            }
            else
            {
                _currentColumns = validColumns.ToArray();
                _console.LogInfo($"Loaded {_currentColumns.Length} parameter table columns from user preferences.");
            }
        }

        /// <inheritdoc />
        public string GetColumnDisplayName(ParameterTableColumn column)
        {
            return AttributeHelper.GetDescription(column);
        }
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
public PCTrackingInfoFormatter(IConsole console, IParameterTableConfigurationManager columnConfigManager)
{
    _console = console ?? throw new ArgumentNullException(nameof(console));
    _columnConfigManager = columnConfigManager ?? throw new ArgumentNullException(nameof(columnConfigManager));
    _activeColumns = _columnConfigManager.GetParameterTableColumns(); // Load initial columns
}

// In AppendParameters method
private void AppendParameters(StringBuilder builder, List<PCParameter> parameters)
{
    if (parameters == null || !parameters.Any())
    {
        builder.AppendLine("\n  No parameters tracked yet.");
        return;
    }

    builder.AppendLine("\n" + ConsoleColors.Colorize("TRACKED PARAMETERS", ConsoleColors.SectionHeader));
    builder.AppendLine(ConsoleColors.Colorize(new string('═', "TRACKED PARAMETERS".Length), ConsoleColors.SectionHeader));

    // Refresh active columns in case user preferences changed
    _activeColumns = _columnConfigManager.GetParameterTableColumns();

    var columnFormatters = new List<ITableColumnFormatter<PCParameter>>();

    foreach (var column in _activeColumns)
    {
        switch (column)
        {
            case ParameterTableColumn.ParameterName:
                columnFormatters.Add(new TextColumnFormatter<PCParameter>(
                    _columnConfigManager.GetColumnDisplayName(ParameterTableColumn.ParameterName),
                    p => p.Name, 20, 40));
                break;
            case ParameterTableColumn.ProgressBar:
                columnFormatters.Add(new ProgressBarColumnFormatter<PCParameter>(
                    _columnConfigManager.GetColumnDisplayName(ParameterTableColumn.ProgressBar),
                    p => p.Value, 20));
                break;
            case ParameterTableColumn.Value:
                columnFormatters.Add(new TextColumnFormatter<PCParameter>(
                    _columnConfigManager.GetColumnDisplayName(ParameterTableColumn.Value),
                    p => p.Value.ToString("F2"), 10, 15));
                break;
            case ParameterTableColumn.Range:
                columnFormatters.Add(new TextColumnFormatter<PCParameter>(
                    _columnConfigManager.GetColumnDisplayName(ParameterTableColumn.Range),
                    p => $"{p.Min:F2}-{p.Max:F2}", 15, 20));
                break;
            case ParameterTableColumn.Expression:
                columnFormatters.Add(new TextColumnFormatter<PCParameter>(
                    _columnConfigManager.GetColumnDisplayName(ParameterTableColumn.Expression),
                    p => p.Expression, 20, 50));
                break;
            default:
                _console.LogWarning($"Unknown parameter table column: {column}. It will not be displayed.");
                break;
        }
    }

    if (!columnFormatters.Any())
    {
        builder.AppendLine("  No columns configured for display.");
        return;
    }

    var tableFormatter = new TableFormatter(); // This could be injected if needed elsewhere
    tableFormatter.AppendTable(builder, "", parameters, columnFormatters, 2, 80);
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
- Parameter table column configuration is displayed in the system help screen (F2) showing current columns and their order

## System Help Integration

The parameter table column configuration is now integrated into the system help screen (accessible via F2). The help screen displays:

- **Current Column Configuration**: Shows all currently active columns in their display order
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
- [x] **4.1** Update `Configs/UserPreferences.json` with example configuration
- [ ] **4.2** Test default propagation to saved JSON
- [x] **4.3** Update README.md with new configuration option
- [x] **4.4** Add configuration examples to documentation
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