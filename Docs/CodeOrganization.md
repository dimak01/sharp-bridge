# Code Organization & Folder Structure

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

## Proposed Folder/Namespace Structure

### **Core Business Layer** (`SharpBridge.Core`)
```
Core/
├── Clients/
│   ├── VTubeStudioPCClient.cs
│   └── VTubeStudioPhoneClient.cs
├── Engines/
│   ├── TransformationEngine.cs
│   └── WindowsFirewallEngine.cs
├── Orchestrators/
│   └── ApplicationOrchestrator.cs
└── Services/
    ├── ApplicationInitializationService.cs
    ├── ExternalEditorService.cs
    ├── ParameterColorService.cs
    ├── PortDiscoveryService.cs
    ├── PortStatusMonitorService.cs
    └── ProcessInfo.cs
```

### **Configuration Management Layer** (`SharpBridge.Configuration`)
```
Configuration/
├── Managers/
│   ├── ConfigManager.cs
│   ├── ShortcutConfigurationManager.cs
│   └── ParameterTableConfigurationManager.cs
├── Services/
│   ├── Remediation/
│   │   ├── BaseConfigSectionRemediationService.cs
│   │   ├── GeneralSettingsConfigRemediationService.cs
│   │   ├── TransformationEngineConfigRemediationService.cs
│   │   ├── VTubeStudioPCConfigRemediationService.cs
│   │   └── VTubeStudioPhoneClientConfigRemediationService.cs
│   └── Validators/
│       ├── BaseConfigSectionValidator.cs
│       ├── GeneralSettingsConfigValidator.cs
│       ├── TransformationEngineConfigValidator.cs
│       ├── VTubeStudioPCConfigValidator.cs
│       └── VTubeStudioPhoneClientConfigValidator.cs
└── Factories/
    ├── ConfigSectionFieldExtractorsFactory.cs
    ├── ConfigSectionRemediationServiceFactory.cs
    └── ConfigSectionValidatorsFactory.cs
```

### **User Interface Layer** (`SharpBridge.UI`)
```
UI/
├── Managers/
│   ├── ConsoleModeManager.cs
│   └── ConsoleWindowManager.cs
├── Providers/
│   ├── InitializationContentProvider.cs
│   ├── MainStatusContentProvider.cs
│   ├── NetworkStatusContentProvider.cs
│   └── SystemHelpContentProvider.cs
├── Formatters/
│   ├── NetworkStatusFormatter.cs
│   ├── PCTrackingInfoFormatter.cs
│   ├── PhoneTrackingInfoFormatter.cs
│   └── TransformationEngineInfoFormatter.cs
└── Components/
    ├── KeyboardInputHandler.cs
    ├── TableFormatter.cs
    ├── TextColumnFormatter.cs
    ├── ProgressBarColumnFormatter.cs
    └── NumericColumnFormatter.cs
```

### **Infrastructure Layer** (`SharpBridge.Infrastructure`)
```
Infrastructure/
├── Wrappers/
│   ├── UdpClientWrapper.cs
│   ├── WebSocketWrapper.cs
│   ├── FileSystemWatcherWrapper.cs
│   └── SystemConsole.cs
├── Factories/
│   ├── UdpClientWrapperFactory.cs
│   ├── FileSystemWatcherFactory.cs
│   └── InterpolationMethodFactory.cs
├── Providers/
│   └── WindowsNetworkCommandProvider.cs
├── Services/
│   ├── WindowsFirewallAnalyzer.cs
│   └── ProcessLauncher.cs
└── Interop/
    ├── ComInterfaces.cs
    ├── NativeMethods.cs
    └── WindowsInterop.cs
```

### **Domain-Specific Layer** (`SharpBridge.Domain`)
```
Domain/
├── Managers/
│   └── VTubeStudioPCParameterManager.cs
├── Services/
│   ├── BezierInterpolationMethod.cs
│   ├── LinearInterpolationMethod.cs
│   └── SimpleRecoveryPolicy.cs
└── Adapters/
    └── VTSParameterPrefixAdapter.cs
```

## Namespace Convention:
- `SharpBridge.Core.{EntityType}`
- `SharpBridge.Configuration.{EntityType}`
- `SharpBridge.UI.{EntityType}`
- `SharpBridge.Infrastructure.{EntityType}`
- `SharpBridge.Domain.{EntityType}`

## Model Organization (Layer-Based)

Models are organized by **consuming layer** rather than data type, ensuring clear boundaries and reduced coupling between layers.

### **Configuration Models** (`SharpBridge.Models.Configuration`)
- Application settings and configuration sections
- **Consumed by**: Configuration layer, ConfigManagers
- **Examples**: `ApplicationConfig`, `VTubeStudioPCConfig`, `UserPreferences`, `ConfigValidationResult`

### **API Models** (`SharpBridge.Models.Api`)
- External communication contracts and responses
- **Consumed by**: Clients, API wrappers
- **Examples**: `VTSApiRequest<T>`, `AuthRequest`, `DiscoveryResponse`, `ParameterCreationRequest`

### **Domain Models** (`SharpBridge.Models.Domain`)
- Core business entities and concepts
- **Consumed by**: Engines, Services, Domain layer
- **Examples**: `VTSParameter`, `BlendShape`, `ParameterRuleDefinition`, `BezierInterpolation`

### **UI Models** (`SharpBridge.Models.UI`)
- User interface and display models
- **Consumed by**: UI layer, Formatters, ContentProviders
- **Examples**: `ConsoleRenderContext`, `PCTrackingInfo`, `PhoneTrackingInfo`, `ConsoleMode`

### **Infrastructure Models** (`SharpBridge.Models.Infrastructure`)
- System-level models and status
- **Consumed by**: Infrastructure layer, Wrappers, System services
- **Examples**: `ServiceStats`, `NetworkStatus`, `FirewallRule`, `PCClientStatus`

### **Events Models** (`SharpBridge.Models.Events`)
- Event data and context
- **Consumed by**: Event handlers, Orchestrators
- **Examples**: `RulesChangedEventArgs`, `RulesLoadResult`

## Proposed Model Folder Structure

### **Configuration Models** (`SharpBridge.Models.Configuration`)
```
Configuration/
├── ApplicationConfig.cs
├── VTubeStudioPCConfig.cs
├── VTubeStudioPhoneClientConfig.cs
├── TransformationEngineConfig.cs
├── UserPreferences.cs
├── GeneralSettingsConfig.cs
├── ConfigSectionTypes.cs
├── ConfigFieldState.cs
├── ConfigValidationResult.cs
├── ConfigLoadResult.cs
└── RemediationResult.cs
```

### **API Models** (`SharpBridge.Models.Api`)
```
Api/
├── VTSApiRequest.cs
├── VTSApiResponse.cs
├── AuthRequest.cs
├── AuthTokenRequest.cs
├── AuthenticationResponse.cs
├── AuthenticationTokenResponse.cs
├── InjectParamsRequest.cs
├── InputParameterListRequest.cs
├── InputParameterListResponse.cs
├── ParameterCreationRequest.cs
├── ParameterCreationResponse.cs
├── ParameterDeletionRequest.cs
└── DiscoveryResponse.cs
```

### **Domain Models** (`SharpBridge.Models.Domain`)
```
Domain/
├── VTSParameter.cs
├── BlendShape.cs
├── ParameterRuleDefinition.cs
├── ParameterTransformation.cs
├── ParameterExtremums.cs
├── TrackingParam.cs
├── BezierInterpolation.cs
├── LinearInterpolation.cs
├── IInterpolationDefinition.cs
├── Coordinates.cs
├── Point.cs
├── Shortcut.cs
├── ShortcutAction.cs
├── PhoneTrackingInfo.cs
├── PCTrackingInfo.cs
└── TransformationEngineInfo.cs
```

### **UI Models** (`SharpBridge.Models.UI`)
```
UI/
├── ConsoleMode.cs
├── ConsoleRenderContext.cs
├── ParameterTableColumn.cs
├── InitializationProgress.cs
├── StepInfo.cs
└── StepStatus.cs
```

### **Infrastructure Models** (`SharpBridge.Models.Infrastructure`)
```
Infrastructure/
├── ServiceStats.cs
├── NetworkStatus.cs
├── FirewallRule.cs
├── FirewallAnalysisResult.cs
├── PCClientStatus.cs
├── PhoneClientStatus.cs
├── TransformationEngineStatus.cs
├── ShortcutStatus.cs
└── InitializationStep.cs
```

### **Events Models** (`SharpBridge.Models.Events`)
```
Events/
├── RulesChangedEventArgs.cs
└── RulesLoadResult.cs
```

## Model Namespace Convention:
- `SharpBridge.Models.Configuration`
- `SharpBridge.Models.Api`
- `SharpBridge.Models.Domain`
- `SharpBridge.Models.UI`
- `SharpBridge.Models.Infrastructure`
- `SharpBridge.Models.Events`

## Interface Organization (Layer-Based)

Interfaces are organized by **consuming layer** and **entity type hierarchy**, mirroring the main entity organization for perfect consistency.

### **Core Business Interfaces** (`SharpBridge.Interfaces.Core`)
```
Core/
├── Clients/
│   ├── IVTubeStudioPCClient.cs
│   ├── IVTubeStudioPhoneClient.cs
│   └── IVTubeStudioPCAuthManager.cs
├── Engines/
│   ├── ITransformationEngine.cs
│   └── IFirewallEngine.cs
├── Orchestrators/
│   └── IApplicationOrchestrator.cs
├── Services/
│   ├── IInterpolationMethod.cs
│   └── IRecoveryPolicy.cs
├── Managers/
│   └── IVTubeStudioPCParameterManager.cs
└── Adapters/
    └── IVTSParameterAdapter.cs
```

### **Configuration Interfaces** (`SharpBridge.Interfaces.Configuration`)
```
Configuration/
├── Managers/
│   ├── IConfigManager.cs
│   ├── IShortcutConfigurationManager.cs
│   └── IParameterTableConfigurationManager.cs
├── Services/
│   ├── Remediation/
│   │   ├── IConfigRemediationService.cs
│   │   └── IConfigSectionRemediationService.cs
│   └── Validators/
│       ├── IConfigFieldValidator.cs
│       └── IConfigSectionValidator.cs
├── Factories/
│   ├── IConfigSectionFieldExtractorsFactory.cs
│   ├── IConfigSectionRemediationFactory.cs
│   └── IConfigSectionValidatorsFactory.cs
├── Extractors/
│   └── IConfigSectionFieldExtractor.cs
└── IConfigSection.cs
```

### **UI Interfaces** (`SharpBridge.Interfaces.UI`)
```
UI/
├── Managers/
│   ├── IConsoleModeManager.cs
│   └── IConsoleWindowManager.cs
├── Providers/
│   └── IConsoleModeContentProvider.cs
├── Formatters/
│   ├── IFormatter.cs
│   ├── ITableFormatter.cs
│   ├── ITableColumnFormatter.cs
│   └── INetworkStatusFormatter.cs
├── Components/
│   ├── IConsole.cs
│   ├── IKeyboardInputHandler.cs
│   ├── IMainStatusRenderer.cs
│   ├── ISystemHelpRenderer.cs
│   ├── IParameterColorService.cs
│   └── IFormattableObject.cs
└── IInitializable.cs
```

### **Infrastructure Interfaces** (`SharpBridge.Interfaces.Infrastructure`)
```
Infrastructure/
├── Wrappers/
│   ├── IUdpClientWrapper.cs
│   ├── IWebSocketWrapper.cs
│   └── IFileSystemWatcherWrapper.cs
├── Factories/
│   ├── IUdpClientWrapperFactory.cs
│   └── IFileSystemWatcherFactory.cs
├── Providers/
│   └── INetworkCommandProvider.cs
├── Services/
│   ├── IAppLogger.cs
│   ├── IAuthTokenProvider.cs
│   ├── IExternalEditorService.cs
│   ├── IFirewallAnalyzer.cs
│   ├── IPortDiscoveryService.cs
│   ├── IPortStatusMonitorService.cs
│   ├── IProcessLauncher.cs
│   ├── IServiceStats.cs
│   └── IServiceStatsProvider.cs
├── Interop/
│   └── IWindowsInterop.cs
└── IFileChangeWatcher.cs
```

### **Application Interfaces** (`SharpBridge.Interfaces.Application`)
```
Application/
└── IApplicationInitializationService.cs
```

### **Domain Interfaces** (`SharpBridge.Interfaces.Domain`)
```
Domain/
└── ITransformationRulesRepository.cs
```

## Interface Namespace Convention:
- `SharpBridge.Interfaces.Core.{EntityType}`
- `SharpBridge.Interfaces.Configuration.{EntityType}`
- `SharpBridge.Interfaces.UI.{EntityType}`
- `SharpBridge.Interfaces.Infrastructure.{EntityType}`
- `SharpBridge.Interfaces.Application`
- `SharpBridge.Interfaces.Domain`

## Test Organization (Mirrors Main Entity Structure)

Tests are organized to **exactly mirror** the main entity folder structure, ensuring perfect alignment between production code and test code.

### **Test Folder Structure**
```
Tests/
├── Core/
│   ├── Clients/
│   │   ├── VTubeStudioPCClientTests.cs
│   │   └── VTubeStudioPhoneClientTests.cs
│   ├── Engines/
│   │   ├── TransformationEngineTests.cs
│   │   └── FirewallEngineTests.cs
│   ├── Orchestrators/
│   │   └── ApplicationOrchestratorTests.cs
│   ├── Services/
│   │   ├── InterpolationMethodTests.cs
│   │   └── RecoveryPolicyTests.cs
│   ├── Managers/
│   │   └── VTubeStudioPCParameterManagerTests.cs
│   └── Adapters/
│       └── VTSParameterAdapterTests.cs
├── Configuration/
│   ├── Managers/
│   │   ├── ConfigManagerTests.cs
│   │   ├── ShortcutConfigurationManagerTests.cs
│   │   └── ParameterTableConfigurationManagerTests.cs
│   ├── Services/
│   │   ├── Remediation/
│   │   │   ├── ConfigRemediationServiceTests.cs
│   │   │   └── ConfigSectionRemediationServiceTests.cs
│   │   └── Validators/
│   │       ├── ConfigFieldValidatorTests.cs
│   │       └── ConfigSectionValidatorTests.cs
│   ├── Factories/
│   │   ├── ConfigSectionFieldExtractorsFactoryTests.cs
│   │   ├── ConfigSectionRemediationFactoryTests.cs
│   │   └── ConfigSectionValidatorsFactoryTests.cs
│   └── Extractors/
│       └── ConfigSectionFieldExtractorTests.cs
├── UI/
│   ├── Managers/
│   │   ├── ConsoleModeManagerTests.cs
│   │   └── ConsoleWindowManagerTests.cs
│   ├── Providers/
│   │   └── ConsoleModeContentProviderTests.cs
│   ├── Formatters/
│   │   ├── FormatterTests.cs
│   │   ├── TableFormatterTests.cs
│   │   ├── TableColumnFormatterTests.cs
│   │   └── NetworkStatusFormatterTests.cs
│   └── Components/
│       ├── ConsoleTests.cs
│       ├── KeyboardInputHandlerTests.cs
│       ├── MainStatusRendererTests.cs
│       ├── SystemHelpRendererTests.cs
│       └── ParameterColorServiceTests.cs
├── Infrastructure/
│   ├── Wrappers/
│   │   ├── UdpClientWrapperTests.cs
│   │   ├── WebSocketWrapperTests.cs
│   │   └── FileSystemWatcherWrapperTests.cs
│   ├── Factories/
│   │   ├── UdpClientWrapperFactoryTests.cs
│   │   └── FileSystemWatcherFactoryTests.cs
│   ├── Providers/
│   │   └── NetworkCommandProviderTests.cs
│   ├── Services/
│   │   ├── AppLoggerTests.cs
│   │   ├── AuthTokenProviderTests.cs
│   │   ├── ExternalEditorServiceTests.cs
│   │   ├── FirewallAnalyzerTests.cs
│   │   ├── PortDiscoveryServiceTests.cs
│   │   ├── PortStatusMonitorServiceTests.cs
│   │   ├── ProcessLauncherTests.cs
│   │   ├── ServiceStatsTests.cs
│   │   └── ServiceStatsProviderTests.cs
│   └── Interop/
│       └── WindowsInteropTests.cs
├── Application/
│   └── ApplicationInitializationServiceTests.cs
├── Domain/
│   └── TransformationRulesRepositoryTests.cs
├── Models/
│   ├── Configuration/
│   │   ├── GeneralSettingsConfigTests.cs
│   │   ├── UserPreferencesTests.cs
│   │   └── TransformConfigTests.cs
│   ├── Api/
│   │   ├── VTSParameterTests.cs
│   │   └── BlendShapeTests.cs
│   ├── Domain/
│   │   ├── ParameterRuleDefinitionTests.cs
│   │   ├── PhoneTrackingInfoTests.cs
│   │   ├── PCTrackingInfoTests.cs
│   │   └── TransformationEngineInfoTests.cs
│   ├── UI/
│   │   ├── ConsoleModeTests.cs
│   │   └── ParameterExtremumsTests.cs
│   ├── Infrastructure/
│   │   ├── ProcessInfoTests.cs
│   │   └── ServiceStatsTests.cs
│   └── Events/
│       ├── RulesChangedEventArgsTests.cs
│       └── RulesLoadResultTests.cs
└── Utilities/
    ├── ConsoleColorsTests.cs
    ├── InterpolationUtilityTests.cs
    └── [Other utility test files...]
```

### **Test Namespace Convention:**
- `SharpBridge.Tests.Core.{EntityType}`
- `SharpBridge.Tests.Configuration.{EntityType}`
- `SharpBridge.Tests.UI.{EntityType}`
- `SharpBridge.Tests.Infrastructure.{EntityType}`
- `SharpBridge.Tests.Application`
- `SharpBridge.Tests.Domain`
- `SharpBridge.Tests.Models.{Layer}`
- `SharpBridge.Tests.Utilities`

### **Test Organization Principles:**
1. **Perfect Mirroring**: Test folder structure exactly matches main entity structure
2. **Consistent Naming**: Test class names follow `{EntityName}Tests.cs` pattern
3. **Layer Isolation**: Tests are organized by the same layers as main entities
4. **Model Testing**: Models are tested in their respective layer folders
5. **Utility Testing**: Utilities maintain their own test folder for cross-cutting concerns

### **Test Data Organization:**
```
Tests/
└── TestData/
    ├── Configuration/
    │   ├── valid-config.json
    │   ├── invalid-config.json
    │   └── sample-transforms.json
    ├── Api/
    │   ├── sample-vts-parameters.json
    │   └── sample-blendshapes.json
    └── Domain/
        ├── sample-tracking-data.json
        └── sample-rule-definitions.json
```

## Key Benefits:
1. **Clear Separation of Concerns**: Each layer has a specific responsibility
2. **Dependency Direction**: Higher layers depend on lower layers, not vice versa
3. **Testability**: Each layer can be tested independently
4. **Maintainability**: Related functionality is grouped together
5. **Scalability**: Easy to add new features within the appropriate layer
6. **Test Discoverability**: Developers can easily find tests for any entity
7. **Consistent Structure**: Test organization never deviates from main entity organization
