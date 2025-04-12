# Implementation Details

This document provides detailed information about the core components, data models, and communication protocols used in the Sharp Bridge application.

## Core Components and Interfaces

### ITrackingReceiver

The `ITrackingReceiver` interface is responsible for receiving tracking data from iPhone VTube Studio via UDP.

```csharp
public interface ITrackingReceiver
{
    Task RunAsync(string iphoneIp, CancellationToken cancellationToken);
    event EventHandler<TrackingResponse> TrackingDataReceived;
}
```

Key responsibilities:
- Opens a UDP socket to receive tracking data
- Deserializes incoming JSON into `TrackingResponse` objects
- Raises events when new tracking data is received
- Handles reconnection if connection is lost

### ITransformationEngine

The `ITransformationEngine` interface transforms tracking data according to configuration rules.

```csharp
public interface ITransformationEngine
{
    Task LoadRulesAsync(string filePath);
    IEnumerable<TrackingParam> TransformData(TrackingResponse trackingData);
}
```

Key responsibilities:
- Loads transformation rules from JSON configuration file
- Evaluates mathematical expressions using the tracking data
- Applies min/max bounds to transformed values
- Produces parameters ready to be sent to VTube Studio

### IVTubeStudioClient

The `IVTubeStudioClient` interface handles communication with VTube Studio PC via WebSocket.

```csharp
public interface IVTubeStudioClient
{
    Task RunAsync(CancellationToken cancellationToken);
    Task SendTrackingAsync(IEnumerable<TrackingParam> parameters, bool faceFound);
}
```

Key responsibilities:
- Establishes WebSocket connection to VTube Studio
- Handles authentication process
- Discovers VTube Studio port if not specified
- Sends transformed parameters to VTube Studio
- Manages connection state and reconnection

### IBridgeService

The `IBridgeService` interface coordinates the overall data flow between components.

```csharp
public interface IBridgeService
{
    Task RunAsync(string iphoneIp, string transformConfigPath, CancellationToken cancellationToken);
}
```

Key responsibilities:
- Initializes and coordinates other components
- Connects tracking data events to transformation and forwarding
- Manages error handling and graceful shutdown

## Data Models

### TrackingResponse

The `TrackingResponse` model represents tracking data received from iPhone VTube Studio.

```csharp
public class TrackingResponse
{
    public ulong Timestamp { get; set; }
    public short Hotkey { get; set; }
    public bool FaceFound { get; set; }
    public Coordinates Rotation { get; set; }
    public Coordinates Position { get; set; }
    public Coordinates EyeLeft { get; set; }
    public List<BlendShape> BlendShapes { get; set; }
}
```

### Coordinates

The `Coordinates` model represents 3D coordinates.

```csharp
public class Coordinates
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
```

### BlendShape

The `BlendShape` model represents a facial expression blend shape.

```csharp
public class BlendShape
{
    public string Key { get; set; }
    public double Value { get; set; }
}
```

### TransformRule

The `TransformRule` model defines a transformation rule for a parameter.

```csharp
public class TransformRule
{
    public string Name { get; set; }
    public string Func { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double DefaultValue { get; set; }
}
```

### TrackingParam

The `TrackingParam` model represents a parameter to send to VTube Studio.

```csharp
public class TrackingParam
{
    public string Id { get; set; }
    public double? Weight { get; set; }
    public double Value { get; set; }
}
```

## Communication Protocols

### iPhone to Bridge Communication

Communication with iPhone VTube Studio uses UDP protocol:

- Port: 21412 (default)
- Format: JSON
- Message Types:
  - Request: `iOSTrackingDataRequest`
  - Response: Tracking data in JSON format

Request format:
```json
{
    "messageType": "iOSTrackingDataRequest",
    "sentBy": "SharpBridge",
    "sendForSeconds": 10,
    "ports": [port]
}
```

### Bridge to VTube Studio Communication

Communication with VTube Studio uses WebSocket protocol:

- Default URL: `ws://localhost:8001`
- Format: JSON
- Key message types:
  - Authentication request/response
  - Parameter injection request/response
  - API state request/response
  - Parameter creation request/response

## Expression Evaluation

For evaluating mathematical expressions in transformation rules, we plan to use:

- **TBD** - Selection of expression evaluation library is pending

Requirements for the expression library:
- Support for basic arithmetic operations
- Support for mathematical functions
- Variables for tracking data parameters
- Efficient evaluation of expressions

## Authentication Flow

The authentication flow with VTube Studio follows these steps:

1. Check if a token file exists and read token if available
2. Request API state to determine if authentication is needed
3. If not authenticated and no token available, request a new token
4. If not authenticated but token available, authenticate with existing token
5. If token is invalid, request a new token
6. Store valid token to file for future use

## Parameter Transformation

The parameter transformation process involves:

1. Loading transformation rules from configuration file
2. For each tracking data update:
   - Create a context with tracking data values
   - For each transformation rule:
     - Evaluate the expression with the context
     - Apply min/max bounds
     - Add the resulting parameter to the output collection
   - Send the transformed parameters to VTube Studio 