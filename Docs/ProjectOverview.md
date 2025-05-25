# Project Overview & Architecture

## Introduction

Sharp Bridge is a .NET/C# application that bridges iPhone's VTube Studio to VTube Studio on PC. It receives tracking data from the iPhone, processes it according to user-defined configurations, and sends the transformed data to VTube Studio running on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## High-Level Architecture

The application follows a **resilient, orchestrated data flow architecture** with automatic recovery capabilities:

1. **VTubeStudioPhoneClient** - Receives tracking data from iPhone VTube Studio via UDP
2. **TransformationEngine** - Processes tracking data according to configuration rules
3. **VTubeStudioPCClient** - Sends transformed data to PC VTube Studio via WebSocket
4. **ApplicationOrchestrator** - Coordinates the flow between all components with recovery logic
5. **ConsoleRenderer** - Provides real-time status display and handles user input
6. **Recovery System** - Automatically detects and recovers from service failures

```
                                        ┌───────────────────────────┐
                                        │                           │
                                        │  ApplicationOrchestrator  │
                                        │  + Recovery Policy        │
                                        └───────────────┬───────────┘
                                                    │ coordinates & monitors
                                                    ▼
┌─────────────┐    UDP    ┌─────────────────┐            ┌────────────────────┐   WebSocket   ┌─────────────┐
│ iPhone      │  ───────► │ VTubeStudio     │   ───────► │ Transformation     │  ────────────► │ VTube      │
│ VTube Studio│           │ PhoneClient     │            │ Engine             │                │ Studio (PC) │
│             │           │ + Health Monitor│            │                    │                │             │
└─────────────┘           └─────────────────┘            └────────────────────┘                └─────────────┘
                                  │                              ▲                                      │
                                  │                              │                                      │
                                  ▼                      ┌─────────────────┐                           ▼
                          ┌─────────────────┐            │ Configuration   │                   ┌─────────────────┐
                          │ Recovery Logic  │            │ (JSON)          │                   │ Health Monitor  │
                          │ Auto-reconnect  │            └─────────────────┘                   │ Auto-recovery   │
                          └─────────────────┘                                                  └─────────────────┘
                                                                 
                                         ┌───────────────────────┐
                                         │                       │
                                         │   ConsoleRenderer     │<─── User Input (Keyboard)
                                         │   + Health Display    │
                                         └───────────────────────┘
```

## Resiliency & Recovery Architecture

The application implements a **comprehensive resiliency system** that ensures continuous operation even when individual services fail:

### Core Resiliency Features

1. **Graceful Initialization** - Application starts successfully even if services are initially unavailable
2. **Automatic Recovery** - Failed services are automatically detected and recovery is attempted
3. **Health Monitoring** - Real-time health status tracking for all components
4. **Connection Recreation** - WebSocket and UDP connections can be cleanly recreated
5. **Token Management** - Authentication tokens are properly loaded, cached, and reused

### Recovery Mechanisms

- **Service Health Detection** - Each service reports its health status via `IServiceStats`
- **Automatic Recovery Attempts** - Unhealthy services are automatically reinitialized every 2 seconds
- **Connection State Management** - WebSocket connections can be recreated when in closed/aborted states
- **Token Persistence** - Authentication tokens are loaded from disk to avoid unnecessary re-authentication

## Current Implementation Status

### Completed Components

1. **VTubeStudioPhoneClient** (fully implemented with resiliency)
   - Receives tracking data from iPhone's VTube Studio via UDP
   - Implements `IInitializable` for graceful initialization and recovery
   - Uses `PhoneClientStatus` enum for type-safe status management
   - Provides comprehensive health monitoring via `IServiceStats`
   - Handles network errors gracefully with automatic recovery
   - Supports connection recreation during recovery scenarios

2. **VTubeStudioPCClient** (fully implemented with resiliency)
   - Communicates with VTube Studio via WebSocket
   - Implements `IInitializable` for graceful initialization and recovery
   - Features `RecreateWebSocket()` for clean connection recovery
   - Manages authentication tokens efficiently (loads existing tokens)
   - Provides real-time health status and error tracking
   - Supports automatic reconnection during network failures

3. **TransformationEngine** (fully implemented)
   - Loads and validates transformation rules from JSON configuration
   - Transforms tracking data according to mathematical expressions
   - Applies min/max bounds to tracking parameters
   - Features robust validation during rule loading
   - Uses fail-fast error handling during transformation
   - Supports hot-reloading of configurations at runtime

4. **ApplicationOrchestrator** (fully implemented with recovery)
   - Coordinates data flow between all components
   - Implements comprehensive recovery logic via `IRecoveryPolicy`
   - Manages application lifecycle with graceful degradation
   - Monitors service health and triggers automatic recovery
   - Processes events from VTubeStudioPhoneClient
   - Forwards transformed data to VTubeStudioPCClient
   - Supports hot-reloading of transformation configurations
   - Handles keyboard shortcuts for runtime operations

5. **Recovery System** (fully implemented)
   - `SimpleRecoveryPolicy` provides consistent 2-second recovery intervals
   - Automatic detection of unhealthy services
   - Non-blocking recovery attempts that don't interrupt normal operation
   - Comprehensive logging of recovery attempts and outcomes

6. **Console Status Display System** (fully implemented)
   - Displays real-time statistics from all components including health status
   - Shows connection status, tracking data, and performance metrics
   - Implements formatters for different data types with customizable verbosity levels
   - Uses centralized console rendering for efficient display updates
   - Supports keyboard shortcuts for interactive control

7. **Command-Line Interface** (fully implemented)
   - Uses System.CommandLine for declarative parameter definition
   - Supports all necessary configuration options
   - Includes interactive and non-interactive modes
   - Provides helpful error messages and usage information

## Core Interfaces

### Resiliency Interfaces

1. **IInitializable** - Enables graceful initialization and recovery
   - `TryInitializeAsync()` - Non-blocking initialization that returns success/failure
   - `LastInitializationError` - Provides detailed error information for diagnostics

2. **IServiceStats** - Enhanced service monitoring
   - `IsHealthy` - Real-time health status indicator
   - `LastSuccessfulOperation` - Timestamp of last successful operation
   - `LastError` - Most recent error message for troubleshooting

3. **IRecoveryPolicy** - Defines recovery timing and behavior
   - `GetNextDelay()` - Determines interval between recovery attempts

### Core Application Interfaces

4. **IVTubeStudioPhoneClient** - Interface for receiving tracking data from the phone
   - Handles UDP socket communication with automatic recovery
   - Parses tracking data with error resilience
   - Provides events for new data
   - Implements health monitoring and status reporting

5. **IUdpClientWrapper** - Abstraction over UDP client for testability
   - Wraps UDP operations for easier mocking
   - Enables thorough testing of network components
   - Provides clean separation of concerns

6. **ITransformationEngine** - Interface for transforming tracking data
   - Loads and parses transformation rules
   - Applies expressions to track data
   - Manages parameter boundaries

7. **IVTubeStudioPCClient** - Interface for VTube Studio communication
   - Manages WebSocket connections with automatic recovery
   - Handles authentication with token persistence
   - Supports port discovery and parameter sending
   - Implements comprehensive health monitoring

8. **IApplicationOrchestrator** - Primary service that coordinates the flow
   - Initializes and connects all components with graceful degradation
   - Manages component lifecycle with automatic recovery
   - Processes tracking data from phone to PC
   - Handles keyboard input for runtime configuration changes

9. **IServiceStatsProvider** - Interface for components that provide statistics
   - Returns structured statistics about component state and health
   - Enables centralized monitoring of application health
   - Supports console status display with health indicators

10. **IConsoleRenderer** - Interface for rendering application status to console
    - Provides methods for updating the display with service statistics
    - Registers formatters for different types of data
    - Manages console display and layout

11. **IFormatter** - Interface for formatting specific types of data for display
    - Converts domain objects to human-readable string representations
    - Supports different verbosity levels that can be cycled at runtime
    - Specializations exist for different tracking data types

## Application Organization

The application is organized into several key areas:

1. **Models** - Data structures for tracking data, configuration, parameters, and statistics
2. **Interfaces** - Well-defined contracts between components including resiliency interfaces
3. **Services** - Concrete implementations with built-in recovery capabilities
4. **Utilities** - Helper classes for formatting, console rendering, WebSocket management, etc.

## Code Structure

The code follows clean architecture principles with resiliency built-in:
- Components communicate via well-defined interfaces
- Separation of concerns with clear responsibilities
- Centralized orchestration for application flow and recovery
- Consistent error handling and resource management
- Standardized statistics reporting with health monitoring
- Event-driven communication between components
- Graceful degradation when services are unavailable
- Automatic recovery mechanisms for failed components

## Key Design Decisions

1. **Resilient Architecture** - Built-in recovery mechanisms for all network-dependent components
2. **Graceful Degradation** - Application continues operating even when some services fail
3. **Health Monitoring** - Real-time health status tracking for all components
4. **Centralized Recovery** - ApplicationOrchestrator manages recovery for all services
5. **Token Efficiency** - Authentication tokens are loaded, cached, and reused
6. **Connection Recreation** - Network connections can be cleanly recreated during recovery
7. **Type-Safe Status** - Enums prevent status inconsistencies
8. **Non-Blocking Recovery** - Recovery attempts don't interrupt normal operation
9. **Dependency Injection** - Services depend on abstractions, not implementations
10. **Event-Driven Design** - Using events to propagate tracking data changes

## Runtime Features

1. **Real-time Status Display** - Console-based UI showing real-time statistics, health status, and recovery attempts
2. **Keyboard Shortcuts**:
   - Alt+P: Cycle PC client display verbosity
   - Alt+O: Cycle Phone client display verbosity
   - Alt+K: Reload transformation configuration
   - Ctrl+C: Exit application gracefully
3. **Performance Metrics** - FPS counting, error tracking, and request monitoring
4. **Health Indicators** - Visual indicators of service health and recovery status
5. **Automatic Recovery** - Silent recovery from network failures and service interruptions
6. **Error Resilience** - Comprehensive error handling with detailed logging

## Testing Strategy

The project implements a comprehensive testing strategy:
- Unit tests for all core components including resiliency features
- Mock-based testing of network dependencies and recovery scenarios
- Integration tests for recovery flows and health monitoring
- High code coverage targets (271 tests currently passing)
- Coverage tracking with automated reports

## Resiliency Benefits

The implemented resiliency system provides:

1. **Improved Reliability** - Application continues working despite network issues
2. **Better User Experience** - No manual intervention required for common failures
3. **Reduced Downtime** - Automatic recovery minimizes service interruptions
4. **Enhanced Monitoring** - Real-time visibility into service health
5. **Simplified Operations** - Self-healing architecture reduces maintenance overhead

## Future Enhancements

1. **Advanced Console UI** - Enhanced interactive console interface with more visualization options
2. **Configuration UI** - Graphical interface for editing transformation rules
3. **Profile Management** - Support for multiple configuration profiles
4. **Performance Optimizations** - Benchmarking and optimizing critical paths
5. **Extended Statistics** - More detailed performance and error metrics
6. **Advanced Recovery Policies** - Configurable recovery strategies (exponential backoff, circuit breakers)
7. **Health Dashboards** - Web-based monitoring interface

## Technology Stack

- **.NET 8.0** - Modern, cross-platform .NET implementation
- **System.CommandLine** - For declarative command-line parsing
- **System.Text.Json** - For JSON serialization/deserialization
- **System.Net.WebSockets** - For WebSocket communication with recovery capabilities
- **Moq & FluentAssertions** - For comprehensive unit testing including resiliency scenarios
- **Serilog** - For structured logging with file and console output

## Logging Architecture

**Core Abstraction**: The application uses a logging system based on the `IAppLogger` interface, which provides standard methods (Debug, Info, Warning, Error) with consistent parameter patterns. This abstraction decouples the application from specific logging implementations.

**Implementation**: The `SerilogAppLogger` provides structured logging with both console and file outputs, configured with daily rolling logs (1MB size limit) and a retention policy (31 days).

**Integration & Testing**: Logging is fully integrated into all major components via constructor injection, with proper error reporting, status tracking, and comprehensive recovery event logging.