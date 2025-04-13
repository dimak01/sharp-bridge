# Implementation Details

This document provides detailed information about the core components, data models, and communication protocols used in the Sharp Bridge application.

## Core Components and Interfaces

### ITrackingReceiver

The `ITrackingReceiver` interface is responsible for receiving tracking data from iPhone VTube Studio via UDP.

```csharp
public interface ITrackingReceiver
{
    Task RunAsync(CancellationToken cancellationToken);
    event EventHandler<TrackingResponse> TrackingDataReceived;
}
```

Key responsibilities:
- Opens a UDP socket to receive tracking data
- Deserializes incoming JSON into `TrackingResponse` objects
- Raises events when new tracking data is received
- Handles reconnection if connection is lost
- Sends periodic tracking request messages to the iPhone

### TrackingReceiver

Our concrete implementation of `ITrackingReceiver` has these features:
- Uses UDP client wrapper for better testability
- Sends tracking requests at configurable intervals
- Processes incoming JSON data
- Raises events for valid tracking data
- Handles network and deserialization errors gracefully
- Supports timeout-based cancellation via tokens

### IUdpClientWrapper

This interface abstracts the UDP client for improved testability:

```csharp
public interface IUdpClientWrapper : IDisposable
{
    Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port);
    Task<UdpReceiveResult> ReceiveAsync(CancellationToken token);
    int Available { get; }
    bool Poll(int microseconds, SelectMode mode);
}
```

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
    Task RunAsync(TrackingReceiverConfig config, string transformConfigPath, CancellationToken cancellationToken);
}
```

Key responsibilities:
- Initializes and coordinates other components
- Connects tracking data events to transformation and forwarding
- Manages error handling and graceful shutdown

## Data Models

### TrackingReceiverConfig

The `TrackingReceiverConfig` model specifies configuration for the tracking receiver:

```csharp
public class TrackingReceiverConfig
{
    public string IphoneIpAddress { get; init; } = string.Empty;
    public int IphonePort { get; init; } = 21412;
    public int LocalPort { get; init; } = 21413;
    public int ReceiveBufferSize { get; init; } = 4096;
    public int RequestIntervalSeconds { get; init; } = 1;
    public int SendForSeconds { get; init; } = 10;
    public int ReceiveTimeoutMs { get; init; } = 2000;
}
```

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
    public Coordinates EyeRight { get; set; }
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
    [JsonPropertyName("k")]
    public string Key { get; set; }
    
    [JsonPropertyName("v")]
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

## Command-Line Interface

Sharp Bridge provides a command-line interface with the following options:

```
--ip, -i               iPhone IP address
--iphone-port, -p      iPhone port (default: 21412)
--local-port, -l       Local listening port (default: 21413)
--interval, -t         Request interval in seconds (default: 1)
--send-seconds, -s     Time to send data for in seconds (default: 10)
--timeout, -r          Receive timeout in milliseconds (default: 2000)
--interactive, -x      Launch in interactive mode
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

## Performance Monitoring

The application includes a built-in performance monitor that displays:

- Connection status
- Frames per second (current and average)
- Total frames received
- Uptime
- Facial tracking data visualization (head rotation, key facial expressions)

## Error Handling

The application implements robust error handling:

- Socket errors are detected and reported with helpful messages
- JSON parsing errors are caught and logged
- Reconnection is attempted after network errors
- Resources are properly disposed even on error

## Testing Strategy

The application includes a comprehensive test suite:

- Unit tests for core components
- Mock-based testing for network dependencies
- Coverage tracking with XPlat Code Coverage
- Performance monitoring for tracking real-time metrics 