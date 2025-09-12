# System Architecture

## Overview

Sharp Bridge implements a **resilient, orchestrated data pipeline** that bridges iPhone's VTube Studio to VTube Studio on PC. The system processes real-time face tracking data through a transformation engine and delivers it to the PC application via WebSocket communication.

## High-Level Architecture

The application follows a **resilient, orchestrated data flow architecture** with automatic recovery capabilities and a console-based user interface:

```
                                        ┌───────────────────────────┐
                                        │  ApplicationOrchestrator  │
                                        │  + Recovery Policy        │
                                        │  + Console Management     │
                                        │  + Configuration Mgmt     │
                                        └─────────────┬─────────────┘
                                                      │ coordinates & monitors
                                                      ▼
┌─────────────┐    UDP    ┌─────────────────┐            ┌────────────────────┐   WebSocket    ┌─────────────┐
│ iPhone      │  ───────► │ VTubeStudio     │   ───────► │ Transformation     │  ────────────► │ VTube       │
│ VTube Studio│           │ PhoneClient     │            │ Engine             │                │ Studio (PC) │
└─────────────┘           └─────────────────┘            └────────────────────┘                └─────────────┘
                                  │                              ▲                                     │
                                  │                              │                                     │
                                  ▼                              │                                     ▼
                          ┌─────────────────┐            ┌─────────────────┐                   ┌─────────────────┐
                          │ Health Monitor  │            │ Rule Validation │                   │ Health Monitor  │
                          │ Auto-recovery   │            │ + Hot Reload    │                   │ Auto-recovery   │
                          └─────────────────┘            └─────────────────┘                   └─────────────────┘
                                                                 
                                         ┌───────────────────────┐
                                         │  Console UI System    │<─── User Input (Keyboard)
                                         │  + Real-time Display  │
                                         │  + Dynamic Shortcuts  │
                                         │  + User Preferences   │
                                         │  + Customizable UI    │
                                         └───────────────────────┘
```

## Core Components

### ApplicationOrchestrator
The **central coordinator** that manages the entire application lifecycle and component interactions.

**Key Responsibilities:**
- Coordinates data flow between all components
- Manages component lifecycle and recovery
- Handles keyboard input and console management
- Processes tracking data events
- Manages configuration hot-reload

**Key Dependencies:**
- `IVTubeStudioPCClient` - PC communication
- `IVTubeStudioPhoneClient` - iPhone communication  
- `ITransformationEngine` - Data transformation
- `IConsoleModeManager` - UI management
- `IConfigManager` - Configuration management

### VTubeStudioPhoneClient
**UDP client** that receives tracking data from iPhone VTube Studio.

**Key Responsibilities:**
- Establishes UDP connection to iPhone
- Sends tracking requests periodically
- Receives and parses tracking data
- Raises `TrackingDataReceived` events
- Monitors connection health

**Key Dependencies:**
- `IUdpClientWrapper` - UDP communication abstraction
- `IConfigManager` - Configuration management
- `IFileChangeWatcher` - Configuration change monitoring

### TransformationEngine
**Mathematical expression evaluator** that transforms tracking data according to user-defined rules.

**Key Responsibilities:**
- Loads transformation rules from configuration
- Evaluates mathematical expressions using NCalc
- Applies interpolation methods (Linear, Bezier)
- Handles parameter dependencies and multi-pass evaluation
- Tracks runtime min/max values

**Key Dependencies:**
- `ITransformationRulesRepository` - Rule persistence
- `IConfigManager` - Configuration management
- `IFileChangeWatcher` - Configuration change monitoring

### VTubeStudioPCClient
**WebSocket client** that sends transformed data to PC VTube Studio.

**Key Responsibilities:**
- Establishes WebSocket connection to VTube Studio
- Handles authentication with token persistence
- Discovers VTube Studio port automatically
- Sends tracking data via WebSocket
- Monitors connection health

**Key Dependencies:**
- `IWebSocketWrapper` - WebSocket communication abstraction
- `IPortDiscoveryService` - Port discovery
- `IVTSParameterAdapter` - Parameter adaptation
- `IConfigManager` - Configuration management

## Console UI System

The application features a **console-based user interface** with dynamic configuration and user preferences:

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              Console UI System                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────────────────┐ │
│  │ IConsole        │    │ ConsoleMode      │    │ KeyboardInputHandler            │ │
│  │ Abstraction     │◄───┤ Manager          │◄───┤ + Dynamic Shortcuts             │ │
│  │ + SystemConsole │    │ + Mode Switching │    │ + User Preferences              │ │
│  │ + TestConsole   │    │ + Content Mgmt   │    └─────────────────────────────────┘ │
│  └─────────────────┘    └──────────────────┘                                        │
│           │                       │                                                 │
│           ▼                       ▼                                                 │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────────────────┐ │
│  │ConsoleWindow    │    │ Content          │    │ TableFormatter                  │ │
│  │Manager          │    │ Providers        │    │ + Multi-column Layout           │ │
│  │ + Size Control  │    │ + MainStatus     │    │ + Progress Bars                 │ │
│  │ + User Prefs    │    │ + SystemHelp     │    │ + Responsive Design             │ │
│  └─────────────────┘    │ + NetworkStatus  │    └─────────────────────────────────┘ │
│                         └──────────────────┘                                        │
│                                   │                                                 │
│                                   ▼                                                 │
│                          ┌──────────────────┐    ┌────────────────────────────────┐ │
│                          │ Formatters       │    │ Service-Specific Formatters    │ │
│                          │ + Verbosity      │    │ + PhoneTrackingInfoFormatter   │ │
│                          │ + Color Support  │    │ + PCTrackingInfoFormatter      │ │
│                          │ + Health Status  │    │ + TransformationEngineFormatter│ │
│                          └──────────────────┘    └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Console UI Components

#### Core Interfaces
- **`IConsole`** - Abstraction over console operations (cursor, writing, sizing)
- **`IConsoleModeManager`** - Central mode coordinator with content provider management
- **`IConsoleModeContentProvider`** - Interface for console mode content providers
- **`IFormatter`** - Pluggable formatters for different data types with verbosity support
- **`IKeyboardInputHandler`** - Keyboard shortcut registration and processing

#### Console Modes
- **Main Status** - Real-time monitoring and parameter display (default)
- **System Help** - Configuration management and system information (F1)
- **Network Status** - Network diagnostics and troubleshooting (F2)

#### Key Features
- **Real-time Status Display** - Live service health monitoring with color-coded indicators
- **Dynamic Interactive Controls** - Configurable shortcuts, verbosity cycling, hot reload
- **User Preferences System** - Persistent settings for verbosity, console dimensions, UI customization
- **Adaptive Formatting System** - Three verbosity levels, multi-column layout, progress visualization

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
- **Console Services** - `IConsoleModeManager`, `IKeyboardInputHandler`, formatters
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

## Key Design Patterns

### 1. Orchestrator Pattern
**ApplicationOrchestrator** coordinates all components and manages the application lifecycle.

### 2. Repository Pattern
**ITransformationRulesRepository** abstracts rule persistence with file-based implementation.

### 3. Factory Pattern
Multiple factories create clients, validators, and extractors based on configuration.

### 4. Observer Pattern
Event-driven communication between components (e.g., `TrackingDataReceived` event).

### 5. Strategy Pattern
Different interpolation methods, verbosity levels, and formatting strategies.

### 6. Dependency Injection
Extensive use of DI container for loose coupling and testability.

## Quality Attributes

### Performance
- **Real-time processing** - < 100ms latency for data transformation
- **Efficient networking** - UDP for iPhone, WebSocket for PC
- **Optimized rendering** - Console updates at 10 FPS

### Reliability
- **Automatic recovery** - Failed services are automatically reinitialized
- **Health monitoring** - Real-time health status tracking
- **Graceful degradation** - Continues operation with reduced functionality

### Maintainability
- **Modular design** - Clear separation of concerns
- **Interface-based** - Components depend on abstractions
- **Configuration-driven** - Behavior controlled by configuration
- **Comprehensive logging** - Structured logging with Serilog

### Usability
- **Console UI** - Real-time status display with interactive controls
- **Hot-reload** - Configuration changes without restart
- **User preferences** - Persistent settings for UI customization
- **Help system** - Built-in help and troubleshooting

## Next Steps

- **Code Organization** - See [CodeOrganization.md](CodeOrganization.md) for module structure
- **Deployment** - See [Deployment.md](Deployment.md) for system requirements
- **User Guide** - See [User Guide](../UserGuide/README.md) for user documentation
