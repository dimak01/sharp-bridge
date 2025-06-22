# Sharp Bridge Coding Standards

## SonarQube Compliance Guidelines

### 1. Nullable Reference Types

#### ✅ DO: Use null-forgiving operator for intentional null tests
```csharp
[Fact]
public void Constructor_WithNullParameter_ThrowsException()
{
    // Use null! for intentional null testing
    Assert.Throws<ArgumentNullException>(() => new Service(null!));
}
```

#### ✅ DO: Add null-forgiving operator when you know value won't be null
```csharp
// Path.GetDirectoryName() on temp files is never null
var directory = Path.GetDirectoryName(tempFilePath)!;
var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, fileName);
```

#### ❌ DON'T: Pass raw null to non-nullable parameters
```csharp
// Bad
new Service(null); // CS8625 error

// Good  
new Service(null!); // Intentional null for testing
```

### 2. IDisposable Pattern

#### ✅ DO: Always call GC.SuppressFinalize in Dispose
```csharp
public void Dispose()
{
    // Cleanup logic here
    _resource?.Dispose();
    
    // Always include this line
    GC.SuppressFinalize(this);
}
```

#### ✅ DO: Use Record.Exception for exception testing
```csharp
[Fact]
public void Dispose_CalledMultipleTimes_DoesNotThrow()
{
    // Act
    var exception = Record.Exception(() => service.Dispose());
    
    // Assert
    exception.Should().BeNull("Dispose should not throw");
}
```

### 3. Test Assertions

#### ✅ DO: Always include meaningful assertions
```csharp
[Fact]
public void Method_WhenCalled_DoesNotThrow()
{
    // Act
    var exception = Record.Exception(() => service.Method());
    
    // Assert - Never leave a test without assertions
    exception.Should().BeNull("Method should not throw exceptions");
}
```

#### ❌ DON'T: Rely on comments as assertions
```csharp
// Bad
public void Test()
{
    service.Method();
    // Should not throw - this is not an assertion!
}
```

### 4. Performance Guidelines

#### ✅ DO: Use char overloads for single characters
```csharp
// Good
if (text.StartsWith('"')) // char overload
if (text.IndexOf('"', 1) > 0) // char overload

// Bad
if (text.StartsWith("\"")) // string overload - slower
if (text.IndexOf("\"", 1) > 0) // string overload - slower
```

#### ✅ DO: Make methods static when they don't use instance data
```csharp
// If method doesn't access instance fields/properties
private static (string executable, string arguments) ParseCommand(string command)
{
    // Implementation
}
```

### 5. Reflection Best Practices

#### ✅ DO: Use null-forgiving operator for known-to-exist methods
```csharp
var method = typeof(Service)
    .GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Static);

// We know this method exists, so suppress the warning
var result = method!.Invoke(null, parameters);
var typedResult = ((string, string))result!;
```

### 6. Common Patterns

#### Constructor Null Validation Tests
```csharp
[Fact]
public void Constructor_WithNullParameter_ThrowsArgumentNullException()
{
    var exception = Assert.Throws<ArgumentNullException>(() => 
        new Service(null!, validParam2, validParam3));
    exception.ParamName.Should().Be("parameterName");
}
```

#### Async Method Testing
```csharp
[Fact]
public async Task Method_WithCondition_ReturnsExpectedResult()
{
    // Arrange
    // Setup

    // Act
    var result = await service.MethodAsync();

    // Assert
    result.Should().BeTrue();
    // Additional assertions
}
```

#### File System Testing with Temp Files
```csharp
[Fact]
public void Method_WithTempFile_WorksCorrectly()
{
    // Arrange
    var tempFile = Path.GetTempFileName();
    
    try
    {
        // Use Path.GetDirectoryName(tempFile)! - temp files always have directories
        var directory = Path.GetDirectoryName(tempFile)!;
        var fileName = Path.GetFileName(tempFile);
        
        // Act & Assert
        // Test logic here
    }
    finally
    {
        if (File.Exists(tempFile))
            File.Delete(tempFile);
    }
}
```

### 7. IDE Setup Recommendations

#### Enable nullable reference types in .csproj:
```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS8600;CS8601;CS8602;CS8604;CS8625</WarningsNotAsErrors>
</PropertyGroup>
```

#### Install SonarAnalyzer package:
```xml
<PackageReference Include="SonarAnalyzer.CSharp" Version="9.16.0.82469">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

## Quick Reference Checklist

Before committing code, verify:

- [ ] All null parameters in tests use `null!`
- [ ] All `IDisposable.Dispose()` methods call `GC.SuppressFinalize(this)`
- [ ] All tests have meaningful assertions (not just comments)
- [ ] String operations use char overloads where possible
- [ ] Methods that don't use instance data are static
- [ ] Reflection calls use `!` operator when method existence is guaranteed
- [ ] File system operations handle null directory names with `!`
- [ ] No unused variables remain in code

## Common SonarQube Rules Reference

| Rule ID | Description | Fix Pattern |
|---------|-------------|-------------|
| CS8625 | Cannot convert null literal | Use `null!` for intentional nulls |
| CS8602 | Dereference of possibly null | Use `!` when you know it's not null |
| CS8604 | Possible null reference argument | Use `!` for guaranteed non-null values |
| CA1063 | Implement IDisposable correctly | Add `GC.SuppressFinalize(this)` |
| S2699 | Tests should include assertions | Use `Record.Exception()` and proper assertions |
| S3242 | Method parameters should be declared with base types | Use interface types in parameters |
| S1481 | Unused local variables should be removed | Remove or use the variable |

This document should be updated after each SonarQube scan to capture new patterns. 