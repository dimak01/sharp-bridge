# Configuration Validation & First-Time Setup - Refined Approach

## Overview

We've refined our approach to eliminate unnecessary abstractions while maintaining clean separation of concerns. Each DTO implements `IConfigSection` and has its own validator and first-time setup service, with the root validator orchestrating the overall process using a factory-based, type-driven approach.

## Core Principles

### 1. Clean DTOs for Consumers
- **No sentinel values** - DTOs should always be complete and usable
- **No nullable types** - DTOs represent complete configuration state
- **Required fields have no defaults** - `public string Host { get; set; }` (no default)
- **Optional fields keep defaults** - `public int Port { get; set; } = 8001`
- **Required attributes for clarity** - `[Required] public string Host { get; set; }`

### 2. Raw Data for Validation
- **Single data structure** - `ConfigFieldState` captures everything about a field
- **Used only for validation** - not for consumption
- **Enables detailed analysis** of what's missing/invalid
- **No mixing of concerns** - validation logic separate from consumption logic

### 3. Clear Separation of Responsibilities
- **ConfigManager**: Loads raw field data for each section (generic + non-generic methods)
- **IConfigSectionValidator<T>**: Validates fields for a specific section type
- **IConfigSectionFirstTimeSetupService<T>**: Fixes missing fields for a specific section
- **Factory Services**: Provide validators and setup services for any section type
- **ConfigRemediationService**: Coordinates the overall flow using type-driven iteration

## Refined Implementation Structure

```csharp
### Introduce enum instead of using barebone Type (match to corresponding class names because we can (could be useful????))

public enum ConfigSectionTypes{
    VTubeStudioPCConfig,
    VTubeStudioPhoneClientConfig,
    GeneralSettingsConfig,
    TransformationEngineConfig 
} 
```

### Core Interfaces
```csharp
public interface IConfigSection
{
    // Marker interface - no validation logic
}

public interface IConfigSectionValidator
{
    ConfigValidationResult ValidateSection(List<ConfigFieldState> fieldsState);
}

public interface IConfigSectionFirstTimeSetupService
{
    Task<(bool Success, IConfigSection? UpdatedConfig)> RunSetupAsync(
        List<ConfigFieldState> fieldsState);
}

// Factory interfaces for runtime type handling
public interface IConfigSectionValidatorsFactory
{
    IConfigSectionValidator GetValidator(ConfigSectionTypes sectionType);
}

public interface IConfigSectionFirstTimeSetupFactory
{
    IConfigSectionFirstTimeSetupService GetFirstTimeSetupService(ConfigSectionTypes sectionType);
}
```

### ConfigFieldState Record
```csharp
public record ConfigFieldState(
    string FieldName,           // e.g., "Host", "Port", "EditorCommand"
    object? Value,              // The actual value from the config file (null if missing)
    bool IsPresent,             // Was this field present in the JSON?
    Type ExpectedType,          // What type should this field be?
    string Description          // From [Description] attribute for user-friendly display
);
```

### ConfigManager Interface Updates
```csharp
public interface IConfigManager
{   
    Task<IConfigSection> LoadSectionAsync(ConfigSectionTypes sectionType);
    Task SaveSectionAsync(ConfigSectionTypes sectionType, IConfigSection config);
    Task<List<ConfigFieldState>> GetSectionFieldsAsync(ConfigSectionTypes sectionType);
}
```

### Type-Driven Flow
```csharp
// In ConfigRemediationService
var sectionConfigTypes = new[] { 
    typeof(VTubeStudioPCConfig), 
    typeof(VTubeStudioPhoneClientConfig), 
    typeof(GeneralSettingsConfig), 
    typeof(TransformationEngineConfig) 
};

var allSectionFields = new Dictionary<ConfigSectionTypes, List<ConfigFieldState>>();
var allUpdatedConfigs = new Dictionary<ConfigSectionTypes, IConfigSection>();

// Load field states for all sections
foreach (var sectionType in sectionConfigTypes)
{
    var sectionFields = await _configManager.GetSectionFieldsAsync(sectionType);
    allSectionFields[sectionType] = sectionFields;
}

// Validate and fix each section
foreach (var sectionType in allSectionFields.Keys)
{
    var fields = allSectionFields[sectionType];
    var validator = _validatorsFactory.GetValidator(sectionType);
    var validation = validator.ValidateSection(fields);
    
    if (!validation.IsValid)
    {
        var setupService = _firstTimeSetupFactory.GetFirstTimeSetupService(sectionType);
        var (success, updatedConfig) = await setupService.RunSetupAsync(fields);
        
        if (!success)
        {
            throw new InvalidOperationException($"Failed to setup {sectionType.Name} after retries");
        }
        
        allUpdatedConfigs[sectionType] = updatedConfig;
    }
}

// Save all updated sections
foreach (var sectionType in allUpdatedConfigs.Keys)
{
    var updatedConfig = allUpdatedConfigs[sectionType];
    await _configManager.SaveSectionAsync(sectionType, updatedConfig);
}
```

## Key Benefits

✅ **Clean DTOs** - consumers always get complete, usable objects
✅ **No sentinel values** - works for any data type (string, int, bool, etc.)
✅ **Clear separation** - validation logic separate from consumption logic
✅ **Type safety** - generic interfaces ensure correct pairing
✅ **Simple flow** - raw data → validate → fix → consume
✅ **Consumer-friendly** - DTOs work exactly as expected
✅ **No unnecessary abstractions** - eliminated IConfigSectionState and MissingField
✅ **Type-driven iteration** - no hardcoded section handling
✅ **Factory pattern** - clean separation of concerns
✅ **Generic + non-generic methods** - compile-time safety + runtime flexibility

## Implementation Details

### DTO Structure
```csharp
public class VTubeStudioPCConfig : IConfigSection
{
    [Required]
    [Description("Host Address")]
    public string Host { get; set; } = "localhost";  // Default but still required
    
    [Description("Port Number")]
    public int Port { get; set; } = 8001;  // Optional with default
}

public class VTubeStudioPhoneClientConfig : IConfigSection
{
    [Required]
    [Description("iPhone IP Address")]
    public string IphoneIpAddress { get; set; } = "127.0.0.1";  // Default but still required
    
    [Description("iPhone Port")]
    public int IphonePort { get; set; } = 21412;  // Optional with default
}
```

### Validator Implementation
```csharp
public class VTubeStudioPCConfigValidator : IConfigSectionValidator<VTubeStudioPCConfig>
{
    public ConfigValidationResult ValidateSection(List<ConfigFieldState> fieldsState)
    {
        var missingFields = fieldsState
            .Where(f => !f.IsPresent || f.Value == null)
            .Select(f => new MissingField(f.FieldName, f.ExpectedType, f.Description))
            .ToList();
            
        return new ConfigValidationResult(missingFields);
    }
}
```

### First-Time Setup Implementation
```csharp
public class VTubeStudioPCConfigFirstTimeSetup : IConfigSectionFirstTimeSetupService<VTubeStudioPCConfig>
{
    public async Task<(bool Success, VTubeStudioPCConfig? UpdatedConfig)> RunSetupAsync(
        List<ConfigFieldState> fieldsState)
    {
        var missingFields = fieldsState.Where(f => !f.IsPresent || f.Value == null);
        
        // Prompt user for each missing field
        // Return updated config with all required fields populated
    }
}
```

## Open Questions for Further Refinement

1. **Field discovery**: Should each validator hard-code its expected fields, or use reflection to discover them from the DTO type?
2. **Section serialization**: Keep single `ApplicationConfig.json` or split into separate section files?
3. **Error handling**: How should we handle malformed JSON vs. missing fields vs. invalid types?
4. **Validation granularity**: Should we validate each section independently first, then the whole config?
5. **Factory implementation**: Should we use a simple switch-based factory or a more sophisticated DI-based approach?

## Implementation Status

- [x] **Phase 1**: Create ConfigRemediationService ✅
- [x] **Phase 2**: Update service registration ✅
- [x] **Phase 3**: Remove first-time setup from ApplicationOrchestrator ✅
- [ ] **Phase 4**: Implement ConfigFieldState and generic interfaces (pending)
- [ ] **Phase 5**: Update validation logic (pending)
- [ ] **Phase 6**: Update first-time setup service (pending)
- [ ] **Phase 7**: Testing and integration (pending)

## Next Steps

1. **Create IConfigSection interface** and update all DTOs
2. **Implement ConfigFieldState record** 
3. **Create generic interfaces** for validators and first-time setup
4. **Create factory interfaces** for runtime type handling
5. **Update ConfigManager** with generic + non-generic methods
6. **Implement section-specific validators** and first-time setup services
7. **Test the complete flow** end-to-end
