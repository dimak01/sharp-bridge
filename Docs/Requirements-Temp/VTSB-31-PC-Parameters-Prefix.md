# VTSB-31: PC Parameters Prefix Feature

## Problem Statement

VTube Studio requires parameter uniqueness across all plugins, not just within a single plugin. To avoid naming conflicts with other plugins and ensure our parameters are always present in VTube Studio PC, we need to add a configurable prefix to parameter names.

## Requirements

### Functional Requirements

1. **Parameter Prefix Configuration**
   - Add configurable prefix to `VTubeStudioPCConfig`
   - Default prefix: `"SB_"` (to avoid name conflicts with parameters from other plugins)
   - Allow empty prefix (0 characters)
   - Maximum prefix length: 15 characters
   - Prefix must be alphanumeric only, no spaces

2. **Parameter Name Transformation**
   - Apply prefix only at the "last mile" - just before sending to VTube Studio PC
   - Keep internal naming clean and unchanged
   - Console display and TransformationEngine remain unaware of prefixes
   - Apply prefix in both parameter synchronization and data sending

3. **VTube Studio API Compliance**
   - Final parameter names must meet VTS requirements:
     - 4-32 characters total length
     - Alphanumeric only, no spaces
     - Unique across all plugins

### Non-Functional Requirements

1. **Backward Compatibility**
   - No migration strategy needed (pre-release)
   - Users can manually delete existing parameters if desired

2. **Error Handling**
   - Keep current behavior for parameter conflicts/rejections
   - Future enhancement: parse VTS responses for better error handling

3. **Performance**
   - Minimal performance impact
   - Prefix application should be efficient

## Implementation Plan

### Phase 1: Configuration
1. **Add ParameterPrefix to VTubeStudioPCConfig**
   - Property: `string ParameterPrefix { get; set; } = "SB_"`
   - Add XML documentation
   - Add Description attribute for configuration UI

2. **Add Configuration Validation**
   - Update `VTubeStudioPCConfigRemediationService`
   - Validate prefix length (0-15 characters)
   - Validate prefix format (alphanumeric only, no spaces)
   - Future: validate combined length (prefix + parameter name ≤ 32)

### Phase 2: Parameter Synchronization
3. **Update VTubeStudioPCParameterManager**
   - Modify `TrySynchronizeParametersAsync` to apply prefix to parameter names
   - Apply prefix when creating/updating parameters in VTube Studio
   - Keep original parameter names for internal tracking

### Phase 3: Data Sending
4. **Update VTubeStudioPCClient**
   - Modify `SendTrackingAsync` to apply prefix to parameter IDs
   - Apply prefix to `TrackingParam.Id` values before sending
   - Maintain original parameter names in internal data structures

### Phase 4: Testing
5. **Add Comprehensive Tests**
   - Test prefix configuration validation
   - Test parameter synchronization with prefixes
   - Test data sending with prefixes
   - Test edge cases (empty prefix, maximum length, invalid characters)
   - Test backward compatibility scenarios

### Phase 5: Documentation
6. **Update Documentation**
   - Update README.md with prefix configuration
   - Update configuration examples
   - Add troubleshooting section for prefix-related issues

## Technical Design

### Configuration Structure
```json
{
  "PCClient": {
    "Host": "localhost",
    "Port": 8001,
    "UsePortDiscovery": true,
    "ParameterPrefix": "SB_"
  }
}
```

### Parameter Name Flow
```
Transformation Rules (vts_transforms.json)
    ↓ (original names: "FaceAngleY", "MouthOpen")
TransformationEngine
    ↓ (unchanged names)
PCTrackingInfo
    ↓ (unchanged names)
VTubeStudioPCParameterManager (apply prefix)
    ↓ (prefixed names: "SB_FaceAngleY", "SB_MouthOpen")
VTube Studio PC
```

### Key Components Modified
- `Models/VTubeStudioPCConfig.cs` - Add ParameterPrefix property
- `Services/Remediation/VTubeStudioPCConfigRemediationService.cs` - Add validation
- `Utilities/VTubeStudioPCParameterManager.cs` - Apply prefix in synchronization
- `Services/VTubeStudioPCClient.cs` - Apply prefix in data sending

## Testing Strategy

### Unit Tests
- Configuration validation tests
- Prefix application logic tests
- Edge case handling tests

### Integration Tests
- End-to-end parameter flow with prefixes
- VTube Studio parameter synchronization with prefixes
- Data sending with prefixed parameter names

### Manual Testing
- Verify parameters appear with correct prefixes in VTube Studio
- Verify parameters are grouped at top of list (with "SB_" prefix)
- Test with empty prefix (no prefix applied)
- Test with maximum length prefix

## Future Enhancements

1. **Enhanced Validation**
   - Validate combined prefix + parameter name length
   - Check for potential conflicts with existing VTS parameters

2. **Better Error Handling**
   - Parse VTS API responses for parameter creation failures
   - Display meaningful error messages for prefix-related issues

3. **Migration Tools**
   - Tool to rename existing parameters with new prefix
   - Bulk parameter management utilities

## Acceptance Criteria

- [ ] ParameterPrefix property added to VTubeStudioPCConfig with default "SB_"
- [ ] Configuration validation prevents invalid prefixes (length, format)
- [ ] Parameters are created in VTube Studio with correct prefixes
- [ ] Parameter data is sent to VTube Studio with prefixed names
- [ ] Console display shows original parameter names (unprefixed)
- [ ] Empty prefix works correctly (no prefix applied)
- [ ] Maximum length prefix (15 chars) works correctly
- [ ] All existing functionality remains unchanged
- [ ] Comprehensive test coverage for all scenarios
- [ ] Documentation updated with prefix feature

## Risks and Mitigation

### Risk: Parameter Name Conflicts
- **Mitigation**: Use distinctive default prefix ("SB_") and validate prefix format

### Risk: VTS API Rejection
- **Mitigation**: Follow VTS naming requirements strictly, keep current error handling

### Risk: Performance Impact
- **Mitigation**: Apply prefix only at last mile, minimal string operations

### Risk: User Confusion
- **Mitigation**: Clear documentation, default prefix that groups parameters visibly
