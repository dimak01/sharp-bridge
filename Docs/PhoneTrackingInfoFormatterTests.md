# PhoneTrackingInfoFormatter Testing Checklist

## Overview
This document outlines the testing strategy for `PhoneTrackingInfoFormatter`, a component responsible for formatting `PhoneTrackingInfo` objects into human-readable strings for console display. This formatter handles iPhone tracking data including head rotation, position, and blend shapes.

## Test Categories

### 1. Core Interface Tests
- [ ] Test `IFormatter` interface implementation
- [ ] Verify `Format` method with null input
- [ ] Verify `Format` method with invalid type input

### 2. Verbosity Level Tests
- [ ] Test initial verbosity level (should be Normal)
- [ ] Test `CycleVerbosity` method through all levels
- [ ] Verify behavior at each verbosity level

### 3. Header Formatting Tests
- [ ] Test header shows "=== iPhone Tracking Data === [Alt+O]"
- [ ] Test header formatting with face detected
- [ ] Test header formatting without face detected
- [ ] Test header with different blend shape counts
- [ ] Test header with null blend shapes

### 4. Head Rotation Tests
- [ ] Test rotation display at Normal verbosity
- [ ] Test rotation display at Basic verbosity (should be hidden)
- [ ] Test null rotation handling
- [ ] Test degree formatting (X,Y,Z with 1 decimal place)
- [ ] Test extreme rotation values
- [ ] Test zero rotation values

### 5. Head Position Tests
- [ ] Test position display at Normal verbosity
- [ ] Test position display at Basic verbosity (should be hidden)
- [ ] Test null position handling
- [ ] Test coordinate formatting (X,Y,Z with 1 decimal place)
- [ ] Test extreme position values
- [ ] Test zero position values

### 6. Blend Shapes Tests
- [ ] Test blend shapes display at Normal verbosity (10 shapes)
- [ ] Test blend shapes display at Detailed verbosity (all shapes)
- [ ] Test blend shapes display at Basic verbosity (should be hidden)
- [ ] Test with empty blend shapes list
- [ ] Test with null blend shapes
- [ ] Test progress bar creation for different values
- [ ] Test key expressions display
- [ ] Test total blend shapes count display

### 7. Progress Bar Tests
- [ ] Test progress bar with value 0.0 (empty bar)
- [ ] Test progress bar with value 1.0 (full bar)
- [ ] Test progress bar with value 0.5 (half bar)
- [ ] Test progress bar with extreme values (< 0.0 or > 1.0)
- [ ] Verify consistent bar length (20 characters)
- [ ] Verify correct fill/empty characters (█/░)

### 8. Formatting Consistency Tests
- [ ] Verify consistent spacing and alignment
- [ ] Verify proper line breaks
- [ ] Verify proper indentation
- [ ] Test alignment with different blend shape key lengths
- [ ] Test numeric value formatting consistency

## Test Implementation Notes

### Test Data Setup
- Create mock `PhoneTrackingInfo` objects with various configurations
- Prepare blend shapes with different combinations of values
- Set up test cases for each verbosity level
- Create test data for rotation and position combinations

### Assertion Strategy
- Verify exact string output matches expected format
- Check proper handling of null values and edge cases
- Validate numeric formatting and alignment
- Confirm progress bar visualization accuracy
- Verify correct data visibility at different verbosity levels

### Test Organization
- Group tests by component (rotation, position, blend shapes)
- Use descriptive test names
- Include comments explaining complex test cases
- Maintain consistent test structure

### Coverage Goals
- Achieve 100% coverage of public methods
- Cover all edge cases and error conditions
- Verify proper handling of null inputs
- Test all verbosity level combinations
- Ensure proper handling of all tracking data components 