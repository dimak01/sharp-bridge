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
5. **ConsoleRenderer** - Provides real-time status display and handles user input

```
                                        ┌───────────────────────────┐
                                        │                           │
                                        │  ApplicationOrchestrator  │
                                        │                       │
                                        └───────────────┬───────────┘
                                                    │ coordinates
                                                    ▼
┌─────────────┐    UDP    ┌─────────────────┐            ┌────────────────────┐   WebSocket   ┌─────────────┐
│ iPhone      │  ───────► │ VTubeStudio     │   ───────► │ Transformation     │  ────────────► │ VTube      │
│ VTube Studio│           │ PhoneClient     │            │ Engine             │                │ Studio (PC) │
└─────────────┘           └─────────────────┘            └────────────────────┘                └─────────────┘
                                                                 ▲
                                                                 │
                                                        ┌─────────────────┐
                                                        │ Configuration   │
                                                        │ (JSON)          │
                                                        └─────────────────┘
                                                                 
                                         ┌───────────────────────┐
                                         │                       │
                                         │   ConsoleRenderer     │<─── User Input (Keyboard)
                                         │                       │
                                         └───────────────────────┘
```

## Current Implementation Status

### Completed Components

1. **VTubeStudioPhoneClient** (fully implemented)
   - Receives tracking data from iPhone's VTube Studio via UDP
   - Sends periodic tracking requests to iPhone
   - Processes and raises events for new tracking data
   - Handles network and deserialization errors
   - Implements IServiceStatsProvider for statistics reporting
   - Provides real-time performance metrics (FPS, failed frames, etc.)

2. **VTubeStudioPCClient** (dummy implementation)
   - Currently provides a simulated interface for communicating with VTube Studio
   - Implements basic connection, authentication, and discovery logic
   - Maintains service statistics for tracking state and operations
   - Prepares the architecture for actual WebSocket implementation
   - Full implementation pending for actual WebSocket communication

3. **TransformationEngine** (fully implemented)
   - Loads and validates transformation rules from JSON configuration
   - Transforms tracking data according to mathematical expressions
   - Applies min/max bounds to tracking parameters
   - Features robust validation during rule loading
   - Uses fail-fast error handling during transformation
   - Supports hot-reloading of configurations at runtime

4. **ApplicationOrchestrator** (fully implemented)
   - Coordinates data flow between all components
   - Manages application lifecycle (initialization, operation, shutdown)
   - Handles error conditions gracefully
   - Processes events from VTubeStudioPhoneClient
   - Forwards transformed data to VTubeStudioPCClient
   - Implements keyboard shortcut handling for runtime operations
   - Supports hot-reloading of transformation configurations

5. **Console Status Display System** (fully implemented)
   - Displays real-time statistics from all components
   - Shows connection status, tracking data, and performance metrics
   - Implements formatters for different data types with customizable verbosity levels
   - Uses centralized console rendering for efficient display updates
   - Supports keyboard shortcuts for interactive control

6. **Command-Line Interface** (fully implemented)
   - Uses System.CommandLine for declarative parameter definition
   - Supports all necessary configuration options
   - Includes interactive and non-interactive modes
   - Provides helpful error messages and usage information

## Core Interfaces

1. **IVTubeStudioPhoneClient** - Interface for receiving tracking data from the phone
   - Handles UDP socket communication
   - Parses tracking data
   - Provides events for new data
   - Exposes methods for sending tracking requests and receiving responses

2. **IUdpClientWrapper** - Abstraction over UDP client for testability
   - Wraps UDP operations for easier mocking
   - Enables thorough testing of network components
   - Provides clean separation of concerns

3. **ITransformationEngine** - Interface for transforming tracking data
   - Loads and parses transformation rules
   - Applies expressions to track data
   - Manages parameter boundaries

4. **IVTubeStudioPCClient** - Interface for VTube Studio communication
   - Defines contract for WebSocket connection management
   - Specifies authentication flow
   - Declares methods for port discovery and parameter sending
   - Currently implemented with a dummy class for testing

5. **IApplicationOrchestrator** - Primary service that coordinates the flow
   - Initializes and connects all components 
   - Manages component lifecycle (initialization, operation, shutdown)
   - Processes tracking data from phone to PC
   - Handles keyboard input for runtime configuration changes

6. **IServiceStatsProvider** - Interface for components that provide statistics
   - Returns structured statistics about component state
   - Enables centralized monitoring of application health
   - Supports console status display

7. **IConsoleRenderer** - Interface for rendering application status to console
   - Provides methods for updating the display with service statistics
   - Registers formatters for different types of data
   - Manages console display and layout

8. **IFormatter** - Interface for formatting specific types of data for display
   - Converts domain objects to human-readable string representations
   - Supports different verbosity levels that can be cycled at runtime
   - Specializations exist for different tracking data types

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
- Event-driven communication between components

## Key Design Decisions

1. **Centralized Orchestration** - ApplicationOrchestrator manages all component interactions
2. **Statistic Standardization** - Consistent approach to reporting component status
3. **Clean Architecture** - Separation of concerns with clear boundaries
4. **Dependency Injection** - Services depend on abstractions, not implementations
5. **Event-Driven Design** - Using events to propagate tracking data changes
6. **Resource Management** - Careful handling of I/O resources with proper cleanup
7. **Interactive Runtime Control** - Support for keyboard shortcuts to adjust settings without restarting
8. **Hot Reloading** - Ability to reload configurations without application restart

## Runtime Features

1. **Real-time Status Display** - Console-based UI showing real-time statistics and status
2. **Keyboard Shortcuts**:
   - Alt+P: Cycle PC client display verbosity
   - Alt+O: Cycle Phone client display verbosity
   - Alt+K: Reload transformation configuration
   - Ctrl+C: Exit application gracefully
3. **Performance Metrics** - FPS counting, error tracking, and request monitoring
4. **Error Resilience** - Automatic error handling and appropriate recovery mechanisms

## Testing Strategy

The project implements a comprehensive testing strategy:
- Unit tests for all core components
- Mock-based testing of network dependencies
- High code coverage targets
- Coverage tracking with automated reports

## Future Enhancements

1. **Advanced Console UI** - Enhanced interactive console interface with more visualization options
2. **Configuration UI** - Graphical interface for editing transformation rules
3. **Profile Management** - Support for multiple configuration profiles
4. **Performance Optimizations** - Benchmarking and optimizing critical paths
5. **Extended Statistics** - More detailed performance and error metrics
6. **Improved Error Handling** - Enhanced error recovery and reporting mechanisms
7. **Full VTubeStudioPCClient Implementation** - Replace the dummy implementation with actual WebSocket communication

## Technology Stack

- **.NET 8.0** - Modern, cross-platform .NET implementation
- **System.CommandLine** - For declarative command-line parsing
- **System.Text.Json** - For JSON serialization/deserialization
- **System.Net.WebSockets** - For WebSocket communication
- **Moq & FluentAssertions** - For comprehensive unit testing 