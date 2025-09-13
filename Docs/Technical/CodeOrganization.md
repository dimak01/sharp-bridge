# Code Organization

This document describes the **Development View (3d)** of the Sharp Bridge architecture, focusing on module structure, code organization patterns, and development practices.

## Module Structure

The codebase follows a **layered architecture** with clear separation of concerns:

```
src/
├── Interfaces/           # Contract definitions
│   ├── Configuration/    # Configuration interfaces
│   ├── Core/            # Core business interfaces
│   ├── Domain/          # Domain service interfaces
│   ├── Infrastructure/  # Infrastructure interfaces
│   └── UI/              # User interface interfaces
├── Models/              # Data models and DTOs
│   ├── Api/             # VTube Studio API models
│   ├── Configuration/   # Configuration models
│   ├── Domain/          # Domain models
│   ├── Events/          # Event models
│   ├── Infrastructure/  # Infrastructure models
│   └── UI/              # UI models
├── Core/                # Core business logic
│   ├── Adapters/        # Data adapters
│   ├── Clients/         # External service clients
│   ├── Engines/         # Business engines
│   ├── Managers/        # Business managers
│   ├── Orchestrators/   # Application orchestration
│   └── Services/        # Core services
├── Domain/              # Domain services
│   └── Services/        # Domain-specific services
├── Infrastructure/      # Infrastructure implementations
│   ├── Factories/       # Object factories
│   ├── Interop/         # Platform interop
│   ├── Providers/       # External providers
│   ├── Repositories/    # Data repositories
│   ├── Services/        # Infrastructure services
│   ├── Utilities/       # Infrastructure utilities
│   └── Wrappers/        # System wrapper classes
├── Configuration/       # Configuration management
│   ├── Extractors/      # Configuration extractors
│   ├── Factories/       # Configuration factories
│   ├── Managers/        # Configuration managers
│   ├── Services/        # Configuration services
│   └── Utilities/       # Configuration utilities
└── UI/                  # User interface
    ├── Components/      # UI components
    ├── Formatters/      # Data formatters
    ├── Managers/        # UI managers
    ├── Providers/       # Content providers
    └── Utilities/       # UI utilities
```

## Architectural Layers

### 1. **Interfaces Layer** (`src/Interfaces/`)
- **Purpose**: Contract definitions and abstractions
- **Organization**: Mirrors implementation structure
- **Key Patterns**: Interface Segregation, Dependency Inversion
- **Examples**: `IConfigManager`, `IVTubeStudioPCClient`, `IFormatter`

### 2. **Models Layer** (`src/Models/`)
- **Purpose**: Data models, DTOs, and value objects
- **Organization**: Categorized by domain/usage
- **Key Patterns**: Immutable DTOs, Value Objects
- **Examples**: `ApplicationConfig`, `TransformationRule`, `ConsoleMode`

### 3. **Core Layer** (`src/Core/`)
- **Purpose**: Core business logic and orchestration
- **Organization**: Business domain grouping
- **Key Patterns**: Orchestrator, Manager, Service
- **Examples**: `ApplicationOrchestrator`, `TransformationEngine`, `VTubeStudioPCClient`

### 4. **Domain Layer** (`src/Domain/`)
- **Purpose**: Domain-specific business logic
- **Organization**: Domain service grouping
- **Key Patterns**: Domain Service, Strategy
- **Examples**: `BezierInterpolationMethod`, `LinearInterpolationMethod`, `SimpleRecoveryPolicy`

### 5. **Infrastructure Layer** (`src/Infrastructure/`)
- **Purpose**: External system integration and platform-specific code
- **Organization**: Technical concern grouping
- **Key Patterns**: Repository, Factory, Wrapper, Adapter
- **Examples**: `FileBasedTransformationRulesRepository`, `WebSocketWrapper`, `WindowsFirewallAnalyzer`

### 6. **Configuration Layer** (`src/Configuration/`)
- **Purpose**: Configuration management and validation
- **Organization**: Configuration concern grouping
- **Key Patterns**: Manager, Factory, Validator, Remediation
- **Examples**: `ConfigManager`, `ConfigRemediationService`, `ShortcutConfigurationManager`

### 7. **UI Layer** (`src/UI/`)
- **Purpose**: User interface and presentation logic
- **Organization**: UI concern grouping
- **Key Patterns**: Component, Formatter, Provider, Manager
- **Examples**: `ConsoleModeManager`, `MainStatusContentProvider`, `ParameterTableFormatter`

## Code Organization Patterns

### **Interface Segregation**
- **Principle**: Interfaces are focused and cohesive
- **Implementation**: Separate interfaces for different concerns
- **Examples**: 
  - `IConfigManager` vs `IConfigSectionFieldExtractor`
  - `IFormatter` vs `IConsoleModeContentProvider`

### **Dependency Inversion**
- **Principle**: High-level modules depend on abstractions
- **Implementation**: All dependencies injected via interfaces
- **Examples**: `ApplicationOrchestrator` depends on `IConfigManager`, not concrete implementation

### **Factory Pattern**
- **Purpose**: Object creation abstraction
- **Implementation**: Factory interfaces with concrete implementations
- **Examples**: `IUdpClientWrapperFactory`, `IConfigSectionFieldExtractorsFactory`

### **Repository Pattern**
- **Purpose**: Data access abstraction
- **Implementation**: Repository interfaces with file-based implementations
- **Examples**: `ITransformationRulesRepository` → `FileBasedTransformationRulesRepository`

### **Wrapper Pattern**
- **Purpose**: System component abstraction
- **Implementation**: Wrapper classes around system components
- **Examples**: `WebSocketWrapper`, `UdpClientWrapper`, `FileSystemWatcherWrapper`

### **Strategy Pattern**
- **Purpose**: Algorithm variation
- **Implementation**: Strategy interfaces with concrete implementations
- **Examples**: `IInterpolationMethod` → `BezierInterpolationMethod`, `LinearInterpolationMethod`

## Testing Organization

The test structure **mirrors the source structure** for easy navigation:

```
tests/
├── Configuration/        # Configuration layer tests
├── Core/                # Core layer tests
├── Domain/              # Domain layer tests
├── Infrastructure/      # Infrastructure layer tests
├── Models/              # Model tests
├── UI/                  # UI layer tests
└── TestData/            # Test data and fixtures
```

### **Testing Patterns**

#### **Test Organization**
- **Mirror Structure**: Tests follow same folder structure as source
- **Naming Convention**: `{ClassName}Tests.cs`
- **Test Data**: Centralized in `TestData/` folder

#### **Testing Frameworks**
- **xUnit**: Primary testing framework
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking framework
- **WireMock.Net**: HTTP service mocking

#### **Coverage Strategy**
- **Target**: 100% branch coverage
- **Exclusions**: Generated code, interop code, main entry points
- **Coverage Tool**: Coverlet with Cobertura format
- **Excluded Files**: `ServiceRegistration.cs`, `Program.cs`, wrapper classes

## Development Practices

### **Code Quality**
- **Static Analysis**: SonarAnalyzer.CSharp for code quality
- **Nullable Reference Types**: Enabled for null safety
- **Implicit Usings**: Disabled for explicit imports
- **Documentation**: XML documentation for public APIs

### **Build Configuration**
- **Target Framework**: .NET 8.0
- **Output Type**: Self-contained executable
- **Documentation**: XML documentation file generation enabled
- **Package References**: Minimal, focused dependencies

### **Dependency Management**
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Configuration**: Microsoft.Extensions.Configuration.Json
- **Logging**: Serilog with file and console sinks
- **JSON Processing**: System.Text.Json
- **Mathematical Expressions**: NCalcSync

### **File Organization**
- **Single Responsibility**: One class per file
- **Namespace Alignment**: Namespaces match folder structure
- **Using Statements**: Organized by source (Microsoft, third-party, local)
- **Configuration Files**: Copied to output directory

## Key Development Principles

### **1. Separation of Concerns**
- Each layer has a distinct responsibility
- Interfaces define contracts between layers
- Dependencies flow inward (Infrastructure → Core → Domain)

### **2. Testability**
- All components are unit testable
- Dependencies are injected via interfaces
- Mock-friendly design patterns

### **3. Maintainability**
- Clear module boundaries
- Consistent naming conventions
- Comprehensive test coverage

### **4. Extensibility**
- Plugin architecture for formatters and interpolation methods
- Configuration-driven behavior
- Interface-based design for easy extension

### **5. Performance**
- Efficient data structures
- Minimal allocations in hot paths
- Async/await for I/O operations

## Module Dependencies

### **Dependency Flow**
```
UI Layer → Core Layer → Domain Layer
    ↓         ↓
Configuration Layer  Infrastructure Layer
    ↓         ↓
    Models Layer ← Interfaces Layer
```

### **Key Dependencies**
- **Core** depends on **Domain** and **Infrastructure**
- **UI** depends on **Core** and **Configuration**
- **Infrastructure** implements **Interfaces**
- **Configuration** manages **Models**

## Next Steps

- **Architecture** - See [Architecture.md](Architecture.md) for component relationships
- **Deployment** - See [Deployment.md](Deployment.md) for system requirements
