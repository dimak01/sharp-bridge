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

## The Data Pipeline Story

### How Data Flows Through the System

**1. Data Ingestion**
The journey begins when the iPhone's VTube Studio captures face tracking data. This data is sent via UDP to our [`VTubeStudioPhoneClient`](../../src/Core/Clients/VTubeStudioPhoneClient.cs), which acts as the system's "ears" - constantly listening for tracking updates and periodically requesting fresh data.

**2. Data Transformation**
Once received, the raw tracking data flows into our [`TransformationEngine`](../../src/Core/Engines/TransformationEngine.cs) - the system's "brain." Here, mathematical expressions and interpolation rules transform the data according to user-defined configurations. This is where the magic happens: head rotations become smooth curves, eye movements get enhanced, and complex parameter dependencies are resolved through multi-pass evaluation.

**3. Data Delivery**
The transformed data then flows to our [`VTubeStudioPCClient`](../../src/Core/Clients/VTubeStudioPCClient.cs) - the system's "voice." This component establishes a WebSocket connection to the PC's VTube Studio, handles authentication, and delivers the processed tracking data in real-time.

**4. Orchestration and Monitoring**
Throughout this entire pipeline, the [`ApplicationOrchestrator`](../../src/Core/Orchestrators/ApplicationOrchestrator.cs) acts as the "conductor" - coordinating the flow, monitoring health, and ensuring everything stays in sync. It's the orchestrator that makes this feel like a cohesive system rather than disconnected components.

### Why This Architecture?

**Event-Driven Design**: The system uses events ([`TrackingDataReceived`](../../src/Models/Events/TrackingDataReceivedEventArgs.cs)) to maintain loose coupling between components. This allows each piece to focus on its core responsibility while remaining responsive to changes.

**Resilient Pipeline**: Every component has built-in health monitoring and recovery capabilities. If the PC connection drops, the system automatically attempts to reconnect. If the transformation engine fails, it gracefully degrades rather than crashing.

**Real-Time Performance**: The data pipeline processes face tracking data at the rates iPhone sends them, i.e. 30-60 FPS (with 60 FPS being the default), while the console UI updates at 10 FPS for responsive user interaction. The system is designed to process each frame under 10ms to maintain real-time performance.

**Built-in Recovery**: Every component has health monitoring and automatic recovery capabilities. If the PC connection drops, the system automatically attempts to reconnect. If the transformation engine fails, it gracefully degrades rather than crashing, ensuring the application continues operating with reduced functionality.

## The Console UI Story

### Why Console-Based UI?

For a real-time bridge application, console UI provides several key advantages:

**Low Overhead**: Unlike GUI applications, console UI doesn't compete with VTube Studio or your streaming software for system resources.

**Developer-Friendly**: The console interface serves both end users and developers, providing detailed diagnostics and configuration access without requiring separate tools.

**Rapid Development**: Console UI was chosen for faster initial development with limited resources. While a lightweight GUI remains a possibility for the future, console UI allowed us to focus on core functionality and get the application working quickly.

### How the UI System Works

**Multi-Mode Design**: The console operates in different modes (Main Status, System Help, Network Status, Initialization), each optimized for specific tasks. Users can switch between modes using keyboard shortcuts, making the interface both powerful and accessible.

**Real-Time Updates**: The UI updates at 10 FPS, providing smooth, live feedback about system state. Parameter values, connection status, and transformation results are displayed with color coding and progress indicators.

**Dynamic Configuration**: The UI adapts to user preferences - verbosity levels, parameter table customization, and display options can all be configured on-the-fly without restarting the application.

**Interactive Controls**: Keyboard shortcuts provide quick access to common functions - mode switching, configuration editing, verbosity cycling, and hot reload capabilities.

### Console UI System Architecture

The console UI follows a layered architecture with clear separation of concerns:

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
│                         │ + Initialization │                                        │
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

This architecture enables:
- **Modular UI components** that can be easily extended or modified
- **Consistent formatting** across different display modes
- **Dynamic user interaction** through configurable keyboard shortcuts
- **Responsive design** that adapts to different console sizes and user preferences

### How the Console UI System Works

**1. Data Flow Visualization**
The console UI acts as a real-time window into the data pipeline, serving two distinct audiences. For **streamers** who just want to run the app, it displays connection status and basic health information. For **Live2D model riggers** who need to configure parameters and transformation rules, it provides extensive technical details about data flow and transformations.

**2. Dual-Purpose Data Display**
The UI system uses specialized formatters like [`PhoneTrackingInfoFormatter`](../../src/UI/Formatters/PhoneTrackingInfoFormatter.cs) and [`PCTrackingInfoFormatter`](../../src/UI/Formatters/PCTrackingInfoFormatter.cs) to serve both audiences. The phone formatter shows raw blend shapes and face tracking data for riggers to understand what data is available. The PC formatter displays parameter values, transformation expressions, interpolation rules, and min/max ranges - essentially the complete transformation pipeline that riggers need to configure and debug.

**3. Mode-Based Information Architecture**
The [`ConsoleModeManager`](../../src/UI/Managers/ConsoleModeManager.cs) organizes different types of information into focused modes - Main Status for real-time monitoring, System Help for configuration, Network Status for diagnostics. This component acts as the system's "librarian" - ensuring users can find the right information at the right time.

**4. Interactive Controls and User Preferences**
When users do interact with the system, the [`KeyboardInputHandler`](../../src/UI/Components/KeyboardInputHandler.cs) captures input and maps it to actions. The [`ConsoleWindowManager`](../../src/UI/Managers/ConsoleWindowManager.cs) handles window sizing and user preferences, while the [`ParameterColorService`](../../src/UI/Services/ParameterColorService.cs) provides color coding for enhanced readability.

### Why This UI Architecture?

**Event-Driven Updates**: The UI system responds to data changes through events and the orchestrator's update cycle, ensuring real-time information display without blocking the data pipeline.

**Modular Design**: Each UI component has a single responsibility - input handling, mode management, content generation, or formatting. This makes the system easy to extend and maintain.

**User-Centric Configuration**: The system adapts to user preferences through [`UserPreferences`](../../configs/UserPreferences.json) and dynamic shortcut configuration, making it both powerful and personalized.

## The Resilient System Story

Sharp Bridge is designed to be resilient from the moment it starts up through its entire runtime. The system handles failures gracefully, provides clear feedback to users, and can recover from many common issues automatically.

### Startup Resilience: The Initialization Journey

The application startup is a carefully orchestrated sequence of steps, each with built-in error handling and recovery:

```
Console Setup → Transformation Engine → File Watchers → PC Client → Phone Client → Parameter Sync → Final Setup
```

**Parameter Sync Step:**
During the "Parameter Sync" step, Sharp Bridge:
1. **Retrieves the list of existing parameters** from VTube Studio on PC, including both default model parameters and any custom parameters
2. **Compares transformation rule names** against the list of default parameters
3. **Skips default parameters** - If a transformation rule's name matches a default VTube Studio parameter, it is skipped (default parameters cannot be modified or recreated)
4. **Creates or updates custom parameters** - For transformation rules that don't match default parameters, Sharp Bridge creates them as custom parameters or updates existing custom parameters

This automatic detection ensures that transformation rules can reuse default VTube Studio parameter names without conflicts, while unique names are automatically created as custom parameters.

Each step is tracked by the [`InitializationContentProvider`](../../src/UI/Providers/InitializationContentProvider.cs), which displays real-time progress with color-coded status indicators:
- **`[OK]`** - Step completed successfully
- **`[RUN]`** - Step currently in progress  
- **`[PEND]`** - Step waiting to start
- **`[FAIL]`** - Step failed (logged for later review)

If any step fails, the system logs the error details and can usually continue with degraded functionality rather than crashing entirely. Once the main screen loads, any ongoing issues are displayed through the status and health UI controls.

### Configuration Resilience: First-Time Setup and Remediation

Sharp Bridge handles configuration issues through a remediation system. When the application starts, the [`ConfigRemediationService`](../../src/Configuration/Services/ConfigRemediationService.cs) automatically:

1. **Validates each configuration section** (General Settings, PC Client, Phone Client, Transformation Engine)
2. **Attempts automatic remediation** for common issues (missing values, invalid formats, etc.)
3. **Falls back to sensible defaults** when possible
4. **Saves corrected configurations** automatically

This means users can start without any configuration file (first-time setup) or with a configuration file that has issues (remediation), and Sharp Bridge will guide them through setup with helpful defaults and validation.

### Runtime Monitoring: Health Tracking and Network Status

Once running, the system provides two types of monitoring:

**Health Tracking** is handled by the tracking info formatters ([`PhoneTrackingInfoFormatter`](../../src/UI/Formatters/PhoneTrackingInfoFormatter.cs), [`PCTrackingInfoFormatter`](../../src/UI/Formatters/PCTrackingInfoFormatter.cs), and [`TransformationEngineInfoFormatter`](../../src/UI/Formatters/TransformationEngineInfoFormatter.cs)):

- **Service Status**: Real-time status of Phone, PC, and Transformation Engine clients with color-coded indicators
- **Face Detection**: Shows whether face tracking is working (√ Detected / X Not Found)
- **Data Flow**: Displays tracking data quality and parameter transformation results
- **Rule Validation**: Shows transformation rule health - valid/invalid rules, failed rule details with error messages
- **Configuration Health**: Displays config file status, hot-reload attempts, and uptime since rules loaded
- **Verbosity Levels**: Users can cycle through Basic/Normal/Detailed views for different detail levels

**Network Status** is handled by the [`NetworkStatusContentProvider`](../../src/UI/Providers/NetworkStatusContentProvider.cs):

- **Port Status Monitoring**: Checks if required ports are available and accessible
- **Firewall Configuration**: Shows firewall rules and port accessibility
- **Background Refresh**: Updates network status every 10 seconds without blocking the UI
- **Troubleshooting Information**: Provides specific guidance when network issues are detected

### Graceful Degradation and Auto-Recovery

When components fail, Sharp Bridge doesn't crash. Instead, it:
- **Stays running** and continuously attempts to reconnect when services come back online
- **Displays clear error states** in the console UI so users know what's broken
- **Auto-recovers** as soon as the underlying service becomes available again
- **Provides recovery options** (retry connections, open configuration editor, reload configs)

**Real-world examples:**
- **iPhone VTS minimized**: App stops receiving data but keeps running; immediately resumes when VTS is reopened
- **PC VTS disconnected**: App continues receiving iPhone data and displays PC connection status; automatically reconnects when PC VTS comes back online
- **Broken transformation rules**: App loads with partial rule set, displays failed rules with error details, and hot-reloads when rules are fixed

### Why This Resilience Matters

**For Streamers**: The application "just works" even when things go wrong. No mysterious crashes or silent failures.

**For Riggers**: Clear error messages and troubleshooting information make it easy to diagnose and fix configuration issues.

**For Developers**: The modular design means failures are isolated and don't cascade through the system.

## Next Steps

- **Development Guide** - See [DevelopmentGuide.md](DevelopmentGuide.md) for code organization and development practices
- **Release Process** - See [ReleaseProcess.md](ReleaseProcess.md) for deployment and release management
- **User Guide** - See [User Guide](../UserGuide/README.md) for user documentation
- **Service Registration** - See [`ServiceRegistration.cs`](../../ServiceRegistration.cs) for dependency injection configuration
