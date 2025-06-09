# SharpBridge Linter Warnings - Organized List

## Summary
- **Total Warnings**: 359
- **Main Project**: 128 warnings  
- **Tests Project**: 231 warnings

---

## 🔴 PRIORITY 1: Nullable Reference Type Warnings (CS8xxx)

### CS8618: Non-nullable property/field must contain non-null value when exiting constructor

#### **Models Directory:**
- `Models\AuthenticationTokenResponse.cs(13,23)`: `AuthenticationToken` property
- `Models\AuthTokenRequest.cs(13,23)`: `PluginName` property  
- `Models\AuthTokenRequest.cs(17,23)`: `PluginDeveloper` property
- `Models\AuthTokenRequest.cs(21,23)`: `PluginIcon` property
- `Models\AuthenticationResponse.cs(17,23)`: `Reason` property
- `Models\AuthRequest.cs(13,23)`: `PluginName` property
- `Models\AuthRequest.cs(17,23)`: `PluginDeveloper` property
- `Models\AuthRequest.cs(21,23)`: `AuthenticationToken` property
- `Models\InjectParamsRequest.cs(18,23)`: `Mode` property
- `Models\InjectParamsRequest.cs(22,43)`: `ParameterValues` property
- `Models\DiscoveryResponse.cs(21,23)`: `InstanceId` property
- `Models\DiscoveryResponse.cs(25,23)`: `WindowTitle` property
- `Models\BlendShape.cs(13,23)`: `Key` property
- `Models\ParameterCreationResponse.cs(12,23)`: `ParameterName` property
- `Models\InputParameterListResponse.cs(17,23)`: `ModelName` property
- `Models\InputParameterListResponse.cs(21,23)`: `ModelId` property
- `Models\InputParameterListResponse.cs(25,42)`: `CustomParameters` property
- `Models\InputParameterListResponse.cs(29,42)`: `DefaultParameters` property
- `Models\ParameterDeletionRequest.cs(12,23)`: `ParameterName` property
- `Models\PhoneTrackingInfo.cs(22,28)`: `Rotation` property
- `Models\PhoneTrackingInfo.cs(25,28)`: `Position` property
- `Models\PhoneTrackingInfo.cs(28,28)`: `EyeLeft` property
- `Models\PhoneTrackingInfo.cs(31,28)`: `EyeRight` property
- `Models\PhoneTrackingInfo.cs(34,33)`: `BlendShapes` property
- `Models\ParameterCreationRequest.cs(13,23)`: `ParameterName` property
- `Models\ParameterCreationRequest.cs(17,23)`: `Explanation` property
- `Models\TrackingParam.cs(12,23)`: `Id` property
- `Models\TransformRule.cs(13,23)`: `Name` property
- `Models\TransformRule.cs(17,23)`: `Func` property
- `Models\VTSApiRequest.cs(13,23)`: `ApiName` property
- `Models\VTSApiRequest.cs(17,23)`: `ApiVersion` property
- `Models\VTSApiRequest.cs(21,23)`: `RequestId` property
- `Models\VTSApiRequest.cs(25,23)`: `MessageType` property
- `Models\VTSApiRequest.cs(29,18)`: `Data` property
- `Models\VTSApiResponse.cs(13,23)`: `ApiName` property
- `Models\VTSApiResponse.cs(17,23)`: `ApiVersion` property
- `Models\VTSApiResponse.cs(25,23)`: `MessageType` property
- `Models\VTSApiResponse.cs(29,23)`: `RequestId` property
- `Models\VTSApiResponse.cs(33,18)`: `Data` property

#### **Services Directory:**
- `Services\TransformationEngine.cs(63,16)`: `_lastError` field
- `Services\TransformationEngine.cs(63,16)`: `_configFilePath` field
- `Services\VTubeStudioPCClient.cs(62,16)`: `_lastTrackingData` field
- `Services\VTubeStudioPCClient.cs(62,16)`: `_authToken` field
- `Services\VTubeStudioPCClient.cs(62,16)`: `_lastInitializationError` field
- `Services\VTubeStudioPhoneClient.cs(47,12)`: `_lastTrackingData` field
- `Services\VTubeStudioPhoneClient.cs(47,12)`: `_lastInitializationError` field
- `Services\VTubeStudioPhoneClient.cs(47,12)`: `TrackingDataReceived` event
- `Services\ApplicationOrchestrator.cs(51,16)`: `_transformConfigPath` field

#### **Utilities Directory:**
- `Utilities\CommandLineParser.cs(27,23)`: `ConfigDirectory` property
- `Utilities\CommandLineParser.cs(32,23)`: `TransformConfigFilename` property
- `Utilities\CommandLineParser.cs(37,23)`: `PCConfigFilename` property
- `Utilities\CommandLineParser.cs(42,23)`: `PhoneConfigFilename` property

### CS8625: Cannot convert null literal to non-nullable reference type
- `Models\ServiceStats.cs(56,32)`: null assignment
- `Models\ServiceStats.cs(57,49)`: null assignment  
- `Models\TransformationEngineInfo.cs(35,52)`: null assignment
- `Utilities\PCTrackingInfoFormatter.cs(258,100)`: null assignment
- `Utilities\PhoneTrackingInfoFormatter.cs(290,100)`: null assignment
- `Services\VTubeStudioPhoneClient.cs(85,40)`: null assignment
- `Services\VTubeStudioPCClient.cs(286,44)`: null assignment
- `Services\TransformationEngine.cs(278,34)`: null assignment
- `Services\TransformationEngine.cs(279,21)`: null assignment
- `Services\TransformationEngine.cs(318,30)`: null assignment
- `Services\TransformationEngine.cs(323,30)`: null assignment
- `Services\VTubeStudioPCClient.cs(427,26)`: null assignment
- `Services\TransformationEngine.cs(448,25)`: null assignment

### CS8603: Possible null reference return
- `Models\PCTrackingInfo.cs(45,20)`: return value
- `Models\PCTrackingInfo.cs(55,20)`: return value
- `Utilities\ConsoleRenderer.cs(57,20)`: return value
- `Utilities\ConsoleRenderer.cs(212,20)`: return value
- `Services\PortDiscoveryService.cs(42,28)`: return value
- `Services\PortDiscoveryService.cs(60,32)`: return value
- `Services\PortDiscoveryService.cs(69,24)`: return value
- `Services\PortDiscoveryService.cs(74,24)`: return value

### CS8622: Nullability mismatch in delegate types
- `Services\ApplicationOrchestrator.cs(211,61)`: Event handler parameter nullability
- `Services\ApplicationOrchestrator.cs(216,61)`: Event handler parameter nullability

### CS8604: Possible null reference argument
- `Services\VTubeStudioPhoneClient.cs(145,13)`: ServiceStats constructor argument

### CS8602: Dereference of a possibly null reference
- `Utilities\WebSocketWrapper.cs(113,17)`: null dereference

---

## 🟡 PRIORITY 2: XML Documentation Warnings (CS1xxx)

### CS1591: Missing XML comment for publicly visible type or member
- `Interfaces\IUdpClientWrapper.cs(8,22)`: Interface
- `Interfaces\IUdpClientWrapper.cs(10,19)`: SendAsync method
- `Interfaces\IUdpClientWrapper.cs(11,32)`: ReceiveAsync method
- `Interfaces\IUdpClientWrapper.cs(12,13)`: Available property
- `Interfaces\IUdpClientWrapper.cs(13,14)`: Poll method
- `Models\PCTrackingInfo.cs(31,16)`: Constructor
- `Services\PortDiscoveryService.cs(21,16)`: Constructor
- `Services\PortDiscoveryService.cs(27,46)`: DiscoverAsync method
- `Services\PortDiscoveryService.cs(78,21)`: Dispose method
- `Services\TransformationEngine.cs(18,23)`: TransformationRule.Name property
- `Services\TransformationEngine.cs(19,27)`: TransformationRule.Expression property
- `Services\TransformationEngine.cs(20,23)`: TransformationRule.ExpressionString property
- `Services\TransformationEngine.cs(21,23)`: TransformationRule.Min property
- `Services\TransformationEngine.cs(22,23)`: TransformationRule.Max property
- `Services\TransformationEngine.cs(23,23)`: TransformationRule.DefaultValue property
- `Services\TransformationEngine.cs(25,16)`: TransformationRule constructor
- `Services\TransformationEngine.cs(63,16)`: TransformationEngine constructor
- `Services\VTubeStudioPhoneClient.cs(67,17)`: Dispose method
- `Utilities\CommandLineParser.cs(13,29)`: CommandLineDefaults.ConfigDirectory
- `Utilities\CommandLineParser.cs(14,29)`: CommandLineDefaults.TransformConfigFilename
- `Utilities\CommandLineParser.cs(15,29)`: CommandLineDefaults.PCConfigFilename
- `Utilities\CommandLineParser.cs(16,29)`: CommandLineDefaults.PhoneConfigFilename
- `Utilities\ConsoleColors.cs(9,29)`: Reset property
- `Utilities\ConsoleColors.cs(10,29)`: Bold property
- `Utilities\ConsoleColors.cs(11,29)`: Underline property
- `Utilities\ConsoleColors.cs(14,29)`: Healthy property
- `Utilities\ConsoleColors.cs(15,29)`: Warning property
- `Utilities\ConsoleColors.cs(16,29)`: Error property
- `Utilities\ConsoleColors.cs(17,29)`: Info property
- `Utilities\ConsoleColors.cs(18,29)`: Success property
- `Utilities\ConsoleColors.cs(21,29)`: Connected property
- `Utilities\ConsoleColors.cs(22,29)`: Connecting property
- `Utilities\ConsoleColors.cs(23,29)`: Disconnected property
- `Utilities\ConsoleColors.cs(24,29)`: Initializing property
- `Utilities\DisplayFormatting.cs(5,21)`: DisplayFormatting class
- `Utilities\DisplayFormatting.cs(7,26)`: FormatDuration method
- `Utilities\UdpClientWrapper.cs(10,18)`: UdpClientWrapper class
- `Utilities\UdpClientWrapper.cs(14,16)`: Constructor
- `Utilities\UdpClientWrapper.cs(22,20)`: Available property
- `Utilities\UdpClientWrapper.cs(24,21)`: Dispose method
- `Utilities\UdpClientWrapper.cs(29,21)`: Poll method
- `Utilities\UdpClientWrapper.cs(34,45)`: ReceiveAsync method
- `Utilities\UdpClientWrapper.cs(39,26)`: SendAsync method
- `Utilities\UdpClientWrapperFactory.cs(17,16)`: Constructor
- `Utilities\UdpClientWrapperFactory.cs(22,34)`: CreateForPhoneClient method
- `Utilities\UdpClientWrapperFactory.cs(27,34)`: CreateForPortDiscovery method
- `Utilities\WebSocketWrapper.cs(99,38)`: SendRequestAsync method

### CS1574: XML comment cref attribute could not be resolved
- `Interfaces\IVTubeStudioPCAuthManager.cs(16,30)`: InvalidOperationException reference

---

## 🟠 PRIORITY 3: Code Analysis Warnings

### CS1998: Async method lacks 'await' operators
- `Services\ApplicationOrchestrator.cs(304,38)`: async method without await
- `Services\VTubeStudioPCClient.cs(410,27)`: async method without await  
- `Services\VTubeStudioPCClient.cs(425,27)`: async method without await

### CS0219: Variable assigned but never used  
- `Tests\Utilities\KeyboardInputHandlerTests.cs(33,18)`: actionExecuted variable
- `Tests\Services\ApplicationOrchestratorTests.cs(118,26)`: testToken variable
- `Tests\Services\ApplicationOrchestratorTests.cs(1163,26)`: testToken variable

---

## 🔵 PRIORITY 4: xUnit Test Framework Warnings

### xUnit1013: Public method should be marked as Fact
- `Tests\Services\ApplicationOrchestratorTests.cs(291,21)`: Dispose method

### xUnit2004: Use Assert.False instead of Assert.Equal for booleans
- `Tests\Services\VTubeStudioPCClientTests.cs(101,17)`: Assert.Equal for boolean

---

## 📝 Test-Specific Nullable Warnings (231 additional CS8xxx warnings in Tests project)
*[Full list of test warnings omitted for brevity - these follow similar patterns to main project]*

---

## 🎯 Recommended Fix Order:

1. **Start with Models** - Fix CS8618 warnings by adding property initializers
2. **Fix Services** - Address field initialization and null reference issues
3. **Clean up Utilities** - Handle remaining nullable issues
4. **Add XML Documentation** - For public APIs if documentation is required
5. **Fix Async/Await** - Remove unnecessary async or add proper await
6. **Clean up Tests** - Fix test-specific issues

Would you like to start with the **Models directory CS8618 warnings**? These are the most straightforward to fix and will make a big dent in the warning count. 