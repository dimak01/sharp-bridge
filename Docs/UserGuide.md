# SharpBridge User Guide

## Overview

SharpBridge connects your iPhone's VTube Studio app to VTube Studio on PC, allowing you to use iPhone face tracking with your PC VTube model. It transforms tracking data according to customizable rules and provides real-time monitoring.

## Quick Start

### Prerequisites
- .NET 8.0 Runtime
- VTube Studio on PC
- VTube Studio app on iPhone (connected to same network)

### Basic Setup
1. Download and extract SharpBridge
2. Configure your transformation rules (see Configuration section)
3. Run: `SharpBridge.exe`
4. Start VTube Studio on PC
5. Start tracking on iPhone VTube Studio

## Command Line Options

```bash
SharpBridge.exe [options]

Options:
  --config-dir <path>           Configuration directory (default: Configs)
  --transform-config <file>     Transform rules filename (default: vts_transforms.json)
  --pc-config <file>           PC VTube Studio config (default: VTubeStudioPCConfig.json)
  --phone-config <file>        Phone config (default: VTubeStudioPhoneConfig.json)
  --help                       Show help information
```

### Examples
```bash
# Use default settings
SharpBridge.exe

# Custom config directory
SharpBridge.exe --config-dir "MyConfigs"

# Custom transform rules
SharpBridge.exe --transform-config "my_custom_rules.json"
```

## Configuration

### Transformation Rules (`vts_transforms_ja.json`)

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

**Fields:**
- `name`: VTube Studio parameter name
- `func`: Mathematical expression using tracking data
- `min`/`max`: Output value bounds  
- `defaultValue`: Fallback value

**Available Tracking Parameters:**
- Head: `HeadPosX`, `HeadPosY`, `HeadPosZ`, `HeadRotX`, `HeadRotY`, `HeadRotZ`
- Eyes: `EyeLeftX`, `EyeLeftY`, `EyeLeftZ`, `EyeRightX`, `EyeRightY`, `EyeRightZ`  
- Blend Shapes: `EyeBlinkLeft`, `EyeBlinkRight`, `JawOpen`, `MouthClose`, etc.

### PC Configuration (`VTubeStudioPCConfig.json`)

```json
{
  "WebSocketUrl": "ws://localhost:8001",
  "PluginName": "SharpBridge",
  "PluginDeveloper": "YourName"
}
```

### Phone Configuration (`VTubeStudioPhoneConfig.json`) 

```json
{
  "UdpPort": 21412,
  "ListenAddress": "0.0.0.0"
}
```

## Interactive Controls

While running, use these keyboard shortcuts:

| Key | Action |
|-----|--------|
| **Alt+P** | Cycle PC client verbosity (Basic → Normal → Detailed) |
| **Alt+O** | Cycle Phone client verbosity (Basic → Normal → Detailed) |
| **Alt+T** | Cycle Transformation Engine verbosity (Basic → Normal → Detailed) |
| **Alt+K** | Hot-reload transformation configuration |
| **Ctrl+C** | Graceful shutdown |

### Verbosity Levels
- **Basic**: Essential status only
- **Normal**: Standard monitoring with key metrics  
- **Detailed**: Full debug information with parameter tables

## Understanding the Console Output

### Status Indicators
- 🟢 **Green**: Service healthy and operating normally
- 🔴 **Red**: Service error, automatic recovery in progress
- 🟡 **Yellow**: Warning or partial functionality
- 🔵 **Cyan**: Informational status

### Service Sections

**Phone Client**
- Connection status and tracking data reception
- Frame rate and parameter counts
- UDP socket health

**Transformation Engine**  
- Rule validation status (valid/invalid counts)
- Transformation performance metrics
- Failed rule details (in Normal/Detailed modes)

**PC Client**
- VTube Studio connection status
- Authentication state
- Parameter transmission success/failure

### Performance Metrics
- **FPS**: Tracking data frame rate from iPhone
- **Success Rate**: Percentage of successful operations
- **Error Counts**: Failed operations by type

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

SharpBridge automatically recovers from common failures:
- Network disconnections
- VTube Studio restarts  
- Configuration file changes (with Alt+K)

### Log Files

Check `Logs/` directory for detailed error information:
- Application logs with timestamps
- Error details for troubleshooting
- Performance metrics history

## Advanced Usage

### Custom Parameter Dependencies

Rules can reference other custom parameters:

```json
{
  "name": "ComplexExpression",
  "func": "FaceAngleY * 0.5 + HeadPosX",
  "min": -10.0,
  "max": 10.0, 
  "defaultValue": 0
}
```

Multi-pass evaluation automatically resolves dependencies.

### Performance Optimization

- Use simpler expressions for better performance
- Minimize the number of transformation rules
- Use Basic verbosity mode for production use

### Network Configuration

For different network setups, modify:
- Phone config: Change `ListenAddress` and `UdpPort`
- PC config: Update `WebSocketUrl` for remote VTube Studio

## Support

- Check console output with Alt+T/Alt+P/Alt+O for detailed diagnostics
- Review log files in `Logs/` directory
- Verify all services show green (healthy) status
- Use Alt+K to reload configuration after changes