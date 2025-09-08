# Initialization Progress Feature

## Problem Statement

Currently, Sharp Bridge shows no UI feedback during the 2-3 second initialization period, making the application appear unresponsive or "frozen" to users. The initialization involves several potentially time-consuming operations:

- Console window setup
- Transformation engine initialization (loading rules from file)
- File watcher setup
- **PC Client initialization** (port discovery, WebSocket connection, authentication)
- **Phone Client initialization** (UDP socket setup, initial data exchange)
- Parameter synchronization with VTube Studio

## Requirements

### Functional Requirements

1. **Real-time Progress Display**: Show initialization progress with step-by-step status updates
2. **Visual Feedback**: Display current step, elapsed time, and completion status
3. **Error Handling**: Show specific errors when initialization steps fail
4. **User Control**: Allow user to cancel initialization with Ctrl+C
5. **Seamless Transition**: Smoothly transition from initialization mode to main application mode

### Non-Functional Requirements

1. **Architecture Compliance**: Follow existing console mode system and content provider patterns
2. **Performance**: Minimal overhead during initialization (polling every 100ms max)
3. **Maintainability**: Leverage existing status tracking infrastructure
4. **User Experience**: Clear, readable progress indication with appropriate timing

## Technical Approach

### Core Strategy

**Event-Driven Status Updates**: Leverage existing client status enums (`PCClientStatus`, `PhoneClientStatus`) that are already updated during initialization. The initialization content provider will poll service stats and map technical statuses to user-friendly descriptions.

### Implementation Plan

#### Phase 1: Data Model & Enums

1. **Add Initialization ConsoleMode**
   ```csharp
   public enum ConsoleMode
   {
       Main = 0,
       SystemHelp = 1,
       NetworkStatus = 2,
       Initialization = 3  // NEW
   }
   ```

2. **Create Initialization Progress Enums**
   ```csharp
   public enum InitializationStep
   {
       ConsoleSetup,
       TransformationEngine,
       FileWatchers,
       PCClient,
       PhoneClient,
       ParameterSync,
       FinalSetup
   }

   public enum StepStatus
   {
       Pending,
       InProgress,
       Completed,
       Failed
   }
   ```

3. **Create InitializationProgress Model**
   ```csharp
   public class InitializationProgress
   {
       public InitializationStep CurrentStep { get; set; }
       public StepStatus Status { get; set; }
       public DateTime StartTime { get; set; }
       public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;
       public Dictionary<InitializationStep, StepInfo> Steps { get; set; }
       public bool IsComplete { get; set; }
   }

   public class StepInfo
   {
       public StepStatus Status { get; set; }
       public DateTime? StartTime { get; set; }
       public DateTime? EndTime { get; set; }
       public string ErrorMessage { get; set; }
       public TimeSpan? Duration => EndTime - StartTime;
   }
   ```

#### Phase 2: Initialization Content Provider

4. **Create InitializationContentProvider**
   - Implements `IConsoleModeContentProvider`
   - Polls service stats every 100ms during initialization
   - Maps client status enums to user-friendly step descriptions
   - Displays progress with visual indicators

5. **Status Mapping Strategy Using Description Attributes**
   
   The project already uses `AttributeHelper.GetDescription()` for enum-to-string mapping. We'll add `[Description]` attributes to the existing status enums:
   
   ```csharp
   // Update PCClientStatus enum with Description attributes
   public enum PCClientStatus
   {
       [Description("Preparing PC connection...")]
       Initializing,
       
       [Description("Discovering VTube Studio port...")]
       DiscoveringPort,
       
       [Description("Connecting to VTube Studio...")]
       Connecting,
       
       [Description("Authenticating with VTube Studio...")]
       Authenticating,
       
       [Description("[OK] PC connection established")]
       Connected,
       
       [Description("[FAIL] Failed to discover VTube Studio port")]
       PortDiscoveryFailed,
       
       [Description("[FAIL] Failed to connect to VTube Studio")]
       ConnectionFailed,
       
       [Description("[FAIL] Failed to authenticate with VTube Studio")]
       AuthenticationFailed,
       
       [Description("[FAIL] PC client initialization failed")]
       InitializationFailed,
       
       [Description("[FAIL] Error sending data to VTube Studio")]
       SendError,
       
       [Description("PC client disconnected")]
       Disconnected
   }

   // Update PhoneClientStatus enum with Description attributes
   public enum PhoneClientStatus
   {
       [Description("Preparing iPhone connection...")]
       Initializing,
       
       [Description("[OK] iPhone connection established")]
       Connected,
       
       [Description("Sending tracking requests to iPhone...")]
       SendingRequests,
       
       [Description("Receiving tracking data from iPhone...")]
       ReceivingData,
       
       [Description("[FAIL] iPhone client initialization failed")]
       InitializationFailed,
       
       [Description("[FAIL] Error sending requests to iPhone")]
       SendError,
       
       [Description("[FAIL] Error receiving data from iPhone")]
       ReceiveError,
       
       [Description("[FAIL] Error processing iPhone data")]
       ProcessingError,
       
       [Description("iPhone client disconnected")]
       Disconnected
   }
   ```
   
   **Mapping Implementation:**
   ```csharp
   // In InitializationContentProvider
   private string GetStatusDescription(IServiceStats stats)
   {
       if (stats.CurrentEntity is PCTrackingInfo)
       {
           var pcStatus = Enum.Parse<PCClientStatus>(stats.Status);
           return AttributeHelper.GetDescription(pcStatus);
       }
       else if (stats.CurrentEntity is PhoneTrackingInfo)
       {
           var phoneStatus = Enum.Parse<PhoneClientStatus>(stats.Status);
           return AttributeHelper.GetDescription(phoneStatus);
       }
       
       return stats.Status; // Fallback to raw status
   }
   ```

#### Phase 3: ApplicationOrchestrator Integration

6. **Modify ApplicationOrchestrator.InitializeAsync()**
   - Switch to initialization mode at start
   - Track progress through each initialization step
   - Update progress model as steps complete
   - Switch to main mode when complete

7. **Progress Tracking Integration**
   - Inject progress tracking into initialization flow
   - Update step status as operations complete
   - Handle errors gracefully with user feedback

#### Phase 4: Console Mode Manager Updates

8. **Register InitializationContentProvider**
   - Add to DI container registration
   - Update ConsoleModeManager validation
   - Ensure proper mode switching

### Visual Design

```
=== INITIALIZATION ===

Initializing Sharp Bridge...
Elapsed: 00:02.3

[OK] Console Setup                    (0.1s)
[OK] Loading Transformation Rules     (0.2s)  
[OK] Setting up File Watchers        (0.1s)
[RUN] PC Client                      (1.5s)
       └─ Discovering VTube Studio port...
[PEND] Phone Client                    (pending)
[PEND] Final Setup                     (pending)

Press Ctrl+C to cancel
```

### Status Indicators

- `[OK]` - Completed successfully
- `[RUN]` - Currently in progress
- `[PEND]` - Pending
- `[FAIL]` - Failed with error message

## Implementation Details

### Key Components

1. **InitializationContentProvider**: Renders progress display
2. **InitializationProgress**: Tracks current state and step information
3. **Status Mappers**: Convert technical statuses to user-friendly descriptions
4. **Progress Poller**: Updates display based on service stats

### Integration Points

1. **ConsoleModeManager**: Register and manage initialization mode
2. **ApplicationOrchestrator**: Coordinate initialization flow with progress tracking
3. **Service Stats**: Leverage existing `IServiceStats` for real-time status
4. **Console Rendering**: Use existing `IConsole.WriteLines()` for display

### Error Handling

- Display specific error messages for failed steps
- Allow graceful degradation (continue with partial initialization)
- Provide clear indication of what failed and why
- Maintain ability to cancel and retry

## Success Criteria

1. **User Experience**: No more "frozen" appearance during initialization
2. **Information**: Users can see exactly what's happening and how long it's taking
3. **Control**: Users can cancel initialization if needed
4. **Reliability**: Clear error messages when things go wrong
5. **Architecture**: Clean implementation following existing patterns

## Future Enhancements

- Estimated time remaining based on historical data
- More detailed sub-step progress for complex operations
- Configuration option to skip initialization mode for advanced users
- Progress persistence across application restarts
