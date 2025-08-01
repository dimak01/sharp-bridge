# Sharp Bridge

[![CI](https://github.com/dimak01/sharp-bridge/actions/workflows/ci.yml/badge.svg)](https://github.com/dimak01/sharp-bridge/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)

A .NET/C# bridge application that connects iPhone's VTube Studio to PC VTube Studio, enabling iPhone face tracking with PC VTube models. SharpBridge receives tracking data from iPhone, transforms it according to customizable rules, and sends it to VTube Studio on PC.

This project is inspired by [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## Quick Start

### Prerequisites
- VTube Studio on PC
- VTube Studio app on iPhone (connected to same network)

### Basic Usage
1. Run: `SharpBridge.exe`
2. Start VTube Studio on PC
3. Start tracking on iPhone VTube Studio

The application will create default configuration files on first run.

## Features

- **Real-time Tracking Bridge** - Seamless iPhone to PC VTube Studio data flow
- **Dynamic Configuration** - Hot-reload application settings without restart
- **Interactive Console UI** - Real-time monitoring with verbosity controls
- **Parameter Table Customization** - Configure which columns to display in the PC parameter table
- **Automatic Recovery** - Self-healing from network and service failures
- **Parameter Synchronization** - Automatic VTube Studio parameter management
- **External Editor Integration** - Open configuration files in your preferred editor
- **Health Monitoring** - Visual status indicators for all components

## Installation

### Download
Download the latest release from the [Releases](https://github.com/dimak01/sharp-bridge/releases) page.

### Requirements
- Windows 10/11 (x64)

### Setup
1. Extract the downloaded archive
2. **Firewall Configuration** (Required for most users):
   - Run `tools\setup-firewall.bat` as Administrator to allow SharpBridge network access
   - This opens the UDP port needed for iPhone communication
   - If you need to remove the firewall rules later, run `tools\cleanup-firewall.bat`
3. Run `SharpBridge.exe` for the first time
4. The application will create default configuration files in the `Configs` directory

## Configuration

SharpBridge uses a consolidated configuration system with automatic hot-reload capabilities.

### Application Configuration (`Configs/ApplicationConfig.json`)

All settings are managed in a single configuration file:

```json
{
  "GeneralSettings": {
    "EditorCommand": "notepad.exe \"%f\"",
    "Shortcuts": {
      "CycleTransformationEngineVerbosity": "Alt+T",
      "CyclePCClientVerbosity": "Alt+P",
      "CyclePhoneClientVerbosity": "Alt+O",
      "ReloadTransformationConfig": "Alt+K",
      "OpenConfigInEditor": "Ctrl+Alt+E",
      "ShowSystemHelp": "F1"
    }
  },
  "PhoneClient": {
    "IphoneIpAddress": "192.168.1.178",
    "IphonePort": 21412,
    "LocalPort": 28964
  },
  "PCClient": {
    "Host": "localhost",
    "Port": 8001,
    "UsePortDiscovery": true
  },
  "TransformationEngine": {
    "ConfigPath": "Configs/vts_transforms.json",
    "MaxEvaluationIterations": 10
  }
}
```

**Configuration Parameters:**

**GeneralSettings:**
- `EditorCommand`: Command to open configuration files in external editor (use `%f` for file path)
- `Shortcuts`: Keyboard shortcuts for interactive controls (see [Usage](#usage) section for details)

**PhoneClient:**
- `IphoneIpAddress`: IP address of your iPhone on the local network
- `IphonePort`: UDP port used by iPhone VTube Studio (default: 21412)
- `LocalPort`: UDP port for receiving tracking data from iPhone (default: 28964)

**PCClient:**
- `Host`: VTube Studio PC WebSocket host (default: localhost)
- `Port`: VTube Studio PC WebSocket port (default: 8001)
- `UsePortDiscovery`: Automatically discover VTube Studio port (recommended: true)

**TransformationEngine:**
- `ConfigPath`: Path to transformation rules configuration file
- `MaxEvaluationIterations`: Maximum iterations for complex parameter dependencies calculation (default: 10)

### User Preferences (`Configs/UserPreferences.json`)

User-specific display preferences and customization options:

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

**User Preferences Parameters:**

**Verbosity Controls:**
- `PhoneClientVerbosity`: Console output detail level for iPhone tracking data (Basic/Normal/Detailed)
- `PCClientVerbosity`: Console output detail level for PC VTube Studio data (Basic/Normal/Detailed)
- `TransformationEngineVerbosity`: Console output detail level for transformation processing (Basic/Normal/Detailed)

**Display Preferences:**
- `PreferredConsoleWidth`: Preferred console window width (default: 150)
- `PreferredConsoleHeight`: Preferred console window height (default: 60)

**Parameter Table Customization:**
- `PCParameterTableColumns`: Array of column names to display in the PC parameter table. Available options:
  - `ParameterName`: Parameter name with color coding
  - `ProgressBar`: Visual progress bar representation
  - `Value`: Raw numeric value
  - `Range`: Weight and min/default/max information
  - `Expression`: Transformation expression
  - `Interpolation`: Interpolation method information (e.g., "Linear", "Bezier (3 pts)")

### Transformation Rules (`Configs/vts_transforms.json`)

Define how iPhone tracking data maps to PC VTube Studio parameters:

```json
[
  {
    "name": "FaceAngleY",
    "func": "(-HeadRotX * 1) + ((EyeBlinkLeft + EyeBlinkRight) * -1)",
    "min": -30.0,
    "max": 30.0,
    "defaultValue": 0
  },
  {
    "name": "MouthOpen", 
    "func": "JawOpen - MouthClose",
    "min": 0.0,
    "max": 1.0,
    "defaultValue": 0
  }
]
```

**Custom Parameter Dependencies**

Rules can reference other custom parameters with automatic dependency resolution:

```json
{
  "name": "ComplexExpression",
  "func": "FaceAngleY * 0.5 + HeadPosX",
  "min": -10.0,
  "max": 10.0, 
  "defaultValue": 0
}
```

**Custom Interpolation Curves**

Transform rules support custom interpolation methods for more natural parameter responses:

```json
{
  "name": "EyeOpenLeft",
  "func": "EyeOpenLeft",
  "min": 0.0,
  "max": 1.0,
  "defaultValue": 0.5,
  "interpolation": {
    "type": "BezierInterpolation",
    "controlPoints": [0.42, 0, 1, 1]
  }
}
```

**Common Bezier Curve Examples:**

**Ease-in (accelerating):**
```json
"controlPoints": [0.42, 0, 1, 1]
```

**Ease-out (decelerating):**
```json
"controlPoints": [0, 0, 0.58, 1]
```

**Ease-in-out (smooth):**
```json
"controlPoints": [0.42, 0, 0.58, 1]
```

**Using CSS Tools:**
1. Go to [cubic-bezier.com](https://cubic-bezier.com/) or similar CSS animation tools
2. Design your curve visually
3. Copy the generated values (e.g., `cubic-bezier(0.25, 0.46, 0.45, 0.94)`)
4. Convert to SharpBridge format: `[0.25, 0.46, 0.45, 0.94]`

The control points use a compact array format that matches CSS `cubic-bezier()` function syntax. This makes it compatible with online tools like [cubic-bezier.com](https://cubic-bezier.com/) and other CSS animation generators.

**CSS Compatibility:**
- The compact format omits the implicit start point `(0, 0)` and end point `(1, 1)`
- Only the middle control points are specified, just like CSS `cubic-bezier(x1, y1, x2, y2)`
- You can copy values directly from CSS tools and use them in SharpBridge configuration
- For higher-order curves (quartic, quintic, etc.), just add more control points: `[x1, y1, x2, y2, x3, y3, ...]`

**Available Tracking Parameters:**

**Head Position & Rotation:**
- `HeadPosX`, `HeadPosY`, `HeadPosZ` - Head position in 3D space
- `HeadRotX`, `HeadRotY`, `HeadRotZ` - Head rotation angles

**Blend Shapes (Facial Expressions):**
- `BrowDownLeft`, `BrowDownRight`, `BrowInnerUp`, `BrowOuterUpLeft`, `BrowOuterUpRight`
- `CheekPuff`, `CheekSquintLeft`, `CheekSquintRight`
- `EyeBlinkLeft`, `EyeBlinkRight`, `EyeLookDownLeft`, `EyeLookDownRight`, `EyeLookInLeft`, `EyeLookInRight`, `EyeLookOutLeft`, `EyeLookOutRight`, `EyeLookUpLeft`, `EyeLookUpRight`, `EyeSquintLeft`, `EyeSquintRight`, `EyeWideLeft`, `EyeWideRight`
- `JawForward`, `JawLeft`, `JawOpen`, `JawRight`
- `MouthClose`, `MouthDimpleLeft`, `MouthDimpleRight`, `MouthFrownLeft`, `MouthFrownRight`, `MouthFunnel`, `MouthLeft`, `MouthLowerDownLeft`, `MouthLowerDownRight`, `MouthPressLeft`, `MouthPressRight`, `MouthPucker`, `MouthRight`, `MouthRollLower`, `MouthRollUpper`, `MouthShrugLower`, `MouthShrugUpper`, `MouthSmileLeft`, `MouthSmileRight`, `MouthStretchLeft`, `MouthStretchRight`, `MouthUpperUpLeft`, `MouthUpperUpRight`
- `NoseSneerLeft`, `NoseSneerRight`
- `TongueOut`

For detailed parameter information, refer to the [VTube Studio API documentation](https://github.com/1996scarlet/VTubeStudio/blob/master/README.mdhttps://github.com/DenchiSoft/VTubeStudio?tab=readme-ov-file).

### Configuration Examples

**Minimal Display:**
```json
{
  "PCParameterTableColumns": ["ParameterName", "Value"]
}
```

**Debug Mode:**
```json
{
  "PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value", "Range", "Expression"]
}
```

**Performance Focus:**
```json
{
  "PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value"]
}
```

## Usage

### Basic Usage
```bash
SharpBridge.exe
```

### Interactive Controls

While running, use these keyboard shortcuts (configurable in `ApplicationConfig.json`):

| Key | Action |
|-----|--------|
| **Alt+P** | Cycle PC client verbosity (Basic → Normal → Detailed) |
| **Alt+O** | Cycle Phone client verbosity (Basic → Normal → Detailed) |
| **Alt+T** | Cycle Transformation Engine verbosity (Basic → Normal → Detailed) |
| **Alt+K** | Hot-reload transformation configuration |
| **Ctrl+Alt+E** | Open configuration in external editor |
| **F1** | Show system help (includes parameter table column configuration) |
| **Ctrl+C** | Graceful shutdown |

### Console Interface

The application provides a real-time console interface with adaptive verbosity levels and detailed service monitoring:

**Service Status Display:**
- **Phone Client**: Face detection status, head position/rotation, blend shape tracking data
- **Transformation Engine**: Rule validation status, configuration file monitoring, error details for failed rules
- **PC Client**: VTube Studio connection status, parameter transmission, authentication state

**Verbosity Levels** (configurable per service):
- **Basic**: Essential status and health indicators
- **Normal**: Detailed tracking data, blend shape tables, parameter information
- **Detailed**: Full debugging information, error tables, performance metrics

**Visual Features:**
- **Color-coded Parameters**: Blend shapes (cyan), head parameters (magenta), calculated parameters (yellow)
- **Progress Bars**: Real-time tracking parameter visualization
- **Status Indicators**: Service health, connection state, error conditions
- **Adaptive Layout**: Automatically adjusts to console window size
- **Customizable Parameter Table**: Configure which columns to display via UserPreferences.json

## Advanced Usage

### Logging

SharpBridge provides comprehensive logging to help with debugging and monitoring:

**Log Files:**
- **Location**: `Logs/` directory (created automatically)
- **Format**: Timestamped entries with detailed error information
- **Rotation**: Automatic log file management (daily rotation, 1MB size limit, 31 files retained)
- **Current Level**: Warning (only Warning and Error levels logged - focused on important events)
- **Customization**: Log level configuration is planned for future releases

**What Gets Logged:**
- Service initialization and connection attempts
- Network communication details
- Configuration file changes and validation errors
- Transformation rule evaluation failures
- Performance metrics and recovery attempts
- User interactions and shortcut usage

**Log Analysis Tips:**
- Check timestamps to correlate with console events
- Look for `ERROR` entries for troubleshooting
- `WARNING` entries indicate recoverable issues

### Configuration Management

SharpBridge provides multiple ways to update configuration:

**Application Configuration (`ApplicationConfig.json`):**
- **Hot Reload**: Changes are automatically detected and applied without restart
- **Manual Editing**: Edit the file directly in any text editor
- **External Editor**: Use `Ctrl+Alt+E` shortcut to open in your configured editor

**User Preferences (`UserPreferences.json`):**
- **Hot Reload**: Changes are automatically detected and applied without restart
- **Manual Editing**: Edit the file directly in any text editor
- **External Editor**: Use `Ctrl+Alt+E` shortcut to open in your configured editor
- **Parameter Table Customization**: Configure which columns to display in the PC parameter table

**Transformation Rules (`vts_transforms.json`):**
- **Manual Reload**: Use `Alt+K` shortcut to reload transformation rules
- **Manual Editing**: Edit the file directly in any text editor
- **External Editor**: Use `Ctrl+Alt+E` shortcut to open in your configured editor

**Configuration Tips:**
- Application config changes are applied immediately
- User preferences changes are applied immediately
- Transformation rule changes require manual reload (`Alt+K`)
- Use `F1` for system help to see current configuration values including parameter table columns
- Verbosity levels can be cycled per service for detailed debugging

### Network Configuration

For different network setups:

**Firewall Configuration:**
- **iPhone Connection**: Run `tools\setup-firewall.bat` as Administrator (opens UDP port 28964 for receiving tracking data)
- **Default Port Only**: The firewall scripts use the default `LocalPort` (28964). If you change this port in configuration, you can either:
  - Edit the scripts to use your custom port, or
  - Manually create firewall rules for your chosen port
- **VTube Studio Discovery**: Port discovery (UDP 47779) typically works without firewall rules when VTube Studio is on the same machine
- **PC Connection**: WebSocket connection to VTube Studio (port 8001) is outbound and typically doesn't need firewall rules
- **Note**: These connections assume VTube Studio is running on the same machine as SharpBridge. Network scenarios (different machines) have not been tested and may require additional firewall configuration
- **Cleanup**: Use `tools\cleanup-firewall.bat` to remove firewall rules
- **Multiple Port Changes**: If you've switched ports multiple times, the cleanup script searches for and removes all firewall rules matching "SharpBridge UDP Port" pattern, ensuring a clean slate

**Phone Configuration:**
- Update `IphoneIpAddress` to match your iPhone's IP address
- Modify `IphonePort` and `LocalPort` if needed
- **Port Changes**: If you change `LocalPort` from default (28964), you must either:
  - Run `tools\cleanup-firewall.bat` to remove old rules, then edit scripts to use your new port and run `tools\setup-firewall.bat`
  - Or manually create firewall rules for your custom port

**PC Configuration:**
- Change `Host` for remote VTube Studio instances
- Adjust `Port` if VTube Studio uses a different port
- **Note**: If you configure a non-localhost connection (different machine), you may need to update the firewall scripts to use the correct port for your setup

## Troubleshooting

### Common Issues

**"Phone Client not receiving data"**
- Check iPhone and PC are on same network
- Verify UDP port 21412 is not blocked
- Restart iPhone VTube Studio app

**"PC Client connection failed"**  
- Ensure VTube Studio is running on PC
- Check WebSocket URL in config (default: ws://localhost:8001)
- Allow plugin connection in VTube Studio

**"Invalid transformation rules"**
- Check JSON syntax in transform config
- Verify parameter names match available tracking data
- Use Alt+T for detailed error information

**"Authentication failed"**
- Delete authentication token files and restart
- Manually allow plugin in VTube Studio settings

### Recovery Features

SharpBridge automatically recovers from:
- Network disconnections
- VTube Studio restarts  
- Configuration file changes (with Alt+K)

### Log Files

Check `Logs/` directory for detailed error information:
- Application logs with timestamps
- Error details for troubleshooting
- Performance metrics history

**When to Check Logs:**
- Console doesn't provide enough detail about an issue
- Network connectivity problems
- Configuration file errors
- Transformation rule failures
- Performance issues or unexpected behavior

## Development

### Building from Source

```bash
git clone https://github.com/dimak01/sharp-bridge.git
cd sharp-bridge
dotnet build sharp-bridge.sln
```

### Running Tests

```bash
dotnet test Tests/Tests.csproj
```

## Support & Documentation

- **Architecture**: [Docs/ProjectOverview.md](Docs/ProjectOverview.md) - Technical architecture documentation
- **Issues**: [GitHub Issues](https://github.com/dimak01/sharp-bridge/issues) - Report bugs and request features

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

## Acknowledgments

- [ovROG/rusty-bridge](https://github.com/ovROG/rusty-bridge) - The original Rust implementation that inspired this project 