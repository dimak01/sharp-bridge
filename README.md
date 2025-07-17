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

### Basic Setup
1. Download and extract SharpBridge
2. **Firewall Configuration** (Required for most users):
   - Run `Scripts\firewall-secure.bat` as Administrator to allow SharpBridge network access
   - This opens the UDP port needed for iPhone communication
   - If you need to remove the firewall rules later, run `Scripts\firewall-cleanup.bat`
3. Run: `SharpBridge.exe`
4. Start VTube Studio on PC
5. Start tracking on iPhone VTube Studio

The application will create default configuration files on first run.

## Features

- **Real-time Tracking Bridge** - Seamless iPhone to PC VTube Studio data flow
- **Dynamic Configuration** - Hot-reload application settings without restart
- **Interactive Console UI** - Real-time monitoring with verbosity controls
- **Automatic Recovery** - Self-healing from network and service failures
- **Parameter Synchronization** - Automatic VTube Studio parameter management
- **External Editor Integration** - Open configuration files in your preferred editor
- **Health Monitoring** - Visual status indicators for all components

## Installation

### Download
Download the latest release from the [Releases](https://github.com/dimak01/sharp-bridge/releases) page.

### Requirements
- Windows 10/11 (x64)

### First Run
1. Extract the downloaded archive
2. Run `SharpBridge.exe`
3. The application will create default configuration files in the `Configs` directory

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

**Available Tracking Parameters:**
- Head: `HeadPosX`, `HeadPosY`, `HeadPosZ`, `HeadRotX`, `HeadRotY`, `HeadRotZ`
- Eyes: `EyeLeftX`, `EyeLeftY`, `EyeLeftZ`, `EyeRightX`, `EyeRightY`, `EyeRightZ`  
- Blend Shapes: `EyeBlinkLeft`, `EyeBlinkRight`, `JawOpen`, `MouthClose`, etc.

## Usage

### Basic Usage
```bash
SharpBridge.exe
```

### Command Line Options
```bash
SharpBridge.exe --config-dir <path> --transform-config <file>
```

- `--config-dir`: Configuration directory (default: Configs)
- `--transform-config`: Transformation rules file (default: vts_transforms.json)

### Interactive Controls

While running, use these keyboard shortcuts:

| Key | Action |
|-----|--------|
| **Alt+P** | Cycle PC client verbosity (Basic → Normal → Detailed) |
| **Alt+O** | Cycle Phone client verbosity (Basic → Normal → Detailed) |
| **Alt+T** | Cycle Transformation Engine verbosity (Basic → Normal → Detailed) |
| **Alt+K** | Hot-reload transformation configuration |
| **Ctrl+Alt+E** | Open configuration in external editor |
| **F1** | Show system help |
| **Ctrl+C** | Graceful shutdown |

### Console Interface

The application provides real-time status monitoring with color-coded indicators:

- 🟢 **Green**: Service healthy and operating normally
- 🔴 **Red**: Service error, automatic recovery in progress
- 🟡 **Yellow**: Warning or partial functionality
- 🔵 **Cyan**: Informational status

**Service Sections:**
- **Phone Client**: Connection status, frame rate, tracking data reception
- **Transformation Engine**: Rule validation, performance metrics, error details
- **PC Client**: VTube Studio connection, authentication, parameter transmission

## Advanced Usage

### Custom Parameter Dependencies

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

### Network Configuration

For different network setups:

**Phone Configuration:**
- Update `IphoneIpAddress` to match your iPhone's IP address
- Modify `IphonePort` and `LocalPort` if needed

**PC Configuration:**
- Change `Host` for remote VTube Studio instances
- Adjust `Port` if VTube Studio uses a different port

### Performance Optimization

- Use simpler expressions for better performance
- Minimize the number of transformation rules
- Use Basic verbosity mode for production use

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

## Development

### Building from Source

```bash
git clone https://github.com/dimak01/sharp-bridge.git
cd sharp-bridge
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## Support & Documentation

- **User Guide**: [Docs/UserGuide.md](Docs/UserGuide.md) - Detailed usage instructions
- **Architecture**: [Docs/ProjectOverview.md](Docs/ProjectOverview.md) - Technical architecture documentation
- **Issues**: [GitHub Issues](https://github.com/dimak01/sharp-bridge/issues) - Report bugs and request features
- **Discussions**: [GitHub Discussions](https://github.com/dimak01/sharp-bridge/discussions) - Community support

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

## Acknowledgments

- [ovROG/rusty-bridge](https://github.com/ovROG/rusty-bridge) - The original Rust implementation that inspired this project 