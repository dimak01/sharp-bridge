# Display Customization

Sharp Bridge offers extensive display customization options to optimize your monitoring experience. Configure verbosity levels, table columns, and visual elements to match your workflow needs.

## Verbosity Controls

### Overview
Each component (Phone Client, PC Client, Transformation Engine) has independent verbosity levels that control the amount of detail displayed:

- **Basic** - Essential information only
- **Normal** - Standard level of detail (default)
- **Detailed** - Comprehensive information for debugging

### Adjusting Verbosity

#### Keyboard Shortcuts
- **Alt+T** - Cycle Transformation Engine verbosity
- **Alt+O** - Cycle Phone Client verbosity  
- **Alt+P** - Cycle PC Client verbosity

#### Configuration File
Set default verbosity levels in `UserPreferences.json`:
```json
{
  "PhoneClientVerbosity": "Normal",
  "PCClientVerbosity": "Normal", 
  "TransformationEngineVerbosity": "Normal"
}
```

### What Changes with Verbosity

#### Phone Client Display
- **Basic**: Face detection status only
- **Normal**: Face status + position/rotation + blend shapes table
- **Detailed**: All above + unlimited blend shapes (no row limit)

#### PC Client Display  
- **Basic**: Face detection status + parameter prefix
- **Normal**: All above + parameter table (limited rows)
- **Detailed**: All above + unlimited parameters (no row limit)

#### Transformation Engine Display
- **Basic**: Service status + rules overview
- **Normal**: All above + invalid rules details
- **Detailed**: All above + comprehensive rule information

## Parameter Table Customization

### Available Columns
Configure which columns to display in the PC parameter table:

- **Parameter Name** - Parameter name with color coding
- **Progress Bar** - Visual representation of parameter value
- **Value** - Raw numeric value
- **Range** - Weight and min/default/max information
- **Expression** - Transformation expression with syntax highlighting
- **Min/Max** - Runtime minimum and maximum values observed
- **Interpolation** - Interpolation method information

### Configuration
Set table columns in `UserPreferences.json`:

```json
{
  "PCParameterTableColumns": [
    "ParameterName",
    "ProgressBar", 
    "Value",
    "Range",
    "Expression",
    "MinMax",
    "Interpolation"
  ]
}
```

### Column Descriptions

#### Parameter Name
- Shows the VTube Studio parameter name
- Color-coded based on parameter status
- Indicates calculated vs. direct parameters

#### Progress Bar
- Visual bar representing parameter value within its range
- Green for normal values, red for extreme values
- Adjustable width (6-20 characters)

#### Value
- Raw numeric value of the parameter
- Formatted to 2 decimal places
- Right-aligned for easy scanning

#### Range
- Shows weight, min, default, and max values
- Format: `Weight: 1.0, Min: -1.0, Default: 0.0, Max: 1.0`
- Helps understand parameter constraints

#### Expression
- Mathematical expression used to calculate the parameter
- Syntax highlighting for variables and operators
- Color-coded for easy reading

#### Min/Max
- Runtime minimum and maximum values observed
- Format: `Min: -0.85, Max: 0.92`
- Useful for understanding actual parameter usage

#### Interpolation
- Shows interpolation method being used
- **Linear** - Standard linear interpolation
- **Bezier** - Bezier curve control points (e.g., `[0,0] [0.3,0.1] [1,1]`)
- **Default** - Falls back to linear when not specified

## Color Coding

### Status Colors
- **Success** - Green for positive status (face detected, valid rules)
- **Warning** - Yellow for caution (face not detected, warnings)
- **Error** - Red for errors (invalid rules, connection failures)
- **Info** - Blue for informational messages

## Progress Bars

### Visual Representation
- **Length** - Proportional to parameter value within its range
- **Color** - Green for normal, red for extreme values
- **Width** - Configurable from 6 to 20 characters
- **Format** - `[████████░░]` style representation

### Range Calculation
- **Min Value** - Maps to 0% (empty bar)
- **Max Value** - Maps to 100% (full bar)
- **Current Value** - Proportional position within range
- **Out of Range** - Values beyond min/max are clamped

## Best Practices

### Verbosity Selection
- **Start with Normal** - Good balance for most users
- **Use Basic for monitoring** - When you just need status
- **Use Detailed for debugging** - When troubleshooting issues
- **Adjust per component** - Different needs for different components

## Configuration Examples

### Minimal Display
```json
{
  "PhoneClientVerbosity": "Basic",
  "PCClientVerbosity": "Basic",
  "TransformationEngineVerbosity": "Basic",
  "PCParameterTableColumns": ["ParameterName","ProgressBar", "Value"]
}
```

### Debug Display
```json
{
  "PhoneClientVerbosity": "Detailed",
  "PCClientVerbosity": "Detailed", 
  "TransformationEngineVerbosity": "Detailed",
  "PCParameterTableColumns": [
    "ParameterName",
    "ProgressBar",
    "Value", 
    "Range",
    "Expression",
    "MinMax",
    "Interpolation"
  ]
}
```

