# Project Overview & Architecture

## Introduction

Sharp Bridge is a .NET/C# application that bridges iPhone's VTube Studio to VTube Studio on PC. It receives tracking data from the iPhone, processes it according to user-defined configurations, and sends the transformed data to VTube Studio running on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## High-Level Architecture

The application follows a **resilient, orchestrated data flow architecture** with automatic recovery capabilities and a console-based user interface:

1. **VTubeStudioPhoneClient** - Receives tracking data from iPhone VTube Studio via UDP
2. **TransformationEngine** - Processes tracking data according to configuration rules
3. **VTubeStudioPCClient** - Sends transformed data to PC VTube Studio via WebSocket
4. **ApplicationOrchestrator** - Coordinates the flow between all components with recovery logic
5. **Console UI System** - Provides real-time status display, interactive controls, and user feedback
6. **Recovery System** - Automatically detects and recovers from service failures
7. **Configuration Management** - Consolidated configuration with hot-reload capabilities
8. **Parameter Synchronization** - Automatic VTube Studio parameter management

```
                                        ┌───────────────────────────┐
                                        │                           │
                                        │  ApplicationOrchestrator  │
                                        │  + Recovery Policy        │
                                        │  + Console Management     │
                                        │  + Configuration Mgmt     │
                                        └───────────────┬───────────┘
                                                    │ coordinates & monitors
                                                    ▼
┌─────────────┐    UDP    ┌─────────────────┐            ┌────────────────────┐   WebSocket   ┌─────────────┐
│ iPhone      │  ───────► │ VTubeStudio     │   ───────► │ Transformation     │  ────────────► │ VTube      │
│ VTube Studio│           │ PhoneClient     │            │ Engine             │                │ Studio (PC) │
│             │           │ + Health Monitor│            │ + Rule Validation  │                │             │
└─────────────┘           └─────────────────┘            └────────────────────┘                └─────────────┘
                                  │                              ▲                                      │
                                  │                              │                                      │
                                  ▼                      ┌─────────────────┐                           ▼
                          ┌─────────────────┐            │ Consolidated    │                   ┌─────────────────┐
                          │ Recovery Logic  │            │ Configuration   │                   │ Health Monitor  │
                          │ Auto-reconnect  │            │ + Hot Reload    │                   │ Auto-recovery   │
                          └─────────────────┘            └─────────────────┘                   └─────────────────┘
                                                                 
                                         ┌───────────────────────┐
                                         │                       │
                                         │   Console UI System   │<─── User Input (Keyboard)
                                         │   + Real-time Display │
                                         │   + Dynamic Shortcuts │
                                         │   + User Preferences  │
                                         │   + Customizable UI   │
                                         └───────────────────────┘
```

## Configuration Architecture

The application uses a **consolidated configuration system** with hot-reload capabilities:

### Configuration Structure
- **ApplicationConfig.json** - Single configuration file containing all settings
  - `GeneralSettings` - Editor commands and keyboard shortcuts
  - `PhoneClient` - iPhone connection settings
  - `PCClient` - VTube Studio PC connection settings
  - `TransformationEngine` - Transformation rules path and settings

### Configuration Management
- **Application Config Hot Reload** - File watchers monitor `ApplicationConfig.json` for automatic reload
- **Transformation Rules Change Detection** - File watchers detect transformation rule changes and notify user (manual reload required)
- **Dynamic Shortcuts** - Keyboard shortcuts configurable via JSON
- **User Preferences** - Persistent user settings (verbosity, console dimensions, parameter table customization)
- **Validation** - Configuration change detection and validation

## Console UI & Display System

The application features a **console-based user interface** with dynamic configuration and user preferences:

### Console UI Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              Console UI System                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────────────────┐ │
│  │ IConsole        │    │ ConsoleRenderer  │    │ KeyboardInputHandler            │ │
│  │ Abstraction     │◄───┤ (Central Hub)    │◄───┤ + Dynamic Shortcuts            │ │
│  │ + SystemConsole │    │ + Formatter Mgmt │    │ + User Preferences              │ │
│  │ + TestConsole   │    │ + Layout Control │    └─────────────────────────────────┘ │
│  └─────────────────┘    └──────────────────┘                                        │
│           │                       │                                                 │
│           ▼                       ▼                                                 │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────────────────┐ │
│  │ConsoleWindow    │    │ IFormatter       │    │ TableFormatter                  │ │
│  │Manager          │    │ Implementations  │    │ + Multi-column Layout          │ │
│  │ + Size Control  │    │ + Verbosity      │    │ + Progress Bars                │ │
│  │ + User Prefs    │    │ + Color Support  │    │ + Responsive Design            │ │
│  └─────────────────┘    └──────────────────┘    └─────────────────────────────────┘ │
│                                   │                                                 │
│                                   ▼                                                 │
│                          ┌──────────────────┐    ┌─────────────────────────────────┐ │
│                          │ ConsoleColors    │    │ Service-Specific Formatters     │ │
│                          │ + ANSI Codes     │    │ + PhoneTrackingInfoFormatter   │ │
│                          │ + Status Colors  │    │ + PCTrackingInfoFormatter      │ │
│                          │ + Health Indicators│   │ + TransformationEngineFormatter│ │
│                          └──────────────────┘    └─────────────────────────────────┘ │
│                                   │                                                 │
│                                   ▼                                                 │
│                          ┌──────────────────┐    ┌─────────────────────────────────┐ │
│                          │ Configuration    │    │ System Help Renderer            │ │
│                          │ Managers         │    │ + Dynamic Content              │ │
│                          │ + Shortcut Mgmt  │    │ + Configuration Display        │ │
│                          │ + Parameter Mgmt │    │ + User Preferences Display     │ │
│                          └──────────────────┘    └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Key Console UI Features

1. **Real-time Status Display**
   - Live service health monitoring with color-coded indicators
   - Performance metrics (FPS, connection status, error counts)
   - Tracking data visualization with progress bars and tables
   - Automatic layout adaptation based on console window size

2. **Dynamic Interactive Controls**
   - **Configurable Shortcuts** - Keyboard shortcuts defined in configuration
   - **Verbosity Cycling** - Runtime verbosity level switching per service
   - **Hot Reload** - Configuration reload without restart
   - **External Editor** - Open configuration files in external editor
   - **System Help** - F1 help system with dynamic content
   - **Network Status** - F2 network troubleshooting and status monitoring

3. **User Preferences System**
   - **Persistent Settings** - Verbosity levels and console dimensions saved
   - **Automatic Restoration** - User preferences restored on startup
   - **Runtime Updates** - Preferences updated during operation
   - **Customizable UI** - Parameter table column configuration

4. **Adaptive Formatting System**
   - **Verbosity Levels** - Three levels of detail for different debugging needs
   - **Multi-column Layout** - Automatically switches between single/multi-column based on available space
   - **Progress Visualization** - Real-time progress bars for tracking parameters
   - **Customizable Tables** - User-configurable column display for parameter tables

### Console UI Components

#### Core Interfaces
- **IConsole** - Abstraction over console operations (cursor, writing, sizing)
- **IConsoleModeManager** - Central mode coordinator with content provider management
- **IConsoleModeContentProvider** - Interface for console mode content providers
- **IFormatter** - Pluggable formatters for different data types with verbosity support
- **IKeyboardInputHandler** - Keyboard shortcut registration and processing

#### Configuration Management Interfaces
- **IShortcutConfigurationManager** - Manages keyboard shortcut configurations
- **IParameterTableConfigurationManager** - Manages parameter table column display configuration
- **ISystemHelpRenderer** - Renders dynamic system help with configuration information

#### Implementations
- **ConsoleModeManager** - Central hub that coordinates console modes and content providers
- **MainStatusContentProvider** - Main status display with service monitoring and formatters
- **SystemHelpContentProvider** - Dynamic help system with configuration display
- **NetworkStatusContentProvider** - Network troubleshooting and status monitoring
- **SystemConsole** - Production console wrapper with window management
- **KeyboardInputHandler** - Real-time keyboard input processing
- **ConsoleWindowManager** - Window size management with user preferences
- **ShortcutConfigurationManager** - Loads and manages keyboard shortcut configurations
- **ParameterTableConfigurationManager** - Manages parameter table column display with graceful degradation

#### Specialized Formatters
- **PhoneTrackingInfoFormatter** - Displays iPhone tracking data with multi-column parameter tables
- **PCTrackingInfoFormatter** - Shows PC client status and outgoing parameter data with customizable columns
- **TransformationEngineInfoFormatter** - Real-time transformation engine statistics with rule status tables
- **NetworkStatusFormatter** - Network troubleshooting display with structured firewall rules tables
- **TableFormatter** - Utility for complex tabular data with responsive layout and indentation support

## Service Registration & Dependency Injection

The application uses a **DI system** with keyed services and factory patterns:

### Service Registration Architecture
- **Keyed Services** - Multiple file watchers for different configuration files
- **Factory Patterns** - UDP client factory for different use cases
- **Configuration Loading** - Consolidated configuration with section access
- **Lifecycle Management** - Scoped orchestrator with singleton services

### Core Services
- **Configuration Services** - `IConfigManager`, `ApplicationConfig`, `UserPreferences`
- **Network Services** - `IWebSocketWrapper`, `IUdpClientWrapperFactory`
- **File Watching** - Multiple `IFileChangeWatcher` instances
- **Console Services** - `IConsoleRenderer`, `IKeyboardInputHandler`, formatters
- **Configuration Managers** - `IShortcutConfigurationManager`, `IParameterTableConfigurationManager`
- **Business Services** - `ITransformationEngine`, `IVTubeStudioPCClient`, `IVTubeStudioPhoneClient`

## Resiliency & Recovery Architecture

The application implements a **resiliency system** with configuration-aware health monitoring:

### Core Resiliency Features

1. **Graceful Initialization** - Application starts successfully even if services are initially unavailable
2. **Automatic Recovery** - Failed services are automatically detected and recovery is attempted
3. **Health Monitoring** - Real-time health status tracking for all components
4. **Connection Recreation** - WebSocket and UDP connections can be cleanly recreated
5. **Token Management** - Authentication tokens are properly loaded, cached, and reused
6. **Configuration Awareness** - Health status considers both operational state and configuration changes

### Recovery Mechanisms

- **Service Health Detection** - Each service reports its health status via `IServiceStats`
- **Automatic Recovery Attempts** - Unhealthy services are automatically reinitialized based on recovery policy
- **Connection State Management** - WebSocket connections can be recreated when in closed/aborted states
- **Token Persistence** - Authentication tokens are loaded from disk to avoid unnecessary re-authentication
- **Configuration Change Handling** - Services detect and handle configuration changes gracefully

## File Change Watching System

The application implements a **file watching system** for configuration hot-reload:

### File Watcher Architecture
- **Multiple Watchers** - Separate watchers for different configuration files
- **Keyed Services** - DI container manages multiple watcher instances
- **Change Detection** - Configuration comparison and validation
- **Service Reconfiguration** - Automatic service updates on application config changes

### Watched Files
- **ApplicationConfig.json** - Main configuration file with automatic hot-reload
- **Transformation Rules** - Transformation configuration with change detection (manual reload required)
- **User Preferences** - User settings persistence

## Parameter Synchronization

The application includes **parameter synchronization** with VTube Studio:

### Parameter Management
- **Automatic Creation** - VTube Studio parameters created based on transformation rules
- **Synchronization** - Parameter definitions synchronized with VTube Studio
- **Lifecycle Management** - Parameter creation and cleanup handled automatically
- **Prefix Support** - Configurable prefix to avoid naming conflicts with other plugins (requires restart for changes)

## Implementation Status

All core architectural components are implemented:

- **VTubeStudioPhoneClient** - UDP-based tracking data reception with health monitoring
- **VTubeStudioPCClient** - WebSocket-based VTube Studio communication with token management
- **TransformationEngine** - Rule-based data transformation with multi-pass evaluation and custom interpolation curves
- **ApplicationOrchestrator** - Component coordination with recovery and console management
- **Recovery System** - Automatic service recovery with configurable policies
- **Console UI System** - Real-time status display with dynamic shortcuts and user preferences
- **Console UI Modes** - Main status, system help, and network status modes with improved architecture
- **Network Status Monitoring** - Real-time network interface status and troubleshooting capabilities with structured firewall rules display
- **Configuration Management** - Consolidated configuration with hot-reload capabilities
- **File Change Watching** - Multi-watcher system for configuration monitoring
- **Parameter Synchronization** - Automatic VTube Studio parameter management with configurable prefix support
- **External Editor Integration** - Configurable file opening capabilities
- **System Help** - Dynamic help system with context awareness
- **Parameter Table Customization** - User-configurable column display for parameter tables

## Core Interfaces

### Console UI Interfaces

1. **IConsole** - Console abstraction for operations and window management
   - `WindowWidth/WindowHeight` - Console dimensions for layout calculations
   - `SetCursorPosition()` - Precise cursor control for real-time updates
   - `TrySetWindowSize()` - Temporary window resizing with restoration support

2. **IConsoleModeManager** - Central mode coordinator
   - `Update()` - Coordinated display updates with service statistics
   - `Toggle()` - Switch between console modes
   - `SetMode()` - Set specific console mode
   - `TryOpenActiveModeInEditorAsync()` - Open active mode configuration in external editor

3. **IConsoleModeContentProvider** - Console mode content provider interface
   - `Mode` - Console mode identifier
   - `DisplayName` - Human-readable mode name
   - `ToggleAction` - Shortcut action for toggling this mode
   - `GetContent()` - Get formatted content for the mode
   - `Enter()` / `Exit()` - Mode lifecycle management
   - `TryOpenInExternalEditorAsync()` - Open mode configuration in external editor

4. **IFormatter** - Pluggable display formatters with verbosity support
   - `CurrentVerbosity` - Three-level verbosity system (Basic/Normal/Detailed)
   - `CycleVerbosity()` - Runtime verbosity switching
   - `Format()` - Service-aware formatting with health status integration

4. **IKeyboardInputHandler** - Interactive keyboard control
   - `RegisterShortcut()` - Dynamic shortcut registration with descriptions
   - `CheckForKeyboardInput()` - Non-blocking input processing
   - `GetRegisteredShortcuts()` - Runtime shortcut discovery

### Configuration Management Interfaces

5. **IShortcutConfigurationManager** - Keyboard shortcut configuration management
   - `GetShortcuts()` - Retrieves configured keyboard shortcuts
   - `LoadFromConfiguration()` - Loads shortcuts from application configuration
   - `GetShortcutDescription()` - Provides human-readable shortcut descriptions

6. **IParameterTableConfigurationManager** - Parameter table column configuration management
   - `GetParameterTableColumns()` - Retrieves currently configured columns
   - `GetDefaultParameterTableColumns()` - Provides default column configuration
   - `LoadFromUserPreferences()` - Loads column configuration from user preferences
   - `GetColumnDisplayName()` - Provides human-readable column names

7. **ISystemHelpRenderer** - Dynamic system help rendering
   - `RenderSystemHelp()` - Renders complete system help display
   - `RenderKeyboardShortcuts()` - Renders keyboard shortcuts section
   - `RenderParameterTableColumns()` - Renders parameter table column configuration

### Resiliency Interfaces

8. **IInitializable** - Enables graceful initialization and recovery
   - `TryInitializeAsync()` - Non-blocking initialization that returns success/failure
   - `LastInitializationError` - Provides detailed error information for diagnostics

9. **IServiceStats** - Enhanced service monitoring
   - `IsHealthy` - Real-time health status indicator
   - `LastSuccessfulOperation` - Timestamp of last successful operation
   - `LastError` - Most recent error message for troubleshooting

10. **IRecoveryPolicy** - Defines recovery timing and behavior
    - `GetNextDelay()` - Determines interval between recovery attempts

### Core Application Interfaces

11. **IVTubeStudioPhoneClient** - Interface for receiving tracking data from the phone
    - Handles UDP socket communication with automatic recovery
    - Parses tracking data with error resilience
    - Provides events for new data
    - Implements health monitoring and status reporting

12. **IUdpClientWrapper** - Abstraction over UDP client for testability
    - Wraps UDP operations for easier mocking
    - Enables thorough testing of network components
    - Provides clean separation of concerns

13. **ITransformationEngine** - Interface for transforming tracking data
    - Loads and parses transformation rules
    - Applies expressions to track data
    - Manages parameter boundaries
    - Supports custom interpolation curves for natural parameter responses

14. **IVTubeStudioPCClient** - Interface for VTube Studio communication
    - Manages WebSocket connections with automatic recovery
    - Handles authentication with token persistence
    - Supports port discovery and parameter sending
    - Implements comprehensive health monitoring

15. **IApplicationOrchestrator** - Primary service that coordinates the flow
    - Initializes and connects all components with graceful degradation
    - Manages component lifecycle with automatic recovery
    - Processes tracking data from phone to PC
    - Handles keyboard input for runtime configuration changes
    - Manages console window and user interface

16. **IServiceStatsProvider** - Interface for components that provide statistics
    - Returns structured statistics about component state and health
    - Enables centralized monitoring of application health
    - Supports console status display with health indicators

17. **IConfigManager** - Interface for configuration management
    - Loads and saves consolidated application configuration
    - Manages user preferences persistence
    - Provides configuration file paths and validation

18. **IFileChangeWatcher** - Interface for file change monitoring
    - Monitors configuration files for changes
    - Provides change events for hot-reload functionality
    - Supports multiple watcher instances for different files

## Application Organization

The application is organized into several key areas:

1. **Models** - Data structures for tracking data, configuration, parameters, and statistics
2. **Interfaces** - Well-defined contracts between components including resiliency interfaces
3. **Services** - Concrete implementations with built-in recovery capabilities
4. **Utilities** - Helper classes for formatting, console rendering, WebSocket management, etc.
5. **Repositories** - Data access layer for transformation rules and configuration

## Entity Type Naming Conventions

**Client** - External system communication (UDP/WebSocket) with auth & recovery
**Engine** - Core processing logic with complex algorithms and state management
**Manager** - Coordinates components/resources, manages lifecycle and configuration
**Orchestrator** - High-level application flow coordination and recovery
**Service** - Specific business functionality implementation
  - **RemediationService** - Configuration issue fixing
  - **ValidatorService** - Configuration/data validation
**Provider** - Supplies data/content to other components
  - **ContentProvider** - Console UI content supply
**Factory** - Creates instances based on type/configuration
**Formatter** - Formats data for display with verbosity levels
**Wrapper** - Abstracts external dependencies for testability

## Data Structure Types (Models Directory)

**Config** - Application configuration sections and settings (mutable)
**Status** - Discrete states and modes (enum-based)
**DTO** - Data transfer objects for API communication (mutable, serializable)
**Model** - Core business entities and domain concepts (varies)
**Result** - Operation results and responses (often immutable)
**Event** - Event data and context information
**Utility** - Supporting data structures and helpers

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
- Configuration-driven behavior with hot-reload capabilities

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
11. **Consolidated Configuration** - Single configuration file with hot-reload capabilities
12. **Dynamic Shortcuts** - Keyboard shortcuts configurable via configuration
13. **User Preferences** - Persistent user settings for console behavior
14. **File Change Watching** - Comprehensive file monitoring for configuration changes
15. **Parameter Synchronization** - Automatic VTube Studio parameter management
16. **Customizable UI** - User-configurable parameter table columns for focused display
17. **Console UI Modes** - Improved separation of concerns with dedicated content providers for different display modes
18. **Network Troubleshooting** - Built-in network status monitoring and troubleshooting capabilities with structured table display
19. **Custom Interpolation** - Support for Bezier curves and other interpolation methods for natural parameter responses
20. **Table Formatting System** - Advanced table rendering with indentation support, responsive layout, and specialized column formatters

## Runtime Features

The application provides real-time status display, dynamic keyboard shortcuts, adaptive console management, performance metrics, health indicators, automatic recovery, configuration hot-reload capabilities, customizable parameter table display, console UI modes (main status, system help, network status), and network troubleshooting capabilities.
