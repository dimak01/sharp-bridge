# Code Organization Guide

## Quick Reference: Where to Put New Code

### **Core Business Layer** (`SharpBridge.Core`)
**Purpose**: Core business logic and application flow
- **Clients/**: External system communication (UDP/WebSocket) with auth & recovery
- **Engines/**: Core processing logic with complex algorithms and state management  
- **Orchestrators/**: High-level application flow coordination and recovery
- **Services/**: Specific business functionality implementation
- **Managers/**: Coordinates components/resources, manages lifecycle and configuration
- **Adapters/**: Adapts external data formats to internal models

### **Configuration Management Layer** (`SharpBridge.Configuration`)
**Purpose**: Application configuration, validation, and remediation
- **Managers/**: Configuration loading, saving, and management
- **Services/Remediation/**: Fixes configuration issues automatically
- **Services/Validators/**: Validates configuration data and rules
- **Factories/**: Creates configuration-related components
- **Extractors/**: Extracts configuration values from various sources
- **Utilities/**: Configuration helper functions and utilities

### **User Interface Layer** (`SharpBridge.UI`)
**Purpose**: Console UI, display formatting, and user interaction
- **Managers/**: UI state management and mode switching
- **Providers/**: Supplies content to UI components
- **Formatters/**: Formats data for display with verbosity levels
- **Components/**: Reusable UI components and input handlers
- **Utilities/**: UI helper functions and utilities

### **Infrastructure Layer** (`SharpBridge.Infrastructure`)
**Purpose**: System-level services, external dependencies, and cross-cutting concerns
- **Wrappers/**: Abstracts external dependencies for testability
- **Factories/**: Creates infrastructure components
- **Providers/**: Supplies system-level data and services
- **Services/**: System-level business logic (logging, file watching, etc.)
- **Repositories/**: Data access implementations
- **Interop/**: Windows API and COM interface wrappers
- **Utilities/**: Infrastructure helper functions

### **Domain-Specific Layer** (`SharpBridge.Domain`)
**Purpose**: Domain-specific business logic and algorithms
- **Services/**: Domain-specific business logic (interpolation methods, recovery policies)
- **Managers/**: Domain-specific resource management
- **Adapters/**: Domain-specific data transformations

## Entity Type Naming Conventions

| Type | Purpose | Naming Pattern | Examples |
|------|---------|----------------|----------|
| **Client** | External system communication | `{System}Client` | `VTubeStudioPCClient` |
| **Engine** | Core processing logic | `{Domain}Engine` | `TransformationEngine` |
| **Manager** | Coordinates components/resources | `{Resource}Manager` | `ConfigManager` |
| **Orchestrator** | High-level flow coordination | `{Scope}Orchestrator` | `ApplicationOrchestrator` |
| **Service** | Business functionality | `{Function}Service` | `ParameterColorService` |
| **Provider** | Supplies data/content | `{Content}Provider` | `MainStatusContentProvider` |
| **Factory** | Creates instances | `{Type}Factory` | `UdpClientWrapperFactory` |
| **Formatter** | Formats data for display | `{Data}Formatter` | `NetworkStatusFormatter` |
| **Wrapper** | Abstracts external dependencies | `{Dependency}Wrapper` | `UdpClientWrapper` |
| **Repository** | Data access implementation | `{Source}{Entity}Repository` | `FileBasedTransformationRulesRepository` |

### Service Sub-Types
- **RemediationService**: `{Config}RemediationService` - Fixes configuration issues
- **ValidatorService**: `{Config}Validator` - Validates configuration data
- **ContentProvider**: `{Content}ContentProvider` - Supplies UI content

## Model Organization (Layer-Based)

Models are organized by **consuming layer** rather than data type:

### **Configuration Models** (`SharpBridge.Models.Configuration`)
- Application settings and configuration sections
- **Consumed by**: Configuration layer, ConfigManagers
- **Examples**: `ApplicationConfig`, `VTubeStudioPCConfig`, `UserPreferences`

### **API Models** (`SharpBridge.Models.Api`)
- External communication contracts and responses
- **Consumed by**: Clients, API wrappers
- **Examples**: `VTSApiRequest<T>`, `AuthRequest`, `DiscoveryResponse`

### **Domain Models** (`SharpBridge.Models.Domain`)
- Core business entities and concepts
- **Consumed by**: Engines, Services, Domain layer
- **Examples**: `VTSParameter`, `ParameterRuleDefinition`, `BezierInterpolation`

### **UI Models** (`SharpBridge.Models.UI`)
- User interface and display models
- **Consumed by**: UI layer, Formatters, ContentProviders
- **Examples**: `ConsoleRenderContext`, `PCTrackingInfo`, `InitializationProgress`

### **Infrastructure Models** (`SharpBridge.Models.Infrastructure`)
- System-level models and status
- **Consumed by**: Infrastructure layer, Wrappers, System services
- **Examples**: `ServiceStats`, `NetworkStatus`, `FirewallRule`

### **Events Models** (`SharpBridge.Models.Events`)
- Event data and context
- **Consumed by**: Event handlers, Orchestrators
- **Examples**: `RulesChangedEventArgs`, `RulesLoadResult`

## Interface Organization

Interfaces mirror the main entity structure and are organized by **consuming layer**:

- **Core Interfaces**: `SharpBridge.Interfaces.Core.{EntityType}`
- **Configuration Interfaces**: `SharpBridge.Interfaces.Configuration.{EntityType}`
- **UI Interfaces**: `SharpBridge.Interfaces.UI.{EntityType}`
- **Infrastructure Interfaces**: `SharpBridge.Interfaces.Infrastructure.{EntityType}`
- **Domain Interfaces**: `SharpBridge.Interfaces.Domain`
- **Application Interfaces**: `SharpBridge.Interfaces.Application`

## Test Organization

Tests **exactly mirror** the main entity structure:

- **Test Location**: `tests/{Layer}/{EntityType}/{EntityName}Tests.cs`
- **Model Tests**: `tests/Models/{Layer}/{ModelName}Tests.cs`
- **Utility Tests**: `tests/Utilities/{UtilityName}Tests.cs`

## Decision Tree: Where Should I Put My New Code?

```
Is it business logic?
├── Yes → Is it core application logic?
│   ├── Yes → Core/
│   └── No → Is it domain-specific?
│       ├── Yes → Domain/
│       └── No → Configuration/ (if config-related) or Core/
└── No → Is it UI-related?
    ├── Yes → UI/
    └── No → Infrastructure/
```

## Key Principles

1. **Layer Separation**: Each layer has a specific responsibility
2. **Dependency Direction**: Higher layers depend on lower layers, never vice versa
3. **Consuming Layer**: Models are organized by who consumes them, not what they contain
4. **Perfect Mirroring**: Test structure exactly matches main entity structure
5. **Clear Naming**: Entity type names clearly indicate their purpose and responsibility
6. **Single Responsibility**: Each component has one clear purpose

## Quick Examples

**New VTube Studio API client?** → `Core/Clients/VTubeStudioNewClient.cs`
**New configuration validator?** → `Configuration/Services/Validators/NewConfigValidator.cs`
**New UI formatter?** → `UI/Formatters/NewDataFormatter.cs`
**New file wrapper?** → `Infrastructure/Wrappers/NewFileWrapper.cs`
**New domain service?** → `Domain/Services/NewDomainService.cs`
**New configuration model?** → `Models/Configuration/NewConfig.cs`
**New API response model?** → `Models/Api/NewApiResponse.cs`