# Configuration Consolidation Pre-Work Checklist

## Overview

This checklist covers the housekeeping tasks that need to be completed before starting the main configuration consolidation phases. These tasks eliminate command-line arguments, clean up inappropriate configuration usage, and simplify the TransformationEngine interface.

**Estimated Time**: 1-1.5 days

## 🧹 Task 1: Remove Unused Configure Methods

### ServiceRegistration.cs Cleanup
- [ ] **Remove `ConfigureVTubeStudioPhoneClient` method** (lines 189-205)
  - [ ] Delete method entirely
  - [ ] Verify no references exist in codebase
- [ ] **Remove `ConfigureVTubeStudioPC` method** (lines 211-228)  
  - [ ] Delete method entirely
  - [ ] Verify no references exist in codebase
- [ ] **Test**: Verify application builds and runs without these methods

### Search for Inappropriate Save Calls
- [ ] **Search codebase for Save method calls**:
  - [ ] `SavePCConfigAsync`
  - [ ] `SavePhoneConfigAsync` 
  - [ ] `SaveApplicationConfigAsync`
- [ ] **Review each usage** to determine if it's appropriate (likely most are not)
- [ ] **Remove inappropriate calls** that save config during startup/runtime

## 🏗️ Task 2: Create TransformationEngineConfig

### Create New Model Class
- [ ] **Create `Models/TransformationEngineConfig.cs`**:
  ```csharp
  namespace SharpBridge.Models
  {
      /// <summary>
      /// Configuration for the transformation engine
      /// </summary>
      public class TransformationEngineConfig
      {
          /// <summary>
          /// Path to the transformation rules JSON file
          /// </summary>
          public string ConfigPath { get; set; } = "Configs/vts_transforms.json";
          
          /// <summary>
          /// Whether hot reload is enabled for transformation rules
          /// </summary>
          public bool EnableHotReload { get; set; } = true;
          
          /// <summary>
          /// Maximum number of evaluation iterations for parameter dependencies
          /// </summary>
          public int MaxEvaluationIterations { get; set; } = 10;
      }
  }
  ```

### Update TransformationEngine Interface
- [ ] **Modify `ITransformationEngine.cs`**:
  - [ ] Change `Task LoadRulesAsync(string filePath)` → `Task LoadRulesAsync()`
  - [ ] Update XML documentation to reflect parameterless method

### Update TransformationEngine Implementation  
- [ ] **Modify `Services/TransformationEngine.cs`**:
  - [ ] Add `TransformationEngineConfig` constructor parameter
  - [ ] Store config as private field: `private readonly TransformationEngineConfig _config;`
  - [ ] Update `LoadRulesAsync()` to be parameterless
  - [ ] Use `_config.ConfigPath` internally instead of parameter
  - [ ] Update all call sites within the class

## 🔄 Task 3: Update ApplicationOrchestrator Interface

### Remove Config Path Parameter
- [ ] **Modify `IApplicationOrchestrator.cs`**:
  - [ ] Change `Task InitializeAsync(string transformConfigPath, CancellationToken cancellationToken)` 
  - [ ] To: `Task InitializeAsync(CancellationToken cancellationToken)`

### Update ApplicationOrchestrator Implementation
- [ ] **Modify `Services/ApplicationOrchestrator.cs`**:
  - [ ] Remove `string transformConfigPath` parameter from `InitializeAsync`
  - [ ] Remove `_transformConfigPath` field (line 44)
  - [ ] Remove `ValidateInitializationParameters` method (no longer needed)
  - [ ] Update `InitializeTransformationEngine()` to be parameterless:
    ```csharp
    private async Task InitializeTransformationEngine()
    {
        await _transformationEngine.LoadRulesAsync(); // No path needed!
    }
    ```
  - [ ] Update `ReloadTransformationConfig()` method (around line 474):
    ```csharp
    await _transformationEngine.LoadRulesAsync(); // Remove path parameter
    ```
  - [ ] Update constructor to accept `TransformationEngineConfig` if needed for other operations

## 📞 Task 4: Update Program.cs

### Remove Command-Line Parsing
- [ ] **Identify command-line parsing code** in `Program.cs`
- [ ] **Remove command-line argument handling** for transformation config path
- [ ] **Update `ApplicationOrchestrator.InitializeAsync` call** to remove config path parameter
- [ ] **Test**: Verify application starts without command-line arguments

### Remove Unused Command-Line Classes (if they exist)
- [ ] **Search for CommandLineParser** references
- [ ] **Remove unused command-line parsing classes/utilities**
- [ ] **Clean up any related imports/dependencies**

## 🔧 Task 5: Update Service Registration

### Add TransformationEngineConfig Registration
- [ ] **Modify `ServiceRegistration.cs`**:
  - [ ] Add TransformationEngineConfig registration:
    ```csharp
    services.AddSingleton(provider =>
    {
        var configManager = provider.GetRequiredService<ConfigManager>();
        // For now, create default config - will be replaced in main phases
        return new TransformationEngineConfig();
    });
    ```

### Update TransformationEngine Registration
- [ ] **Update TransformationEngine registration** to include TransformationEngineConfig dependency
- [ ] **Verify dependency injection chain** works correctly

## 🧪 Task 6: Update Tests

### Find Tests Using Old Interface
- [ ] **Search for tests calling**:
  - [ ] `LoadRulesAsync(string filePath)`
  - [ ] `InitializeAsync(string transformConfigPath, ...)`
- [ ] **Update test method calls** to use new parameterless interfaces
- [ ] **Mock TransformationEngineConfig** in relevant tests
- [ ] **Update test assertions** as needed

### Remove Command-Line Tests
- [ ] **Find tests for command-line argument parsing**
- [ ] **Remove or update tests** that are no longer relevant
- [ ] **Verify all tests pass** after changes

## ✅ Verification Steps

### Build and Basic Functionality
- [ ] **Solution builds without errors**
- [ ] **All tests pass**
- [ ] **Application starts successfully**
- [ ] **Basic functionality works** (phone client connection, transformation engine loads)

### Hot Reload Testing
- [ ] **Test transformation config hot reload**:
  - [ ] Start application
  - [ ] Modify `vts_transforms.json`
  - [ ] Press hot reload shortcut (Alt+K)
  - [ ] Verify config reloads without errors
- [ ] **Verify parameterless LoadRulesAsync works correctly**

### Interface Consistency
- [ ] **Check all ITransformationEngine implementations** use new interface
- [ ] **Verify no lingering references** to old interfaces
- [ ] **Confirm dependency injection** resolves correctly

## 📝 Notes

### File Locations for Reference
- `ServiceRegistration.cs` - Lines 189-228 (Configure methods to remove)
- `Services/ApplicationOrchestrator.cs` - Line 44 (`_transformConfigPath` field to remove)
- `Services/ApplicationOrchestrator.cs` - Constructor and `InitializeAsync` method updates needed
- `Interfaces/ITransformationEngine.cs` - Method signature to update
- `Services/TransformationEngine.cs` - Implementation to update

### Testing Strategy
- Make incremental changes and test after each major step
- Keep a backup of working state before starting
- Test hot reload functionality thoroughly since it's a critical feature

### Risk Mitigation
- This is "pre-work" but involves significant interface changes
- Consider doing this work in a feature branch
- Document any unexpected issues encountered for main phases

## 🎯 Success Criteria

- [ ] **No command-line arguments required** to start application
- [ ] **TransformationEngine.LoadRulesAsync()** works parameterless
- [ ] **ApplicationOrchestrator.InitializeAsync()** takes no config path
- [ ] **All existing functionality preserved** (especially hot reload)
- [ ] **All tests pass**
- [ ] **Clean codebase** with removed inappropriate Save calls

---

**Next Step**: Once this checklist is complete, proceed to Phase 1 of the main configuration consolidation plan. 