# Development Guide

This guide helps developers understand how to work with the Sharp Bridge codebase effectively.

## Getting Started

### Prerequisites
- **.NET 8.0 SDK** - Required for building and testing
- **Visual Studio 2022** or **VS Code** - Recommended IDEs
- **Git** - For version control

### First-Time Setup
```bash
# Clone the repository
git clone <repository-url>
cd sharp-bridge

# Restore dependencies
dotnet restore ./sharp-bridge.sln

# Build the solution
dotnet build ./sharp-bridge.sln

# Run tests
dotnet test ./sharp-bridge.sln

# Run the application
dotnet run --project SharpBridge.csproj
```

### Project Structure Overview
```
src/                    # Source code
├── Interfaces/         # Contract definitions
├── Models/            # Data models and DTOs
├── Core/              # Core business logic
├── Domain/            # Domain-specific services
├── Infrastructure/    # External system integration
├── Configuration/     # Configuration management
└── UI/               # User interface components

tests/                 # Test projects (mirrors src/ structure)
configs/              # Configuration files
docs/                 # Documentation
```

## Development Workflow

### Daily Development
1. **Consider writing tests first** - TDD helps ensure robust implementations
2. **Follow naming conventions** - `{ClassName}Tests.cs` for test files
3. **Use dependency injection** - All dependencies injected via interfaces
4. **Keep interfaces focused** - Follow Interface Segregation Principle
5. **Test thoroughly** - Aim for 100% branch coverage

### Sharp Bridge-Specific Principles
- **Resilient, orchestrated data flow** - Components handle failures gracefully with built-in recovery
- **Console-based user interface** - Real-time status display with interactive controls
- **Consolidated configuration system** - Single `ApplicationConfig.json` with hot-reload capabilities
- **Parameter synchronization** - Automatic VTube Studio parameter management
- **Event-driven communication** - Use events for component interaction (e.g., `TrackingDataReceived`)
- **Reference existing documentation** - Always check docs when implementing new features

### Code Quality Standards
- **Static Analysis**: SonarAnalyzer.CSharp enabled
- **Nullable Reference Types**: Enabled for null safety
- **XML Documentation**: Required for all public APIs
- **Single Responsibility**: One class per file
- **Namespace Alignment**: Namespaces must match folder structure

### Build and Test Commands
```bash
# Build only
dotnet build

# Build with Release configuration
dotnet build -c Release

# Run all tests
dotnet test

# Run tests with coverage (using our script)
scripts/build/run-coverage.bat

# Run specific test project
dotnet test tests/Tests.csproj

# Run specific test class
dotnet test --filter "ClassName"
```

## Testing Strategy

### Test Organization
Tests **mirror the source structure** for easy navigation:
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

### Testing Frameworks
- **xUnit** - Primary testing framework
- **FluentAssertions** - Readable assertions
- **Moq** - Mocking framework
- **WireMock.Net** - HTTP service mocking

### Coverage Requirements
- **Target**: 100% branch coverage
- **Exclusions**: Generated code, interop code, main entry points
- **Coverage Tool**: Coverlet with Cobertura format
- **Excluded Files**: `ServiceRegistration.cs`, `Program.cs`, wrapper classes

### Writing Effective Tests
```csharp
[Fact]
public void MethodName_WhenCondition_ShouldExpectedBehavior()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    var sut = new ClassUnderTest(mockDependency.Object);
    
    // Act
    var result = sut.MethodToTest();
    
    // Assert
    result.Should().Be(expectedValue);
    mockDependency.Verify(x => x.Method(), Times.Once);
}
```

## Module Organization

### Why This Structure?
The codebase follows **layered architecture** with clear separation of concerns:

1. **Interfaces** - Contract definitions (what components can do)
2. **Models** - Data structures (what data looks like)
3. **Core** - Business logic (what the app does)
4. **Domain** - Domain-specific logic (how specific features work)
5. **Infrastructure** - External systems (how we talk to the outside world)
6. **Configuration** - Settings management (how we configure behavior)
7. **UI** - User interface (how users interact with the app)


## Sharp Bridge Components

### Core Components
- **VTubeStudioPhoneClient** - Receives tracking data from iPhone via UDP
- **TransformationEngine** - Processes tracking data according to configuration rules
- **VTubeStudioPCClient** - Sends transformed data to PC VTube Studio via WebSocket
- **ApplicationOrchestrator** - Coordinates flow between components with recovery logic
- **Console UI System** - Real-time status display and interactive controls


## Dependency Injection

Sharp Bridge uses **constructor injection** throughout the application. All dependencies are injected via interfaces, ensuring loose coupling and testability.

### DI Principles
- **Constructor injection only** - No property injection or service locator pattern
- **Interface-based** - All dependencies are abstracted through interfaces
- **Service lifetimes** - Chosen based on use case (Singleton, Scoped, Transient)

### Service Registration
All services are registered in `ServiceRegistration.cs`. This file contains the complete DI configuration and should be your reference for:
- How services are registered
- Service lifetimes and their rationale
- Service dependencies and factory patterns
- Configuration loading and remediation

## Working with Dependencies

### Adding New Dependencies (TDD Approach)
1. **Create interface** - Define contract in `Interfaces/`
2. **Create skeleton implementation** - Throw `NotImplementedException` initially
3. **Write failing tests** - Define expected behavior
4. **Implement service** - Make tests pass
5. **Register in DI** - Add to `ServiceRegistration.cs`
6. **Integrate and test** - Use in application and verify

### Common Dependencies
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Configuration.Json** - JSON configuration
- **Serilog** - Structured logging
- **System.Text.Json** - JSON processing
- **NCalcSync** - Mathematical expressions

## Configuration Management

### Configuration Files
- **ApplicationConfig.json** - Main application settings
- **UserPreferences.json** - User-specific settings
- **Parameter Transformations Config JSON** - Transformation rules

### Hot Reload
Configuration changes are automatically detected and reloaded:
- **ApplicationConfig.json** - Automatic reload
- **Transformation rules** - User notification (manual reload)

### Adding New Configuration
1. **Define model** - Create configuration class in `Models/Configuration/`
2. **Add to ApplicationConfig** - Include in main config structure
3. **Create validator** - Add validation logic
4. **Update DI registration** - Register configuration service

## Code Quality

### Static Analysis
- **SonarAnalyzer.CSharp** - Code quality rules
- **Nullable Reference Types** - Null safety
- **Implicit Usings** - Disabled for explicit imports

### Code Review Checklist
- [ ] All public methods have XML documentation
- [ ] Dependencies are injected via interfaces
- [ ] Tests cover all branches and edge cases
- [ ] Code follows naming conventions
- [ ] No hardcoded values (use configuration)
- [ ] Error handling is appropriate
- [ ] Performance considerations addressed

### Performance Guidelines
- **Minimize allocations** in hot paths
- **Use async/await** for I/O operations
- **Cache expensive operations** when appropriate
- **Profile before optimizing** - Measure first

## Documentation Updates

When making significant changes, consider updating:
- **README.md** - For user-facing changes
- **Architecture.md** - For architectural changes  
- **Release notes** - For new features or breaking changes
- **Relevant feature-specific documentation** - Keep docs in sync with code

### Documentation Principles
- **Always refer to existing documentation** when implementing new features
- **Follow the established architecture patterns** documented in Architecture.md
- **Update relevant documentation** when making significant changes
- **Ensure proper test coverage** for new functionality

## Troubleshooting

### Common Issues
- **Build failures** - Check .NET 8.0 SDK is installed
- **Test failures** - Verify all dependencies are mocked
- **Configuration issues** - Check JSON syntax and file paths
- **DI registration errors** - Verify all services are registered

### Debugging Tips
- **Use Serilog** - Structured logging with file output
- **Check console output** - Real-time status and error messages
- **Verify configuration** - Use F1 (System Help) mode
- **Test in isolation** - Unit tests help isolate issues

## Next Steps

- **Architecture** - See [Architecture.md](Architecture.md) for system design
- **User Guide** - See [User Guide](../UserGuide/README.md) for user documentation
- **Release Process** - See [ReleaseProcess.md](ReleaseProcess.md) for deployment
