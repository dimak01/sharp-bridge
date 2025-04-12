# Development Guide

This document provides guidance for developers working on the Sharp Bridge project, including setup instructions, testing approach, and guidelines for adding new features.

## Development Setup

### Prerequisites

- **.NET 6.0 SDK** or later
- **Visual Studio 2022** (recommended) or **Visual Studio Code** with C# extensions
  - Note: For debugging in VS Code, you may need to use the Microsoft-branded version due to licensing restrictions

### Building the Project

1. Clone the repository
2. Open the solution in Visual Studio or load the project in VS Code
3. Restore NuGet packages
4. Build the solution

```bash
# Command line build
dotnet restore
dotnet build
```

## Development Approach

We are using a Test-Driven Development (TDD) approach for this project. This means:

1. Write a failing test for the functionality you want to implement
2. Write the minimum amount of code to make the test pass
3. Refactor the code to improve design while keeping tests passing

### Testing Tools

The project uses the following testing tools:

1. **xUnit** - Modern testing framework for .NET applications
2. **Moq** - Mocking framework for isolating components during testing
3. **FluentAssertions** - Library for more readable assertions
4. **WireMock.NET** - For mocking HTTP/WebSocket responses

### Test Structure

Tests are organized by component in the `Tests` directory:

```
Tests/
├── TrackingTests/       # Tests for tracking data reception
├── TransformationTests/ # Tests for parameter transformation
├── VTubeStudioTests/    # Tests for VTube Studio communication
├── IntegrationTests/    # End-to-end tests
└── Mocks/               # Mock implementations for testing
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=UnitTest"
```

## Code Style and Standards

The project follows standard C# coding conventions:

- Use **PascalCase** for class names, public methods and properties
- Use **camelCase** for local variables and parameters
- Use **_camelCase** for private fields
- Include XML documentation comments for all public members
- Follow async/await best practices
- Keep methods small and focused

### Example

```csharp
/// <summary>
/// Processes tracking data according to transformation rules.
/// </summary>
/// <param name="trackingData">The tracking data to transform.</param>
/// <returns>Collection of transformed parameters.</returns>
public IEnumerable<TrackingParam> TransformData(TrackingResponse trackingData)
{
    // Implementation
}
```

## Project Structure

The project is organized into the following structure:

```
SharpBridge/
├── Models/       # Data models
├── Interfaces/   # Core component interfaces
├── Services/     # Implementation of interfaces
├── Tests/        # Test projects
├── Configs/      # Configuration files
└── Docs/         # Documentation
```

## Implementation Guidelines

### Adding a New Feature

1. Start by writing tests that define the expected behavior
2. Implement the feature to satisfy the tests
3. Refactor as needed while keeping tests passing
4. Update documentation to reflect the new feature

### Debugging Tips

- Use logging extensively to track data flow
- For UDP debugging, tools like Wireshark can be helpful
- For WebSocket debugging, browser tools or Postman can be used

## TDD Approach for This Project

For the Sharp Bridge project, we're following this TDD workflow:

1. **Define behavior through tests first** - Create tests that describe how components should behave
2. **Use mocks for isolation** - Initially use mocks to isolate components being tested
3. **Implement minimally** - Write just enough code to make tests pass
4. **Refactor** - Improve implementation without changing behavior
5. **Integration testing** - After components work individually, test them together

## Examples

### Example Test for Transformation Engine

```csharp
[Fact]
public async Task TransformData_WithValidRule_ReturnsTransformedParameter()
{
    // Arrange
    var engine = new TransformationEngine();
    var rule = new TransformRule 
    { 
        Name = "TestParam", 
        Func = "HeadRotY * -1", 
        Min = -40, 
        Max = 40, 
        DefaultValue = 0 
    };
    var mockFileSystem = new MockFileSystem();
    mockFileSystem.AddFile("test.json", new MockFileData(JsonSerializer.Serialize(new[] { rule })));
    
    await engine.LoadRulesAsync("test.json");
    
    var trackingData = new TrackingResponse
    {
        Rotation = new Coordinates { Y = 30 }
    };
    
    // Act
    var result = engine.TransformData(trackingData).ToList();
    
    // Assert
    result.Should().HaveCount(1);
    result[0].Id.Should().Be("TestParam");
    result[0].Value.Should().Be(-30);
}
```

### TBD Sections

- **Expression evaluation library selection and usage** - Pending decision
- **Specific performance optimization strategies** - To be determined after initial implementation
- **Continuous integration setup** - TBD 