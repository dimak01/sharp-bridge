# "Up to Date" Monitoring Implementation Plan

## Overview
This document outlines the implementation plan for adding file modification tracking to determine if the loaded transformation rules are up to date with the latest configuration file. This feature will provide users with clear feedback about whether their current rules match the latest saved configuration.

## Core Changes

### 1. TransformationEngine Updates
```csharp
public class TransformationEngine
{
    private DateTime _lastFileModificationTime;
    private string _configFilePath = string.Empty;
    
    // Add to LoadRulesAsync:
    private async Task LoadRulesAsync(string filePath)
    {
        // ... existing validation code ...
        
        // Store file modification time when loading
        _lastFileModificationTime = File.GetLastWriteTimeUtc(filePath);
        _configFilePath = filePath;
        
        // ... rest of existing loading code ...
    }
    
    // Add new method to check if config is up to date
    public bool IsConfigUpToDate()
    {
        if (string.IsNullOrEmpty(_configFilePath) || !File.Exists(_configFilePath))
            return false;
            
        var currentModTime = File.GetLastWriteTimeUtc(_configFilePath);
        return currentModTime <= _lastFileModificationTime;
    }
}
```

### 2. TransformationEngineInfo Model Updates
```csharp
public class TransformationEngineInfo
{
    public string ConfigFilePath { get; }
    public int ValidRulesCount { get; }
    public IReadOnlyList<RuleInfo> InvalidRules { get; }
    public bool IsConfigUpToDate { get; }  // New property
    
    public TransformationEngineInfo(
        string configFilePath,
        int validRulesCount,
        IReadOnlyList<RuleInfo> invalidRules,
        bool isConfigUpToDate)  // New parameter
    {
        ConfigFilePath = configFilePath;
        ValidRulesCount = validRulesCount;
        InvalidRules = invalidRules;
        IsConfigUpToDate = isConfigUpToDate;
    }
}
```

### 3. ServiceStats Updates
```csharp
public IServiceStats GetServiceStats()
{
    // ... existing counter setup ...
    
    var currentEntity = new TransformationEngineInfo(
        configFilePath: _configFilePath ?? string.Empty,
        validRulesCount: _rules.Count,
        invalidRules: _invalidRules.AsReadOnly(),
        isConfigUpToDate: IsConfigUpToDate());  // Add up-to-date check
    
    // ... rest of existing code ...
}
```

### 4. Formatter Updates
```csharp
private static void AppendConfigurationInfo(StringBuilder builder, IServiceStats serviceStats)
{
    if (serviceStats.CurrentEntity is TransformationEngineInfo engineInfo)
    {
        var colorized_config_path = ConsoleColors.Colorize(engineInfo.ConfigFilePath, ConsoleColors.ConfigPathColor);
        builder.AppendLine($"Config File Path: {colorized_config_path}");
        
        // Use actual up-to-date status from engine info
        var upToDateStatus = engineInfo.IsConfigUpToDate ? "Yes" : "No";
        var colorizedStatus = engineInfo.IsConfigUpToDate 
            ? ConsoleColors.Colorize(upToDateStatus, ConsoleColors.Success)
            : ConsoleColors.Colorize(upToDateStatus, ConsoleColors.Warning);
            
        builder.AppendLine($"Up to Date: {colorizedStatus} | Load Attempts: {serviceStats.Counters[HOT_RELOAD_ATTEMPTS_KEY]}, Successful: {serviceStats.Counters[HOT_RELOAD_SUCCESSES_KEY]}");
    }
}
```

## Implementation Steps

1. **Update TransformationEngine**
   - Add file modification tracking fields
   - Update LoadRulesAsync to store modification time
   - Add IsConfigUpToDate method
   - Update GetServiceStats to include up-to-date status

2. **Update TransformationEngineInfo**
   - Add IsConfigUpToDate property
   - Update constructor
   - Update factory methods if any

3. **Update Formatter**
   - Modify status display logic
   - Update color coding
   - Add tests for new formatting

4. **Add Tests**
   - Unit tests for IsConfigUpToDate
   - Unit tests for file modification tracking
   - Formatter tests for new status display

## Testing Strategy

### Unit Tests
1. **TransformationEngine Tests**
   ```csharp
   [Fact]
   public void IsConfigUpToDate_WhenFileNotModified_ReturnsTrue()
   {
       // Arrange
       var engine = new TransformationEngine(mockLogger);
       await engine.LoadRulesAsync("test.json");
       
       // Act
       var result = engine.IsConfigUpToDate();
       
       // Assert
       Assert.True(result);
   }
   
   [Fact]
   public void IsConfigUpToDate_WhenFileModified_ReturnsFalse()
   {
       // Arrange
       var engine = new TransformationEngine(mockLogger);
       await engine.LoadRulesAsync("test.json");
       File.SetLastWriteTimeUtc("test.json", DateTime.UtcNow);
       
       // Act
       var result = engine.IsConfigUpToDate();
       
       // Assert
       Assert.False(result);
   }
   ```

2. **Formatter Tests**
   ```csharp
   [Theory]
   [InlineData(true, "Yes", ConsoleColors.Success)]
   [InlineData(false, "No", ConsoleColors.Warning)]
   public void Format_WithUpToDateStatus_ShowsCorrectColor(bool isUpToDate, string expectedText, string expectedColor)
   {
       // Arrange
       var engineInfo = new TransformationEngineInfo(
           "test.json", 0, new List<RuleInfo>(), isUpToDate);
       var serviceStats = CreateMockServiceStats(engineInfo);
       
       // Act
       var result = _formatter.Format(serviceStats);
       
       // Assert
       result.Should().Contain($"Up to Date: {expectedText}");
       result.Should().Contain(expectedColor);
   }
   ```

### Integration Tests
1. **File Modification Tests**
   - Test with actual file system changes
   - Test with file deletion/recreation
   - Test with file access issues

## Error Handling

1. **File System Errors**
   - Handle file not found
   - Handle access denied
   - Handle file in use

2. **Edge Cases**
   - Handle file deletion after load
   - Handle file modification during check
   - Handle file system errors

## Logging

1. **Status Changes**
   - Log when config becomes out of date
   - Log file modification times
   - Log file access errors

2. **User Feedback**
   - Clear status messages
   - Error notifications
   - Reload prompts

## User Experience

1. **Status Display**
   - Clear up-to-date indicator
   - Color coding (green/red)
   - Reload prompt when out of date

2. **Error Feedback**
   - Clear error messages
   - Recovery instructions
   - Status updates

## Dependencies

1. **Required**
   - System.IO.File
   - Existing transformation engine components
   - Console color utilities

## Timeline

1. **Phase 1: Core Implementation**
   - Add file modification tracking
   - Update model and formatter
   - Basic tests

2. **Phase 2: Testing & Refinement**
   - Comprehensive tests
   - Error handling
   - Edge case handling

3. **Phase 3: User Experience**
   - Status display
   - Error feedback
   - Documentation

## Next Steps
After completing this implementation, we can proceed with the File Watcher feature to provide real-time notifications of file changes. 