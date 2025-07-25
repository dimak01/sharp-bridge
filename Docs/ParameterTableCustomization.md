# Parameter Table Customization Feature

## Overview

This feature adds customizable column display for the PC tracking parameter table in the console UI. Users can configure which columns to show via the application configuration, allowing them to focus on the information most relevant to their needs.

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

Add to `ApplicationConfig.json` under the `PCClient` section:

```json
{
  "PCClient": {
    // ... existing config
    "ParameterTableColumns": ["ParameterName", "ProgressBar", "Value", "Range", "Expression"]
  }
}
```

### 3. Model Updates

```csharp
// In VTubeStudioPCConfig.cs
public ParameterTableColumn[] ParameterTableColumns { get; set; } = DefaultColumns;

// Default columns (current behavior)
private static readonly ParameterTableColumn[] DefaultColumns = 
{
    ParameterTableColumn.ParameterName,
    ParameterTableColumn.ProgressBar, 
    ParameterTableColumn.Value,
    ParameterTableColumn.Range,
    ParameterTableColumn.Expression
};
```

### 4. Formatter Logic Updates

Update the `AppendParameters` method in `PCTrackingInfoFormatter.cs`:

```csharp
private void AppendParameters(StringBuilder builder, PCTrackingInfo trackingInfo)
{
    var parameters = trackingInfo.Parameters.ToList();
    var parametersToShow = parameters.OrderBy(p => p.Id).ToList();

    // Get column configuration with fallback to defaults
    var columnConfig = config.PCClient.ParameterTableColumns ?? DefaultColumns;

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
  "PCClient": {
    "ParameterTableColumns": ["ParameterName", "Value"]
  }
}
```

### Debug Mode
```json
{
  "PCClient": {
    "ParameterTableColumns": ["ParameterName", "ProgressBar", "Value", "Range", "Expression"]
  }
}
```

### Performance Focus
```json
{
  "PCClient": {
    "ParameterTableColumns": ["ParameterName", "ProgressBar", "Value"]
  }
}
```

## Benefits

- **Type Safety**: Enum prevents invalid column names
- **IntelliSense**: IDE will suggest valid column names
- **Consistency**: Standardized column naming across codebase
- **Extensibility**: Easy to add new columns to enum
- **Backward Compatible**: Defaults to current behavior if no configuration provided
- **Simple Configuration**: Just array of enum values
- **Hot Reload**: Changes apply immediately via existing configuration watcher

## Implementation Notes

- Invalid column names in configuration are ignored (graceful degradation)
- If no configuration is provided, defaults to current column set
- Existing table formatting logic remains unchanged
- Column width calculations handled by existing `TableFormatter`
- Configuration changes apply immediately via existing hot-reload system

## Future Enhancements

- **Presets**: Quick presets like "Minimal", "Standard", "Debug"
- **Per-User Preferences**: Store column preferences in `UserPreferences.json`
- **Dynamic Shortcuts**: Keyboard shortcuts to toggle specific columns
- **New Column Types**: Additional columns like change rate, timestamps, etc. 