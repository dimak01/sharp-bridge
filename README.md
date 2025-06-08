# Sharp Bridge

[![CI](https://github.com/dimak01/sharp-bridge/actions/workflows/ci.yml/badge.svg)](https://github.com/dimak01/sharp-bridge/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=bugs)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=dimak01_sharp-bridge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=dimak01_sharp-bridge)

A .NET/C# bridge application for connecting iPhone's VTube Studio to PC. This tool receives tracking data from iPhone, processes it according to user-defined configurations, and sends it to VTube Studio on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## Configuration

SharpBridge uses JSON configuration files located in the `Configs` directory by default. The application will create default configurations if they don't exist.

### VTube Studio PC Configuration

The PC configuration file `Configs/VTubeStudioPCConfig.json` contains settings for connecting to VTube Studio on the PC:

```json
{
  "Host": "localhost",
  "Port": 8001,
  "PluginName": "SharpBridge",
  "PluginDeveloper": "SharpBridge Developer",
  "TokenFilePath": "auth_token.txt",
  "ConnectionTimeoutMs": 5000,
  "ReconnectionDelayMs": 2000,
  "UsePortDiscovery": true
}
```

### VTube Studio Phone Configuration

The phone configuration file `Configs/VTubeStudioPhoneConfig.json` contains settings for connecting to VTube Studio on your iPhone:

```json
{
  "IphoneIpAddress": "192.168.1.178",
  "IphonePort": 21412,
  "LocalPort": 28964,
  "RequestIntervalSeconds": 5,
  "SendForSeconds": 10,
  "ReceiveTimeoutMs": 100
}
```

**Note**: You will need to update the `IphoneIpAddress` to match your iPhone's IP address on your local network.

### Transformation Configuration

The transformation configuration file `Configs/default_transform.json` contains rules for transforming tracking data from the iPhone to parameters that the PC client can understand.

## Usage

To run SharpBridge:

```
SharpBridge [transformConfigPath]
```

Where `transformConfigPath` is an optional path to a transform configuration file (defaults to `Configs/default_transform.json`).

You can also use named command-line arguments:

```
SharpBridge --config-dir <configDirectory> --transform-config <transformFilename> --pc-config <pcConfigFilename> --phone-config <phoneConfigFilename>
```

Where:
- `--config-dir`: Sets a custom directory for all configuration files (default: "Configs")
- `--transform-config`: Specifies the transform configuration filename (default: "default_transform.json")
- `--pc-config`: Specifies the PC configuration filename (default: "VTubeStudioPCConfig.json")
- `--phone-config`: Specifies the Phone configuration filename (default: "VTubeStudioPhoneConfig.json")

All configuration files are loaded from the specified config directory, which simplifies management of multiple configuration profiles.

## Features

- Receive tracking data from iPhone VTube Studio app via UDP
- Transform tracking data using customizable parameter mappings
- Send processed data to VTube Studio on PC via WebSocket
- Configuration system for custom parameter transformations
- Command-line interface for configuration customization
- Support for multiple configuration profiles via command-line options

## Planned Features

- Enhanced UI interface for easier configuration and monitoring
- Real-time parameter visualization
- Preset management for different VTube Studio models
- Performance metrics and diagnostics

## Development

### Building the Project

```
dotnet build
```

### Running Tests

```
dotnet test
```

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

## Acknowledgments

- [ovROG/rusty-bridge](https://github.com/ovROG/rusty-bridge) - The original Rust implementation that inspired this project 