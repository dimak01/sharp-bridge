# File Watcher Implementation Plan

## Overview
This document outlines the implementation plan for adding file modification tracking to the transformation configuration system. The goal is to provide real-time feedback about the status of the transformation rules file without automatic reloading. The file watcher will detect changes to the configuration file and update the up-to-date status, while maintaining the existing manual reload process (Alt+K).

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
- Add file modification tracking via IFileChangeWatcher
- Update IsConfigUpToDate status based on file change events
- Maintain existing manual reload process (Alt+K)
- No automatic config reloading

### 5. TransformationEngineInfo Model Updates
- Add IsConfigUpToDate property
- Update constructor and factory methods

