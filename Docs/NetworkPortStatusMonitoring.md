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
2. **Analyzes firewall rules** to understand connectivity issues
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

1. **System Help Integration**
   - Network troubleshooting section in F1 help
   - Current port status summary
   - Firewall rule analysis
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

1. **INetworkStatusFormatter**
   - Network troubleshooting display for system help
   - Renders firewall analysis and troubleshooting commands
   - No main console display integration

2. **INetworkCommandProvider**
   - Platform-specific command generation
   - No display logic, pure command generation
   - Follows existing naming patterns (like IShortcutConfigurationManager)

3. **IFirewallAnalyzer**
   - Platform-specific firewall rule analysis
   - Analyzes outbound connectivity permissions
   - Returns simple allowed/blocked status with relevant rules

4. **IPortStatusMonitor**
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
   - Register `IFirewallAnalyzer` as singleton
   - Register `INetworkCommandProvider` as singleton
   - Register `INetworkStatusFormatter` for system help integration

2. **Application Orchestrator Integration**
   - Include in health monitoring and recovery logic
   - Add to service statistics collection
   - Integrate with existing recovery system
   - Collect network status for system help display

3. **System Help Integration**
   - Extend existing `SystemHelpRenderer`
   - Add network troubleshooting section
   - Use `INetworkStatusFormatter` for display
   - Use `INetworkCommandProvider` for platform-specific commands

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create interfaces and data models
2. Implement `IFirewallAnalyzer` (Windows)
3. Implement `INetworkCommandProvider` (Windows)
4. Implement `IPortStatusMonitor` service

### Phase 2: Help System Integration
1. Implement `INetworkStatusFormatter`
2. Extend `SystemHelpRenderer` with network troubleshooting
3. Integrate `INetworkCommandProvider` for commands

### Phase 3: Testing & Validation
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

### System Help Section
```
NETWORK TROUBLESHOOTING:
Current Status:
  iPhone Local Port: Open (UDP 28964)
  iPhone Outbound: Allowed (UDP 21412 to 192.168.1.178)
  iPhone Firewall: Blocked (Missing outbound rule)
  PC Discovery: Allowed (UDP 47779 to localhost)
  PC WebSocket: Allowed (8001 to localhost)

Firewall Rules:
  - SharpBridge UDP Outbound (Disabled)
  - Default Outbound UDP (Block)

Windows Commands:
  Add iPhone UDP rule: netsh advfirewall firewall add rule name="SharpBridge iPhone UDP" dir=out action=allow protocol=UDP remoteport=21412 remoteip=192.168.1.178
  Remove rule: netsh advfirewall firewall delete rule name="SharpBridge iPhone UDP"
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
3. **Firewall Analysis**: Accurate detection of firewall rule issues
4. **Platform Commands**: Windows-specific troubleshooting commands
5. **Help Integration**: Network troubleshooting in F1 help system
6. **Zero Configuration**: Feature works with existing configuration
7. **Non-Elevated Operation**: No automatic port manipulation 