# Sharp Bridge User Guide

## Quick Start

### Prerequisites
- VTube Studio running on your PC
- VTube Studio app on your iPhone (connected to the same network)
- Windows 10/11 (x64)

### First Time Setup

1. **Download & Extract**
   - Download the latest release from [GitHub Releases](https://github.com/dimak01/sharp-bridge/releases)
   - Extract to your desired location

2. **Connect Your Devices First**
   - Start VTube Studio on your PC
   - Start tracking on your iPhone VTube Studio app
   - Ensure both are connected to the same network

3. **Start the Application**
   - Run `SharpBridge.exe`
   - The application will guide you through first-time configuration setup
   - Follow the interactive prompts to configure your iPhone's IP address and other settings

4. **Configure Firewall** (If needed)
   - The application will provide firewall setup commands if needed
   - Use the Network Status mode (F2) for detailed connectivity diagnostics

## Understanding the Console Interface

Sharp Bridge provides a sophisticated console interface with four distinct modes:

### Main Status Mode (Default)
The primary dashboard showing real-time status of all components:
- **Transformation Engine**: Rule validation status, configuration monitoring, error details
- **Phone Client**: Face detection status, head position/rotation, blend shape tracking data
- **PC Client**: VTube Studio connection status, parameter transmission, authentication state

### System Help Mode (F1)
Access configuration information and keyboard shortcuts:
- Current configuration values
- Available keyboard shortcuts
- Parameter table column configuration
- External editor integration

### Network Status Mode (F2)
Network troubleshooting and diagnostics:
- Network interface status
- Firewall rules analysis
- Connection diagnostics
- Port status monitoring

### Initialization Mode
Shows during startup with real-time progress tracking:
- **Console Setup** - Window configuration and console initialization
- **Loading Transformation Rules** - Parsing and validating transformation configuration
- **Setting up File Watchers** - Monitoring configuration files for changes
- **PC Client** - Connecting to VTube Studio and authenticating
- **Phone Client** - Setting up UDP connection to iPhone
- **Parameter Sync** - Synchronizing parameters with VTube Studio
- **Final Setup** - Keyboard shortcuts and final configuration

## Core User Workflows

### 1. Basic Operation Workflow

**Starting a Session:**
1. Start VTube Studio on your PC
2. Start tracking on your iPhone VTube Studio app
3. Launch Sharp Bridge (`SharpBridge.exe`)
4. Follow the first-time setup prompts if needed
5. Wait for initialization to complete (watch the progress display)
6. Watch the Main Status mode for connection confirmations

**Monitoring Your Session:**
- **Green indicators**: All systems healthy and connected
- **Yellow indicators**: Warnings or degraded performance
- **Red indicators**: Errors requiring attention
- **Progress bars**: Real-time parameter values and ranges

**Ending a Session:**
- Press `Ctrl+C` for graceful shutdown
- Or simply close the console window

### 2. Configuration Management Workflow

**Viewing Current Configuration:**
- Press `F1` to enter System Help mode
- Navigate through configuration sections
- Press `F1` again to return to Main mode

**Editing Configuration:**
- Press `Ctrl+Alt+E` to open the relevant configuration file in your external editor
  - **Main Status Mode**: Opens `vts_transforms.json` (transformation rules)
  - **System Help Mode**: Opens `ApplicationConfig.json` (main application settings)
  - **Network Status Mode**: Opens `ApplicationConfig.json` (main application settings)
- Or manually edit configuration files directly:
  - `ApplicationConfig.json` - Main application settings
  - `UserPreferences.json` - Display preferences  
  - `vts_transforms.json` - Transformation rules

**Applying Changes:**
- Application config changes: Applied immediately (hot reload)
- User preferences changes: Applied immediately (hot reload)
- Transformation rules changes: Press `Alt+K` to reload

### 3. Troubleshooting Workflow

**When Something Goes Wrong:**
1. **Check the Main Status mode** for error indicators
2. **Press F2** to enter Network Status mode for network diagnostics
3. **Press F1** to check configuration and shortcuts
4. **Check log files** in the `Logs/` directory for detailed error information

**Common Issues & Solutions:**
- **Phone not connecting**: Check network connectivity, restart iPhone VTube Studio, verify firewall settings
- **PC not connecting**: Ensure VTube Studio is running, check WebSocket settings
- **Parameters not updating**: Check transformation rules, use `Alt+K` to reload
- **Authentication failed**: Delete token files and restart
- **Firewall issues**: Use Network Status mode (F2) for detailed diagnostics and setup commands

### 4. Customization Workflow

**Customizing Display:**
1. Edit `UserPreferences.json` to change:
   - Verbosity levels for different services
   - Console window dimensions
   - Parameter table columns to display

**Creating Custom Transformations:**
1. Edit `vts_transforms.json` to define:
   - Parameter names and expressions
   - Min/max values and defaults
   - Custom interpolation curves
2. Press `Alt+K` to reload transformation rules

**Adjusting Keyboard Shortcuts:**
1. Edit `ApplicationConfig.json` under `GeneralSettings.Shortcuts`
2. Changes apply immediately (hot reload)

## Interactive Controls

### Keyboard Shortcuts (Configurable)

| Shortcut | Action | Description |
|----------|--------|-------------|
| **Alt+P** | Cycle PC Client Verbosity | Basic → Normal → Detailed |
| **Alt+O** | Cycle Phone Client Verbosity | Basic → Normal → Detailed |
| **Alt+T** | Cycle Transformation Engine Verbosity | Basic → Normal → Detailed |
| **Alt+K** | Reload Transformation Rules | Hot-reload transformation configuration |
| **Ctrl+Alt+E** | Open in External Editor | Open current mode's config in editor |
| **F1** | System Help | Show configuration and shortcuts |
| **F2** | Network Status | Show network diagnostics |
| **Ctrl+C** | Graceful Shutdown | Exit application safely |

### Verbosity Levels

Each service supports three verbosity levels:

- **Basic**: Essential status and health indicators only
- **Normal**: Detailed tracking data, parameter information, and tables
- **Detailed**: Full debugging information, error tables, and performance metrics

### Parameter Table Customization

Configure which columns to display in the PC parameter table via `UserPreferences.json`:

**Available Columns:**
- `ParameterName` - Parameter name with color coding
- `ProgressBar` - Visual progress bar representation
- `Value` - Raw numeric value
- `Range` - Weight and min/default/max information
- `Expression` - Transformation expression
- `Interpolation` - Interpolation method information
- `MinMax` - Runtime extremum ranges

**Example Configurations:**
```json
// Minimal display
"PCParameterTableColumns": ["ParameterName", "Value"]

// Debug mode
"PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value", "Range", "Expression"]

// Performance focus
"PCParameterTableColumns": ["ParameterName", "ProgressBar", "Value"]
```

## Configuration Files

Sharp Bridge uses three main configuration files:

### ApplicationConfig.json
Main application settings with hot-reload support:
- **GeneralSettings** - Editor commands and keyboard shortcuts
- **PhoneClient** - iPhone connection settings (IP address, ports)
- **PCClient** - VTube Studio PC connection settings (host, port, parameter prefix)
- **TransformationEngine** - Transformation rules path and evaluation settings

### UserPreferences.json
User-specific display preferences:
- **Verbosity Controls** - Detail levels for different services (Basic/Normal/Detailed)
- **Console Dimensions** - Preferred window width and height
- **Parameter Table Columns** - Which columns to display in the PC parameter table

### vts_transforms.json
Transformation rules defining how iPhone tracking data maps to PC parameters:
- **Parameter Definitions** - Name, expression, min/max values, defaults
- **Custom Interpolation** - Bezier curves and other interpolation methods
- **Parameter Dependencies** - Complex expressions referencing other parameters

## Advanced Features

### Custom Interpolation Curves
Create natural parameter responses using Bezier curves:

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

### Parameter Dependencies
Create complex expressions that reference other custom parameters:

```json
{
  "name": "ComplexExpression",
  "func": "FaceAngleY * 0.5 + HeadPosX",
  "min": -10.0,
  "max": 10.0,
  "defaultValue": 0
}
```

### Runtime Extremums Tracking
Monitor actual parameter value ranges during operation:
- View min/max values in the parameter table
- Use for calibration and optimization
- Reset when transformation rules are reloaded

## Logging and Diagnostics

### Log Files
- **Location**: `Logs/` directory (created automatically)
- **Format**: Timestamped entries with detailed error information
- **Rotation**: Daily rotation, 1MB size limit, 31 files retained
- **Level**: Warning and Error levels (focused on important events)

### What Gets Logged
- Service initialization and connection attempts
- Network communication details
- Configuration file changes and validation errors
- Transformation rule evaluation failures
- Performance metrics and recovery attempts

### Log Analysis Tips
- Check timestamps to correlate with console events
- Look for `ERROR` entries for troubleshooting
- `WARNING` entries indicate recoverable issues

## Recovery Features

Sharp Bridge automatically recovers from:
- Network disconnections
- VTube Studio restarts
- Configuration file changes
- Service failures

The application is designed to continue operating even when some services fail, with automatic recovery attempts in the background.

## Getting Help

### Built-in Help
- Press `F1` for system help and configuration display
- Press `F2` for network status and troubleshooting
- Use verbosity cycling (`Alt+P`, `Alt+O`, `Alt+T`) for detailed debugging

### External Resources
- **GitHub Issues**: [Report bugs and request features](https://github.com/dimak01/sharp-bridge/issues)
- **VTube Studio API**: [Parameter reference documentation](https://github.com/DenchiSoft/VTubeStudio)

### Configuration Examples
- Check the `configs/` directory for example configurations
- Use `Ctrl+Alt+E` to open configuration files in your preferred editor
- Start with default configurations and customize as needed

---

**Ready to get started?** Launch `SharpBridge.exe` and follow the Quick Start guide above!
