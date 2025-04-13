# Project Overview & Architecture

## Introduction

Sharp Bridge is a .NET/C# application that bridges iPhone's VTube Studio to VTube Studio on PC. It receives tracking data from the iPhone, processes it according to user-defined configurations, and sends the transformed data to VTube Studio running on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## High-Level Architecture

The application follows a simple data flow architecture:

1. **Tracking Data Receiver** - Listens for UDP packets from iPhone VTube Studio
2. **Transformation Engine** - Processes tracking data according to configuration rules
3. **VTube Studio Client** - Sends transformed data to PC VTube Studio via WebSocket

```
┌─────────────┐    UDP    ┌─────────────────┐            ┌────────────────────┐   WebSocket   ┌─────────────┐
│ iPhone      │ ───────► │ Tracking Data   │ ───────► │ Transformation     │ ────────────► │ VTube      │
│ VTube Studio│           │ Receiver        │            │ Engine             │                │ Studio (PC) │
└─────────────┘           └─────────────────┘            └────────────────────┘                └─────────────┘
                                                                 ▲
                                                                 │
                                                        ┌─────────────────┐
                                                        │ Configuration   │
                                                        │ (JSON)          │
                                                        └─────────────────┘
```

## Current Implementation Status

### Completed Components

1. **TrackingReceiver** (fully implemented)
   - Receives tracking data from iPhone's VTube Studio via UDP
   - Sends periodic tracking requests to iPhone
   - Processes and raises events for new tracking data
   - Handles network and deserialization errors
   - 100% test coverage with comprehensive unit tests

2. **Command-Line Interface** (fully implemented)
   - Uses System.CommandLine for declarative parameter definition
   - Supports all necessary configuration options
   - Includes interactive and non-interactive modes
   - Provides helpful error messages and usage information

3. **Performance Monitor** (fully implemented)
   - Displays real-time tracking statistics
   - Shows FPS, connection status, and facial tracking data
   - Provides visual representation of key facial expressions
   - Updates continuously with minimal UI flicker

### In-Progress Components

1. **Transformation Engine** (planned)
   - Will transform tracking data according to configuration
   - Will support custom mathematical expressions
   - Will apply min/max bounds to parameters

2. **VTube Studio Client** (planned)
   - Will communicate with VTube Studio via WebSocket
   - Will handle authentication and parameter injection
   - Will manage connection state and reconnection

3. **Bridge Service** (planned)
   - Will coordinate data flow between components
   - Will handle error conditions and shutdown

## Core Interfaces

1. **ITrackingReceiver** - Interface for receiving tracking data
   - Handles UDP socket communication
   - Parses tracking data
   - Provides events for new data

2. **IUdpClientWrapper** - Abstraction over UDP client for testability
   - Wraps UDP operations for easier mocking
   - Enables thorough testing of network components
   - Provides clean separation of concerns

3. **ITransformationEngine** - Interface for transforming tracking data
   - Loads and parses transformation rules
   - Applies expressions to track data
   - Manages parameter boundaries

4. **IVTubeStudioClient** - Interface for VTube Studio communication
   - Manages WebSocket connection
   - Handles authentication
   - Discovers VTube Studio port
   - Sends parameters

5. **IBridgeService** - Primary service that coordinates the flow
   - Connects the tracking receiver to transformation engine
   - Forwards transformed parameters to VTube Studio

## Application Organization

The application is organized into several key areas:

1. **Models** - Data structures for tracking data, configuration, and parameters
2. **Interfaces** - Well-defined contracts between components
3. **Services** - Concrete implementations of interfaces
4. **Utilities** - Helper classes like command-line parsing

## Code Structure

The code follows clean architecture principles:
- Each component has clear responsibilities
- Services communicate via well-defined interfaces
- Resource management follows IDisposable pattern
- Error handling is comprehensive and consistent
- Methods follow single-responsibility principle

## Key Design Decisions

1. **Component Isolation** - Components communicate through well-defined interfaces
2. **Clean Architecture** - Separation of concerns with clear boundaries
3. **Dependency Injection** - Services depend on abstractions, not implementations
4. **Command Pattern** - Command-line handling uses declarative definitions
5. **Asynchronous Operations** - Leveraging C# async/await for non-blocking I/O
6. **Event-Driven Design** - Using events to propagate tracking data changes
7. **Resource Management** - Careful handling of I/O resources with proper cleanup

## Testing Strategy

The project implements a comprehensive testing strategy:
- Unit tests for all core components
- Mock-based testing of network dependencies
- High code coverage targets
- Coverage tracking with automated reports

## Future Enhancements

1. **Transformation Engine** - Implementation of mathematical expression evaluation
2. **VTube Studio Integration** - WebSocket communication with VTube Studio
3. **Configuration UI** - Graphical interface for editing transformation rules
4. **Parameter Visualization** - Live preview of parameter values
5. **Profile Management** - Support for multiple configuration profiles

## Technology Stack

- **.NET 8.0** - Modern, cross-platform .NET implementation
- **System.CommandLine** - For declarative command-line parsing
- **System.Text.Json** - For JSON serialization/deserialization
- **System.Net.WebSockets** - For WebSocket communication (planned)
- **Moq & FluentAssertions** - For comprehensive unit testing 