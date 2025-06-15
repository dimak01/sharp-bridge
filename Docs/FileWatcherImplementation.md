# File Watcher Implementation Plan

## Overview
This document outlines the implementation plan for adding file modification tracking to the transformation configuration system. The goal is to provide real-time feedback when the transformation rules file is modified, while maintaining testability and clean architecture.

## Core Components

### 1. File Change Watcher Interface
```csharp
public interface IFileChangeWatcher : IDisposable
{
    event EventHandler<FileChangeEventArgs> FileChanged;
    void StartWatching(string filePath);
    void StopWatching();
}

public class FileChangeEventArgs : EventArgs
{
    public string FilePath { get; }
    public DateTime ChangeTime { get; }
    
    public FileChangeEventArgs(string filePath)
    {
        FilePath = filePath;
        ChangeTime = DateTime.UtcNow;
    }
}
```

### 2. FileSystemWatcher Implementation
```csharp
public class FileSystemChangeWatcher : IFileChangeWatcher
{
    private readonly IAppLogger _logger;
    private FileSystemWatcher? _watcher;
    private string? _currentFilePath;
    
    public event EventHandler<FileChangeEventArgs>? FileChanged;
    
    // Implementation details as outlined in the design
}
```

### 3. Mock Implementation for Testing
```csharp
public class MockFileChangeWatcher : IFileChangeWatcher
{
    public event EventHandler<FileChangeEventArgs>? FileChanged;
    
    // Mock implementation with SimulateFileChange helper
}
```

### 4. TransformationEngine Updates
- Add file modification tracking
- Integrate with IFileChangeWatcher
- Update service stats to include up-to-date status

### 5. TransformationEngineInfo Model Updates
- Add IsConfigUpToDate property
- Update constructor and factory methods

### 6. Formatter Updates
- Update status display to use actual file modification status
- Enhance color coding for up-to-date status

## Implementation Steps

1. **Create Interface and Event Args**
   - Create `IFileChangeWatcher` interface
   - Create `FileChangeEventArgs` class
   - Add XML documentation

2. **Implement FileSystemChangeWatcher**
   - Create concrete implementation
   - Add proper error handling
   - Implement event debouncing
   - Add logging

3. **Create Mock Implementation**
   - Create `MockFileChangeWatcher`
   - Add `SimulateFileChange` helper method
   - Add XML documentation

4. **Update TransformationEngine**
   - Add IFileChangeWatcher dependency
   - Add file modification tracking
   - Implement file change handler
   - Update LoadRulesAsync to use watcher
   - Add IsConfigUpToDate method

5. **Update TransformationEngineInfo**
   - Add IsConfigUpToDate property
   - Update constructor
   - Update factory methods

6. **Update Formatter**
   - Modify status display logic
   - Update color coding
   - Add tests for new formatting

7. **Add Tests**
   - Unit tests for FileSystemChangeWatcher
   - Unit tests for TransformationEngine with mock watcher
   - Integration tests for file watching
   - Formatter tests for new status display

8. **Update DI Registration**
   - Register IFileChangeWatcher
   - Update TransformationEngine registration

## Testing Strategy

### Unit Tests
1. **FileSystemChangeWatcher Tests**
   - Test StartWatching
   - Test StopWatching
   - Test event firing
   - Test error handling

2. **TransformationEngine Tests**
   - Test file change detection
   - Test up-to-date status
   - Test config reloading

3. **Formatter Tests**
   - Test status display
   - Test color coding
   - Test different states

### Integration Tests
1. **File Watching Tests**
   - Test actual file system changes
   - Test multiple rapid changes
   - Test file deletion/recreation

2. **End-to-End Tests**
   - Test complete flow from file change to status update
   - Test user interaction (Alt+K reload)

## Error Handling

1. **File System Errors**
   - Handle file not found
   - Handle access denied
   - Handle file in use

2. **Watcher Errors**
   - Handle watcher initialization failures
   - Handle event processing errors
   - Handle multiple events

3. **Recovery**
   - Automatic watcher restart
   - Graceful degradation
   - User notification

## Logging

1. **File Watcher Events**
   - Log file changes
   - Log watcher start/stop
   - Log errors

2. **Transformation Engine**
   - Log config file changes
   - Log reload attempts
   - Log status changes

## User Experience

1. **Status Display**
   - Clear up-to-date indicator
   - Color coding (green/red)
   - Reload prompt

2. **Error Feedback**
   - Clear error messages
   - Recovery instructions
   - Status updates

## Future Enhancements

1. **Performance**
   - Optimize event handling
   - Reduce file system impact
   - Improve debouncing

2. **Features**
   - Auto-reload option
   - Change history
   - Diff display

3. **Monitoring**
   - Change statistics
   - Performance metrics
   - Health checks

## Dependencies

1. **Required**
   - System.IO.FileSystemWatcher
   - IAppLogger
   - Existing transformation engine components

2. **Optional**
   - Performance monitoring
   - Change tracking
   - Diff tools

## Timeline

1. **Phase 1: Core Implementation**
   - Interface and basic implementation
   - TransformationEngine integration
   - Basic tests

2. **Phase 2: Testing & Refinement**
   - Comprehensive tests
   - Error handling
   - Performance optimization

3. **Phase 3: User Experience**
   - Status display
   - Error feedback
   - Documentation

4. **Phase 4: Future Enhancements**
   - Additional features
   - Performance improvements
   - Monitoring tools 