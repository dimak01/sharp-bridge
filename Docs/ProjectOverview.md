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

## Original Rusty Bridge Analysis

The original rusty-bridge implementation consists of several key components:

### Core Components

1. **VtsPhone** - Handles UDP communication with iPhone's VTube Studio
   - Listens on configured port for tracking data
   - Deserializes incoming JSON data
   - Forwards tracking data to the transformation system

2. **VtsPc** - Manages WebSocket communication with VTube Studio PC
   - Establishes WebSocket connection
   - Handles authentication
   - Implements port discovery
   - Sends transformed parameters

3. **Transformation System** - Processes tracking data
   - Loads transformation rules from JSON configuration
   - Applies mathematical expressions to tracking data
   - Creates parameter values within configured min/max bounds

### Key Protocols

The communication between components uses these protocols:

1. **iPhone to Bridge** - Uses UDP protocol on port 21412 with JSON messages
2. **Bridge to PC VTube Studio** - Uses WebSocket protocol (typically on port 8001) with JSON messages

### Configuration System

The configuration in rusty-bridge is JSON-based, defining transformation rules with:
- Parameter name
- Mathematical expression
- Min/max bounds
- Default value

## Sharp Bridge Design

Sharp Bridge follows a similar architecture to rusty-bridge, but with C# idioms and practices:

### Core Interfaces

1. **ITrackingReceiver** - Interface for receiving tracking data
   - Handles UDP socket communication
   - Parses tracking data
   - Provides events for new data

2. **ITransformationEngine** - Interface for transforming tracking data
   - Loads and parses transformation rules
   - Applies expressions to track data
   - Manages parameter boundaries

3. **IVTubeStudioClient** - Interface for VTube Studio communication
   - Manages WebSocket connection
   - Handles authentication
   - Discovers VTube Studio port
   - Sends parameters

4. **IBridgeService** - Primary service that coordinates the flow
   - Connects the tracking receiver to transformation engine
   - Forwards transformed parameters to VTube Studio

### Data Models

We've implemented the following key data models:

1. **TrackingResponse** - Contains the raw tracking data from iPhone
2. **TransformRule** - Defines how parameters are calculated
3. **TrackingParam** - Represents parameters to send to VTube Studio

### Planned Enhancements

Unlike rusty-bridge, Sharp Bridge aims to eventually provide:

1. Enhanced UI (TBD)
2. Improved configuration experience (TBD)
3. Real-time parameter visualization (TBD)

## Key Design Decisions

1. **Component Isolation** - Components communicate through well-defined interfaces
2. **Configuration Compatibility** - JSON configuration format compatible with rusty-bridge
3. **Asynchronous Operations** - Leveraging C# async/await for non-blocking I/O
4. **Event-Driven Design** - Using events to propagate tracking data changes

## Technology Stack

- **.NET 6.0** - Modern, cross-platform .NET implementation
- **System.Text.Json** - For JSON serialization/deserialization
- **System.Net.WebSockets** - For WebSocket communication
- **TBD** - Expression evaluation library (still to be selected) 