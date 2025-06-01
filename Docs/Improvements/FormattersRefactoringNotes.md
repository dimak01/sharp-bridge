# Test Strategy for Table Formatting Refactoring

## Overview

This document outlines the test strategy following the major refactoring of table formatting logic, where we decomposed PC/Phone formatters from TableFormatter implementation. The beauty of this refactoring is that we've separated concerns: formatters handle business logic and data preparation, while TableFormatter handles rendering.

## Architecture Context

### What We Refactored
- **Before**: PC/Phone formatters contained complex table rendering logic
- **After**: PC/Phone formatters focus on business logic, delegate table rendering to `ITableFormatter`
- **Key Principle**: Each component has a single, well-defined responsibility

### Current Responsibilities

#### PC/Phone Formatters (PCTrackingInfoFormatter, PhoneTrackingInfoFormatter)
- Service header formatting (status, verbosity, health)
- Business logic (what to show based on verbosity, face detection)
- Data transformation (normalized values, expression formatting)
- Metrics calculation and formatting
- TableFormatter integration (calling `AppendTable` with correct parameters)

#### TableFormatter
- Table layout and rendering
- Column sizing and distribution
- Progress bar rendering
- Multi-column layout
- "More items" logic and display limits
- Width calculation and space distribution

## Test Strategy

### 1. Mock TableFormatter Approach

**Rationale**: We want to test formatter business logic without coupling to table rendering implementation.

**Implementation**:
- Use mocked `ITableFormatter` in PC/Phone formatter tests
- Verify correct calls to `AppendTable` with proper parameters
- Test business logic and data transformation independently

### 2. Test Scope Definition

#### What PC/Phone Formatter Tests SHOULD Test:
- ✅ Service header generation (exact wording, status colors)
- ✅ Health status formatting with proper timing
- ✅ Metrics calculation and display formatting
- ✅ Data filtering based on verbosity levels
- ✅ Data transformation (normalized values, expressions)
- ✅ Business rules (when to show/hide sections)
- ✅ TableFormatter integration (verify method calls)
- ✅ Face detection logic (Phone formatter)
- ✅ Parameter/BlendShape data preparation

#### What PC/Phone Formatter Tests Should NOT Test:
- ❌ Table rendering (column widths, alignment)
- ❌ Progress bar visual representation
- ❌ Multi-column layout logic
- ❌ "More items" message generation
- ❌ Table width calculation and distribution
- ❌ Specific table formatting details

### 3. Test Structure

#### PC Formatter Test Categories:
1. **Service Header Tests**
   - Status display with correct colors
   - Verbosity level display
   - Health status formatting with timing
   - Connection metrics formatting

2. **Parameter Data Preparation Tests**
   - Correct parameter filtering based on verbosity
   - Parameter sorting (by ID)
   - Data passed to TableFormatter.AppendTable()

3. **Data Transformation Tests**
   - Normalized value calculation
   - Expression formatting and truncation
   - Range formatting (weight x [min; default; max])
   - Fallback handling for missing definitions

4. **Business Logic Tests**
   - Verbosity-based display decisions
   - Face detection status display
   - Parameter count display

5. **TableFormatter Integration Tests**
   - Verify AppendTable called with correct parameters
   - Verify column definitions are correct
   - Verify display limits based on verbosity

#### Phone Formatter Test Categories:
1. **Service Header Tests**
   - Status display with correct colors
   - Verbosity level display
   - Health status formatting with consistent padding
   - Metrics formatting (frames, failed, FPS)

2. **BlendShape Data Preparation Tests**
   - Correct BlendShape filtering and sorting
   - Data passed to TableFormatter.AppendTable()
   - Verbosity-based display limits

3. **Face Detection Logic Tests**
   - When to show/hide tracking data sections
   - Face status display with correct icons
   - Head rotation/position formatting

4. **Metrics Calculation Tests**
   - Frame count formatting with padding
   - FPS calculation and display
   - Failed frame count formatting

5. **TableFormatter Integration Tests**
   - Verify AppendTable called with correct parameters
   - Verify column definitions for BlendShapes
   - Verify display limits based on verbosity

### 4. Mock Setup Strategy

#### TableFormatter Mock Configuration:
```csharp
// Verify method calls without testing rendering
_mockTableFormatter.Setup(tf => tf.AppendTable(
    It.IsAny<StringBuilder>(),
    It.IsAny<string>(),
    It.IsAny<IEnumerable<T>>(),
    It.IsAny<IList<ITableColumn<T>>>(),
    It.IsAny<int>(),
    It.IsAny<int>(),
    It.IsAny<int>(),
    It.IsAny<int?>()))
.Callback<StringBuilder, string, IEnumerable<T>, IList<ITableColumn<T>>, int, int, int, int?>(
    (builder, title, rows, columns, targetCols, consoleWidth, barWidth, maxItems) =>
    {
        // Verify parameters are correct
        // Optionally append minimal content for integration testing
    });
```

#### Console Mock Configuration:
```csharp
_mockConsole.Setup(c => c.WindowWidth).Returns(80);
_mockConsole.Setup(c => c.WindowHeight).Returns(25);
```

### 5. Test Data Strategy

#### Consistent Test Data:
- Use realistic parameter definitions with proper ranges
- Include edge cases (null values, extreme values)
- Test both healthy and unhealthy service states
- Include various verbosity levels

#### Parameter Test Data Example:
```csharp
var parameterDefinitions = new Dictionary<string, ParameterDefinition>
{
    ["eyeBlinkLeft"] = new ParameterDefinition { Min = 0, Max = 1, DefaultValue = 0 },
    ["jawOpen"] = new ParameterDefinition { Min = 0, Max = 1, DefaultValue = 0 }
};

var parameters = new List<TrackingParam>
{
    new TrackingParam { Id = "eyeBlinkLeft", Value = 0.75f, Weight = 1.0f },
    new TrackingParam { Id = "jawOpen", Value = 0.25f, Weight = 0.8f }
};
```

### 6. Future Test Plans

#### TableFormatter Tests (Separate Suite):
- Comprehensive table rendering tests
- Column sizing and distribution logic
- Progress bar rendering accuracy
- Multi-column layout correctness
- Width calculation algorithms
- "More items" message generation
- Edge cases (very narrow/wide consoles)

#### Integration Tests:
- End-to-end tests with real TableFormatter
- Visual output verification for critical scenarios
- Performance tests for large datasets

## Implementation Guidelines

### Test Naming Convention:
- `Format_WithCondition_ExpectedBehavior`
- `AppendParameters_WithDetailedVerbosity_CallsTableFormatterCorrectly`
- `FormatServiceHeader_WithUnhealthyStatus_ShowsErrorColor`

### Assertion Strategy:
- Use FluentAssertions for readable test assertions
- Verify exact string content for formatter-generated text
- Use Moq.Verify for TableFormatter integration testing
- Test both positive and negative cases

### Test Organization:
- Group related tests in nested classes or regions
- Use descriptive test method names
- Include setup helpers for common test data
- Separate unit tests from integration tests

## Benefits of This Approach

1. **Clear Separation of Concerns**: Tests focus on what each component actually does
2. **Maintainable**: Changes to table rendering don't break formatter tests
3. **Fast Execution**: Mocked dependencies make tests run quickly
4. **Focused Assertions**: Each test verifies specific business logic
5. **Future-Proof**: Easy to add new formatters or modify existing ones

## Success Criteria

- ✅ All formatter business logic is thoroughly tested
- ✅ TableFormatter integration is verified without coupling
- ✅ Tests are fast, reliable, and maintainable
- ✅ Clear distinction between unit and integration tests
- ✅ Easy to add tests for new features or formatters 