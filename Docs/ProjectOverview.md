# Project Overview & Architecture

## Introduction

Sharp Bridge is a .NET/C# application that bridges iPhone's VTube Studio to VTube Studio on PC. It receives tracking data from the iPhone, processes it according to user-defined configurations, and sends the transformed data to VTube Studio running on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## High-Level Architecture

The application follows a **resilient, orchestrated data flow architecture** with automatic recovery capabilities and a sophisticated console-based user interface:

1. **VTubeStudioPhoneClient** - Receives tracking data from iPhone VTube Studio via UDP
2. **TransformationEngine** - Processes tracking data according to configuration rules
3. **VTubeStudioPCClient** - Sends transformed data to PC VTube Studio via WebSocket
4. **ApplicationOrchestrator** - Coordinates the flow between all components with recovery logic
5. **Console UI System** - Provides real-time status display, interactive controls, and user feedback
6. **Recovery System** - Automatically detects and recovers from service failures

```
                                        ┌───────────────────────────┐
                                        │                           │
                                        │  ApplicationOrchestrator  │
                                        │  + Recovery Policy        │
                                        │  + Console Management     │
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
                                         │   Console UI System   │<─── User Input (Keyboard)
                                         │   + Real-time Display │
                                         │   + Interactive Controls│
                                         │   + Status Monitoring │
                                         └───────────────────────┘
```

## Console UI & Display System

The application features a **sophisticated console-based user interface** that provides real-time monitoring, interactive controls, and comprehensive status visualization:

### Console UI Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              Console UI System                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────────────────┐ │
│  │ IConsole        │    │ ConsoleRenderer  │    │ KeyboardInputHandler            │ │
│  │ Abstraction     │◄───┤ (Central Hub)    │◄───┤ + Shortcut Registration        │ │
│  │ + SystemConsole │    │ + Formatter Mgmt │    │ + Real-time Input Processing   │ │
│  │ + TestConsole   │    │ + Layout Control │    └─────────────────────────────────┘ │
│  └─────────────────┘    └──────────────────┘                                        │
│           │                       │                                                 │
│           ▼                       ▼                                                 │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────────────────┐ │
│  │ConsoleWindow    │    │ IFormatter       │    │ TableFormatter                  │ │
│  │Manager          │    │ Implementations  │    │ + Multi-column Layout          │ │
│  │ + Size Control  │    │ + Verbosity      │    │ + Progress Bars                │ │
│  │ + Auto-restore  │    │ + Color Support  │    │ + Responsive Design            │ │
│  └─────────────────┘    └──────────────────┘    └─────────────────────────────────┘ │
│                                   │                                                 │
│                                   ▼                                                 │
│                          ┌──────────────────┐    ┌─────────────────────────────────┐ │
│                          │ ConsoleColors    │    │ Service-Specific Formatters     │ │
│                          │ + ANSI Codes     │    │ + PhoneTrackingInfoFormatter   │ │
│                          │ + Status Colors  │    │ + PCTrackingInfoFormatter      │ │
│                          │ + Health Indicators│   │ + Dynamic Content Display      │ │
│                          └──────────────────┘    └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Key Console UI Features

1. **Real-time Status Display**
   - Live service health monitoring with color-coded indicators
   - Performance metrics (FPS, connection status, error counts)
   - Tracking data visualization with progress bars and tables
   - Automatic layout adaptation based on console window size

2. **Interactive Controls**
   - **Alt+P**: Cycle PC client display verbosity (Basic → Normal → Detailed)
   - **Alt+O**: Cycle Phone client display verbosity (Basic → Normal → Detailed)
   - **Alt+T**: Cycle Transformation Engine display verbosity (Basic → Normal → Detailed)
   - **Alt+K**: Hot-reload transformation configuration
   - **Ctrl+C**: Graceful application shutdown

3. **Adaptive Formatting System**
   - **Verbosity Levels**: Three levels of detail for different debugging needs
   - **Multi-column Layout**: Automatically switches between single/multi-column based on available space
   - **Progress Visualization**: Real-time progress bars for tracking parameters

4. **Console Window Management**
   - Automatic window resizing to optimal dimensions (150x60 characters)
   - Original window size restoration on application exit
   - Graceful handling of console size constraints

5. **Testability & Abstraction**
   - `IConsole` abstraction enables comprehensive unit testing
   - `TestConsole` implementation captures output for test verification
   - Mock-friendly design for isolated component testing

### Console UI Components

#### Core Interfaces
- **IConsole** - Abstraction over console operations (cursor, writing, sizing)
- **IConsoleRenderer** - Central rendering coordinator with formatter management
- **IFormatter** - Pluggable formatters for different data types with verbosity support
- **IKeyboardInputHandler** - Keyboard shortcut registration and processing

#### Implementations
- **ConsoleRenderer** - Central hub that coordinates all console output
- **SystemConsole** - Production console wrapper with window management
- **KeyboardInputHandler** - Real-time keyboard input processing
- **ConsoleWindowManager** - Temporary window size management with restoration

#### Specialized Formatters
- **PhoneTrackingInfoFormatter** - Displays iPhone tracking data with multi-column parameter tables
- **PCTrackingInfoFormatter** - Shows PC client status and outgoing parameter data
- **TransformationEngineInfoFormatter** - Real-time transformation engine statistics with rule status tables and interactive verbosity control
- **TableFormatter** - Utility for complex tabular data with responsive layout

#### Visual Enhancement
- **ConsoleColors** - ANSI color codes for status indication and visual hierarchy
- **TableFormatter** - Progress bars, multi-column layouts, and responsive design

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

3. **TransformationEngine** (fully implemented with major refactoring)
   - **Architectural Improvements**: Introduced `TransformationRule` class to replace complex 6-parameter tuples
   - **Method Decomposition**: Refactored 150+ line methods into focused, single-responsibility methods
   - **Code Quality**: Eliminated excessive comments, TODO items, and magic numbers through self-documenting code
   - **Mathematical Processing**: Transforms tracking data according to expressions with proper error handling
   - **Rule Validation**: Comprehensive validation with graceful degradation for invalid rules
   - **Multi-pass Algorithm**: Supports parameter dependencies with automatic resolution
   - **Hot-reload Support**: Runtime configuration changes without restart
   - **Statistics Integration**: Full `IServiceStatsProvider` implementation for console UI
   - **Health Monitoring**: Real-time status tracking with detailed error reporting

4. **ApplicationOrchestrator** (fully implemented with recovery)
   - Coordinates data flow between all components
   - Implements comprehensive recovery logic via `IRecoveryPolicy`
   - Manages application lifecycle with graceful degradation
   - Monitors service health and triggers automatic recovery
   - Processes events from VTubeStudioPhoneClient
   - Forwards transformed data to VTubeStudioPCClient
   - Supports hot-reloading of transformation configurations
   - Handles keyboard shortcuts for runtime operations including Alt+T for transformation engine verbosity

5. **Recovery System** (fully implemented)
   - `SimpleRecoveryPolicy` provides consistent 2-second recovery intervals
   - Automatic detection of unhealthy services
   - Non-blocking recovery attempts that don't interrupt normal operation
   - Comprehensive logging of recovery attempts and outcomes

6. **Console Status Display System** (fully implemented with transformation engine integration)
   - **Transformation Engine Display**: Real-time statistics showing rule validation, evaluation status, and error tracking
   - **Interactive Controls**: Alt+T shortcut for cycling transformation engine display verbosity
   - **Health Monitoring**: Visual indicators for transformation engine health and rule processing status
   - **Error Visualization**: Dedicated tables for invalid rules and evaluation failures with detailed error messages
   - **Performance Metrics**: Real-time display of transformation counts, success rates, and rule statistics
   - **Multi-level Verbosity**: Basic/Normal/Detailed views for different debugging needs
   - **Responsive Design**: Adaptive layout based on console dimensions with table formatting
   - **Color-coded Status**: Green (healthy), Red (error), Yellow (warning) indicators for quick status assessment

7. **Command-Line Interface** (fully implemented)
   - Uses System.CommandLine for declarative parameter definition
   - Supports all necessary configuration options
   - Includes interactive and non-interactive modes
   - Provides helpful error messages and usage information

## Core Interfaces

### Console UI Interfaces

1. **IConsole** - Console abstraction for operations and window management
   - `WindowWidth/WindowHeight` - Console dimensions for layout calculations
   - `SetCursorPosition()` - Precise cursor control for real-time updates
   - `TrySetWindowSize()` - Temporary window resizing with restoration support

2. **IConsoleRenderer** - Central rendering coordinator
   - `RegisterFormatter<T>()` - Pluggable formatter registration
   - `Update()` - Coordinated display updates with service statistics
   - `GetFormatter<T>()` - Runtime formatter access for verbosity control

3. **IFormatter** - Pluggable display formatters with verbosity support
   - `CurrentVerbosity` - Three-level verbosity system (Basic/Normal/Detailed)
   - `CycleVerbosity()` - Runtime verbosity switching
   - `Format()` - Service-aware formatting with health status integration

4. **IKeyboardInputHandler** - Interactive keyboard control
   - `RegisterShortcut()` - Dynamic shortcut registration with descriptions
   - `CheckForKeyboardInput()` - Non-blocking input processing
   - `GetRegisteredShortcuts()` - Runtime shortcut discovery

### Resiliency Interfaces

5. **IInitializable** - Enables graceful initialization and recovery
   - `TryInitializeAsync()` - Non-blocking initialization that returns success/failure
   - `LastInitializationError` - Provides detailed error information for diagnostics

6. **IServiceStats** - Enhanced service monitoring
   - `IsHealthy` - Real-time health status indicator
   - `LastSuccessfulOperation` - Timestamp of last successful operation
   - `LastError` - Most recent error message for troubleshooting

7. **IRecoveryPolicy** - Defines recovery timing and behavior
   - `GetNextDelay()` - Determines interval between recovery attempts

### Core Application Interfaces

8. **IVTubeStudioPhoneClient** - Interface for receiving tracking data from the phone
   - Handles UDP socket communication with automatic recovery
   - Parses tracking data with error resilience
   - Provides events for new data
   - Implements health monitoring and status reporting

9. **IUdpClientWrapper** - Abstraction over UDP client for testability
   - Wraps UDP operations for easier mocking
   - Enables thorough testing of network components
   - Provides clean separation of concerns

10. **ITransformationEngine** - Interface for transforming tracking data
    - Loads and parses transformation rules
    - Applies expressions to track data
    - Manages parameter boundaries

11. **IVTubeStudioPCClient** - Interface for VTube Studio communication
    - Manages WebSocket connections with automatic recovery
    - Handles authentication with token persistence
    - Supports port discovery and parameter sending
    - Implements comprehensive health monitoring

12. **IApplicationOrchestrator** - Primary service that coordinates the flow
    - Initializes and connects all components with graceful degradation
    - Manages component lifecycle with automatic recovery
    - Processes tracking data from phone to PC
    - Handles keyboard input for runtime configuration changes
    - Manages console window and user interface

13. **IServiceStatsProvider** - Interface for components that provide statistics
    - Returns structured statistics about component state and health
    - Enables centralized monitoring of application health
    - Supports console status display with health indicators

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

1. **Real-time Status Display** - Console-based UI showing real-time statistics, health status, and recovery attempts with:
   - **Color-coded Health Indicators** - Green (healthy), Red (error), Yellow (warning), Cyan (info)
   - **Multi-column Parameter Tables** - Responsive layout that adapts to console width
   - **Progress Bars** - Visual representation of tracking parameter values
   - **Service Status Sections** - Dedicated areas for Phone Client, PC Client, and Transformation Engine status

2. **Interactive Keyboard Shortcuts**:
   - **Alt+P**: Cycle PC client display verbosity (Basic → Normal → Detailed)
   - **Alt+O**: Cycle Phone client display verbosity (Basic → Normal → Detailed)  
   - **Alt+T**: Cycle Transformation Engine display verbosity (Basic → Normal → Detailed)
   - **Alt+K**: Hot-reload transformation configuration without restart
   - **Ctrl+C**: Graceful application shutdown with cleanup

3. **Adaptive Console Management**:
   - **Automatic Window Sizing** - Resizes to optimal 150x60 character dimensions
   - **Original Size Restoration** - Restores user's original console size on exit
   - **Layout Responsiveness** - Switches between single/multi-column based on available space

4. **Performance Metrics** - FPS counting, error tracking, and request monitoring with:
   - **Real-time FPS Display** - Shows current tracking data frame rate
   - **Error Counters** - Tracks failed operations and connection issues
   - **Success Metrics** - Monitors successful data transmission rates

5. **Health Indicators** - Visual indicators of service health and recovery status:
   - **Connection Status** - Real-time WebSocket and UDP connection state
   - **Service Health** - Component-level health monitoring with timestamps
   - **Recovery Progress** - Visual feedback during automatic recovery attempts

6. **Automatic Recovery** - Silent recovery from network failures and service interruptions
7. **Error Resilience** - Comprehensive error handling with detailed logging

## Testing Strategy

The project implements a comprehensive testing strategy:
- Unit tests for all core components including resiliency features and console UI
- Mock-based testing of network dependencies and recovery scenarios
- **Console UI Testing** - Comprehensive test coverage for formatters, rendering, and keyboard input
- Integration tests for recovery flows and health monitoring
- Coverage tracking with automated reports

## Console UI Benefits

The sophisticated console UI system provides:

1. **Enhanced User Experience** - Real-time visual feedback with intuitive color coding
2. **Debugging Efficiency** - Multiple verbosity levels for different troubleshooting needs
3. **Interactive Control** - Runtime configuration changes without application restart
4. **Professional Appearance** - Clean, organized display with responsive layout
5. **Accessibility** - Console-based interface works in various environments
6. **Testability** - Full abstraction enables comprehensive automated testing

## Resiliency Benefits

The implemented resiliency system provides:

1. **Improved Reliability** - Application continues working despite network issues
2. **Better User Experience** - No manual intervention required for common failures
3. **Reduced Downtime** - Automatic recovery minimizes service interruptions
4. **Enhanced Monitoring** - Real-time visibility into service health
5. **Simplified Operations** - Self-healing architecture reduces maintenance overhead



## Technology Stack

- **.NET 8.0** - Modern, cross-platform .NET implementation
- **System.CommandLine** - For declarative command-line parsing
- **System.Text.Json** - For JSON serialization/deserialization
- **System.Net.WebSockets** - For WebSocket communication with recovery capabilities
- **ANSI Color Support** - For rich console color output and status indication
- **Moq & FluentAssertions** - For comprehensive unit testing including resiliency scenarios and console UI
- **Serilog** - For structured logging with file and console output

## Logging Architecture

**Core Abstraction**: The application uses a logging system based on the `IAppLogger` interface, which provides standard methods (Debug, Info, Warning, Error) with consistent parameter patterns. This abstraction decouples the application from specific logging implementations.

**Implementation**: The `SerilogAppLogger` provides structured logging with both console and file outputs, configured with daily rolling logs (1MB size limit) and a retention policy (31 days).

**Integration & Testing**: Logging is fully integrated into all major components via constructor injection, with proper error reporting, status tracking, and comprehensive recovery event logging.