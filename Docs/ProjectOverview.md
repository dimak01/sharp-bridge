# Project Overview & Architecture

## Introduction

Sharp Bridge is a .NET/C# application that bridges iPhone's VTube Studio to VTube Studio on PC. It receives tracking data from the iPhone, processes it according to user-defined configurations, and sends the transformed data to VTube Studio running on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## High-Level Architecture

The application follows an orchestrated data flow architecture:

1. **VTubeStudioPhoneClient** - Receives tracking data from iPhone VTube Studio via UDP
2. **TransformationEngine** - Processes tracking data according to configuration rules
3. **VTubeStudioPCClient** - Sends transformed data to PC VTube Studio via WebSocket
4. **ApplicationOrchestrator** - Coordinates the flow between all components

```
                                        ┌───────────────────────┐
                                        │                       │
                                        │  ApplicationOrchestrator  │
                                        │                       │
                                        └───────────┬───────────┘
                                                    │ coordinates
                                                    ▼
┌─────────────┐    UDP    ┌─────────────────┐            ┌────────────────────┐   WebSocket   ┌─────────────┐
│ iPhone      │ ───────► │ VTubeStudio     │ ───────► │ Transformation     │ ────────────► │ VTube      │
│ VTube Studio│           │ PhoneClient     │            │ Engine             │                │ Studio (PC) │
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

1. **VTubeStudioPhoneClient** (fully implemented)
   - Receives tracking data from iPhone's VTube Studio via UDP
   - Sends periodic tracking requests to iPhone
   - Processes and raises events for new tracking data
   - Handles network and deserialization errors
   - Implements IServiceStatsProvider for statistics reporting
   - 100% test coverage with comprehensive unit tests

2. **VTubeStudioPCClient** (fully implemented)
   - Communicates with VTube Studio via WebSocket
   - Handles authentication and parameter injection
   - Manages connection state and reconnection
   - Implements IServiceStatsProvider for statistics reporting

3. **TransformationEngine** (fully implemented)
   - Loads and validates transformation rules from JSON configuration
   - Transforms tracking data according to mathematical expressions
   - Applies min/max bounds to tracking parameters
   - Features robust validation during rule loading
   - Uses fail-fast error handling during transformation

4. **ApplicationOrchestrator** (fully implemented)
   - Coordinates data flow between all components
   - Manages application lifecycle (initialization, operation, shutdown)
   - Handles error conditions gracefully
   - Processes events from VTubeStudioPhoneClient
   - Forwards transformed data to VTubeStudioPCClient

5. **Command-Line Interface** (fully implemented)
   - Uses System.CommandLine for declarative parameter definition
   - Supports all necessary configuration options
   - Includes interactive and non-interactive modes
   - Provides helpful error messages and usage information

### In-Progress Components

1. **Console Status Display System** (in progress)
   - Displays real-time statistics from all components
   - Shows connection status, tracking data, and performance metrics
   - Implements formatters for different data types
   - Supports different verbosity levels
   - Uses centralized console rendering

## Core Interfaces

1. **IVTubeStudioPhoneClient** - Interface for receiving tracking data from the phone
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

4. **IVTubeStudioPCClient** - Interface for VTube Studio communication
   - Manages WebSocket connection
   - Handles authentication
   - Discovers VTube Studio port
   - Sends parameters

5. **IApplicationOrchestrator** - Primary service that coordinates the flow
   - Initializes and connects all components 
   - Manages component lifecycle (initialization, operation, shutdown)
   - Processes tracking data from phone to PC

6. **IServiceStatsProvider<T>** - Interface for components that provide statistics
   - Returns structured statistics about component state
   - Enables centralized monitoring of application health
   - Supports console status display

## Application Organization

The application is organized into several key areas:

1. **Models** - Data structures for tracking data, configuration, parameters, and statistics
2. **Interfaces** - Well-defined contracts between components
3. **Services** - Concrete implementations of interfaces
4. **Utilities** - Helper classes for formatting, console rendering, etc.

## Code Structure

The code follows clean architecture principles:
- Components communicate via well-defined interfaces
- Separation of concerns with clear responsibilities
- Centralized orchestration for application flow
- Consistent error handling and resource management
- Standardized statistics reporting

## Key Design Decisions

1. **Centralized Orchestration** - ApplicationOrchestrator manages all component interactions
2. **Statistic Standardization** - Consistent approach to reporting component status
3. **Clean Architecture** - Separation of concerns with clear boundaries
4. **Dependency Injection** - Services depend on abstractions, not implementations
5. **Event-Driven Design** - Using events to propagate tracking data changes
6. **Resource Management** - Careful handling of I/O resources with proper cleanup

## Testing Strategy

The project implements a comprehensive testing strategy:
- Unit tests for all core components
- Mock-based testing of network dependencies
- High code coverage targets
- Coverage tracking with automated reports

## Future Enhancements

1. **Advanced Console UI** - Interactive console interface with real-time visualization
2. **Configuration UI** - Graphical interface for editing transformation rules
3. **Profile Management** - Support for multiple configuration profiles
4. **Performance Optimizations** - Benchmarking and optimizing critical paths
5. **Extended Statistics** - More detailed performance and error metrics

## Technology Stack

- **.NET 8.0** - Modern, cross-platform .NET implementation
- **System.CommandLine** - For declarative command-line parsing
- **System.Text.Json** - For JSON serialization/deserialization
- **System.Net.WebSockets** - For WebSocket communication
- **Moq & FluentAssertions** - For comprehensive unit testing 