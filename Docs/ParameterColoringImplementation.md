# Parameter Coloring Implementation Checklist

## Overview
This document tracks the implementation of color-coded parameter names and expressions in the console UI to visually connect the iPhone → PC transformation pipeline.

## Color Strategy
- **Blend Shapes**: Light Cyan (`\u001b[96m`) - iPhone source data
- **Calculated Parameters**: Light Yellow (`\u001b[93m`) - PC derived parameters
- **Color-blind friendly**: Cyan/Yellow combination works for all color vision types

## Implementation Phases

### Phase 1: Foundation & Integration Testing

#### Step 1.1: Define the Interface ⬜
- [ ] Create `Interfaces/IParameterColorService.cs`
- [ ] Define interface with methods:
  - [ ] `void InitializeFromConfiguration(Dictionary<string, string> expressions, IEnumerable<string> blendShapeNames)`
  - [ ] `string GetColoredExpression(string expression)`
  - [ ] `string GetColoredParameterName(string parameterName)`
- [ ] Add XML documentation for all methods

#### Step 1.2: Pass-Through Implementation ⬜
- [ ] Create `Services/ParameterColorService.cs`
- [ ] Implement IParameterColorService with pass-through behavior
- [ ] Add constructor with IAppLogger injection
- [ ] Log initialization calls for debugging
- [ ] All methods return input unchanged (for now)

#### Step 1.3: Integration Points ⬜

**Dependency Injection Setup:**
- [ ] Add `IParameterColorService` registration in DI container
- [ ] Register as singleton (shared state across app)

**PCTrackingInfoFormatter Integration:**
- [ ] Add `IParameterColorService` to constructor
- [ ] Update Parameter column: `param => _colorService.GetColoredParameterName(param.Id)`
- [ ] Update Expression column: `param => _colorService.GetColoredExpression(FormatExpression(param, trackingInfo))`
- [ ] Update unit tests for new constructor parameter

**PhoneTrackingInfoFormatter Integration:**
- [ ] Add `IParameterColorService` to constructor  
- [ ] Update Expression column: `shape => _colorService.GetColoredParameterName(shape.Key)`
- [ ] Update unit tests for new constructor parameter

**TransformationEngine Integration:**
- [ ] Add optional `IParameterColorService` dependency to constructor
- [ ] Call `InitializeFromConfiguration` in `LoadRulesAsync` after successful rule loading
- [ ] Extract blend shape names from current tracking data (aggressive caching)
- [ ] Pass expressions dictionary: `_rules.ToDictionary(r => r.Name, r => r.ExpressionString)`

#### Step 1.4: Verification & Testing ⬜
- [ ] Run application - should work exactly as before
- [ ] Verify all formatter constructors resolve successfully
- [ ] Verify `InitializeFromConfiguration` called during config load
- [ ] Console output should be identical (pass-through behavior)
- [ ] Run all existing unit tests - should pass
- [ ] Add basic integration test for color service registration

### Phase 2: Basic Color Implementation

#### Step 2.1: Color Constants & Assignment ⬜
- [ ] Add color constants to `ConsoleColors.cs`:
  - [ ] `BlendShapeColor = "\u001b[96m"` (Light Cyan)
  - [ ] `CalculatedParameterColor = "\u001b[93m"` (Light Yellow)
- [ ] Implement color assignment in `ParameterColorService`:
  - [ ] `_blendShapeColors` dictionary for blend shape → color mapping
  - [ ] `_parameterColors` dictionary for parameter → color mapping
  - [ ] `AssignColors()` method called from `InitializeFromConfiguration`

#### Step 2.2: Blend Shape Caching Strategy ⬜
- [ ] Add blend shape extraction logic to `TransformationEngine`
- [ ] Implement aggressive caching - extract once, reuse forever
- [ ] Add `ExtractBlendShapeNames()` method that:
  - [ ] Gets blend shape names from first successful transformation
  - [ ] Caches result in private field
  - [ ] Returns cached result on subsequent calls

#### Step 2.3: Basic Parameter Name Coloring ⬜
- [ ] Implement `GetColoredParameterName`:
  - [ ] Check if parameter is blend shape → return cyan colored
  - [ ] Check if parameter is calculated parameter → return yellow colored  
  - [ ] Fallback to uncolored if not found
- [ ] Add unit tests for parameter name coloring

#### Step 2.4: Basic Expression Coloring ⬜
- [ ] Implement `GetColoredExpression` with simple token replacement:
  - [ ] Use regex to find parameter names in expressions
  - [ ] Replace each found parameter with its colored version
  - [ ] Cache colored expressions in `_coloredExpressionCache`
- [ ] Add unit tests for expression coloring

#### Step 2.5: Testing & Verification ⬜
- [ ] Run application with sample configuration
- [ ] Verify blend shapes appear in light cyan
- [ ] Verify calculated parameters appear in light yellow
- [ ] Verify expressions show colored parameter references
- [ ] Test with various expression complexities
- [ ] Run full test suite

### Phase 3: Advanced Features (Future)

#### Step 3.1: Enhanced Expression Parsing ⬜
- [ ] Implement more sophisticated regex patterns
- [ ] Handle edge cases (parentheses, operators, functions)
- [ ] Add support for nested parameter references

#### Step 3.2: Dependency-Aware Coloring ⬜
- [ ] Analyze parameter dependency chains
- [ ] Implement related color schemes for dependent parameters
- [ ] Add visual indicators for parameter complexity

#### Step 3.3: Performance Optimization ⬜
- [ ] Benchmark coloring performance with large configurations
- [ ] Optimize regex patterns if needed
- [ ] Add performance metrics to service stats

## Testing Strategy

### Unit Tests Required
- [ ] `ParameterColorService` basic functionality
- [ ] Color assignment logic
- [ ] Expression parsing and coloring
- [ ] Parameter name coloring
- [ ] Cache behavior verification
- [ ] Error handling and graceful degradation

### Integration Tests Required  
- [ ] End-to-end color display in formatters
- [ ] Configuration loading with color service
- [ ] DI container resolution
- [ ] Multi-formatter consistency

### Manual Testing Scenarios
- [ ] Basic iPhone → PC parameter transformation
- [ ] Complex expressions with multiple parameters
- [ ] Configuration reload scenarios
- [ ] Console output readability in different terminals

## Success Criteria

### Phase 1 Success
- [x] Application runs without errors
- [x] All existing functionality preserved  
- [x] Integration points established
- [x] Foundation ready for color implementation

### Phase 2 Success
- [ ] Blend shapes display in light cyan
- [ ] Calculated parameters display in light yellow
- [ ] Expressions show colored parameter references
- [ ] Performance impact negligible
- [ ] Color-blind accessibility validated

### Phase 3 Success
- [ ] Advanced parsing handles complex expressions
- [ ] Dependency relationships visually clear
- [ ] Performance optimized for large configurations

## Notes & Decisions

### Technical Decisions
- **Caching Strategy**: Aggressive caching of blend shape names and colored expressions
- **Color Palette**: Cyan/Yellow for accessibility 
- **Error Handling**: Graceful degradation to uncolored output
- **Integration**: Optional dependency injection to avoid breaking existing code

### Future Considerations
- Could extend to support user-configurable color schemes
- Could add visual indicators for parameter value ranges
- Could implement parameter grouping/categorization
- Could add color legend display in console UI

---

## Progress Tracking
- **Current Phase**: Phase 1 - Foundation & Integration Testing
- **Last Updated**: [Update when working on this]
- **Estimated Completion**: [Update with timeline] 