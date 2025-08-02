# Network Port Status Monitoring Feature

## Overview

The Network Port Status Monitoring feature provides real-time visibility into the network connectivity status of SharpBridge's critical connections. This feature helps users identify and troubleshoot network connectivity issues before they impact the application's operation.

## Problem Statement

SharpBridge relies heavily on network connectivity:
- **UDP Communication**: Receives tracking data from iPhone VTube Studio
- **WebSocket Communication**: Sends transformed data to PC VTube Studio
- **Port Discovery**: Discovers VTube Studio's actual port via UDP broadcast

When network issues occur, users currently only discover problems through failed connections or error messages. This reactive approach leads to:
- Confusion about what's causing connection failures
- Difficulty determining if issues are network-related or application-related
- Time-consuming troubleshooting without clear guidance

## Solution

Implement proactive network status monitoring that:
1. **Continuously monitors** all critical network connections
2. **Displays real-time status** in the main console interface
3. **Provides troubleshooting guidance** in the system help with platform-specific commands

## Feature Requirements

### Core Functionality

1. **Port Status Monitoring**
   - Monitor local UDP port 28964 (iPhone receiving port)
   - Monitor remote UDP port 21412 (iPhone sending port) - host reachability only
   - Monitor WebSocket port 8001 (PC VTube Studio) - when discovery disabled
   - Monitor UDP port 47779 (VTube Studio discovery) - when discovery enabled
   - Monitor discovered WebSocket port (dynamic) - when discovery enabled

2. **Host Reachability Testing**
   - Test connectivity to iPhone host (`PhoneClient.IphoneIpAddress`)
   - Test connectivity to PC host (`PCClient.Host`)

3. **Configuration-Aware Monitoring**
   - Use `PCClient.UsePortDiscovery` setting to determine monitoring scope
   - When discovery enabled: monitor both discovery port and actual port
   - When discovery disabled: monitor only configured WebSocket port

### Display Requirements

1. **Main Console Display**
   - Compact network status section in main interface
   - Color-coded status indicators (green/red/yellow)
   - Real-time updates with polling frequency
   - Integration with existing console layout system

2. **System Help Integration**
   - Network troubleshooting section in F1 help
   - Current port status summary
   - Platform-specific troubleshooting commands
   - Copy-paste ready commands for users

### Platform Support

1. **Windows (Initial Implementation)**
   - Windows-specific network commands
   - Firewall rule management commands
   - Port status checking commands
   - Host connectivity testing commands

2. **Future Extensibility**
   - Interface designed for multi-platform support
   - Easy addition of Linux/macOS implementations
   - Platform detection and appropriate command selection

## Technical Architecture

### Core Interfaces

1. **INetworkStatusFormatter** (extends IFormatter)
   - Network status display with verbosity levels
   - Integration with existing console formatter system
   - Real-time status formatting for main display

2. **INetworkCommandProvider**
   - Platform-specific command generation
   - No display logic, pure command generation
   - Follows existing naming patterns (like IShortcutConfigurationManager)

3. **IPortStatusMonitor**
   - Background port status monitoring service
   - Continuous polling of network connections
   - Integration with existing health monitoring system

### Data Models

1. **NetworkStatus**
   - Aggregated network status for all monitored connections
   - iPhone and PC connection statuses
   - Timestamp of last status check

2. **PortStatus**
   - Individual port status information
   - Host reachability status
   - Port open/closed status
   - Last check timestamp

### Service Integration

1. **Dependency Injection**
   - Register `IPortStatusMonitor` as singleton
   - Register `INetworkCommandProvider` as singleton
   - Register `INetworkStatusFormatter` with existing formatter system

2. **Application Orchestrator Integration**
   - Include in health monitoring and recovery logic
   - Add to service statistics collection
   - Integrate with existing recovery system

3. **Console UI Integration**
   - Register network status formatter with `ConsoleRenderer`
   - Add network status section to main display
   - Integrate with existing layout and color systems

4. **System Help Integration**
   - Extend existing `SystemHelpRenderer`
   - Add network troubleshooting section
   - Use `INetworkCommandProvider` for platform-specific commands

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create interfaces and data models
2. Implement `IPortStatusMonitor` service
3. Implement `INetworkCommandProvider` (Windows)

### Phase 2: Console Integration
1. Implement `INetworkStatusFormatter`
2. Integrate with existing console renderer
3. Add network status section to main display

### Phase 3: Help System Integration
1. Extend `SystemHelpRenderer` with network troubleshooting
2. Integrate `INetworkCommandProvider` for commands
3. Add network status section to F1 help

### Phase 4: Testing & Validation
1. Unit tests for all new components
2. Integration testing with different network configurations
3. Testing with discovery enabled/disabled scenarios

## Configuration Integration

### No New Configuration Required
- Leverage existing configuration values:
  - `PhoneClient.IphoneIpAddress` and `PhoneClient.LocalPort`
  - `PCClient.Host`, `PCClient.Port`, and `PCClient.UsePortDiscovery`
- Use sensible defaults for monitoring behavior
- Keep feature simple and configuration-free

## Display Examples

### Main Console Display
```
Network Status:
iPhone: [🟢] Host:192.168.1.178 [🟢] Local:28964
PC VTS: [] Discovery:47779 [🟢] WebSocket:8001
```

### System Help Section
```
NETWORK TROUBLESHOOTING:
Current Status:
  iPhone Host: Reachable (192.168.1.178)
  iPhone Local Port: Open (UDP 28964)
  PC Discovery Port: Closed (UDP 47779)
  PC WebSocket Port: Open (8001)

Windows Commands:
  Check iPhone connectivity: ping 192.168.1.178
  Check local UDP port: netstat -an | findstr :28964
  Open UDP port: netsh advfirewall firewall add rule name="SharpBridge UDP" dir=in action=allow protocol=UDP localport=28964
```

## Benefits

1. **Proactive Problem Detection**: Users see connectivity issues before they impact operation
2. **Clear Troubleshooting Guidance**: Platform-specific commands for resolving issues
3. **Reduced Support Burden**: Users can self-diagnose and fix common network issues
4. **Improved User Experience**: Real-time visibility into application health
5. **Security Conscious**: No automatic port manipulation, users execute commands manually
6. **Non-Elevated Operation**: App runs with normal user privileges

## Future Enhancements

1. **Multi-Platform Support**: Linux and macOS implementations
2. **Advanced Network Diagnostics**: More detailed network analysis
3. **Automated Testing**: Network connectivity tests during startup
4. **Custom Port Ranges**: Support for non-standard port configurations
5. **Network Performance Metrics**: Latency and throughput monitoring

## Success Criteria

1. **Real-time Monitoring**: Port status updates every 5 seconds
2. **Configuration Awareness**: Monitoring adapts to `UsePortDiscovery` setting
3. **Platform Commands**: Windows-specific troubleshooting commands
4. **Console Integration**: Seamless integration with existing UI
5. **Help Integration**: Network troubleshooting in F1 help system
6. **Zero Configuration**: Feature works with existing configuration
7. **Non-Elevated Operation**: No automatic port manipulation 