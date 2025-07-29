# Interpolation Customization Feature

## Overview

The Interpolation Customization Feature enhances Sharp Bridge's parameter transformation system by allowing users to define custom interpolation methods for how parameter values are calculated from tracking data. This replaces the current linear interpolation with flexible, user-defined curves that provide more natural and artistic control over parameter behavior.

## Problem Statement

Currently, Sharp Bridge uses linear interpolation for all parameter transformations:
```
Raw Expression Value → Linear Interpolation → Final Parameter Value
```

This limitation prevents users from achieving:
- **Non-linear responses** - Parameters that respond more naturally to tracking data
- **Fine-tuned control** - Smooth acceleration/deceleration curves
- **Creative expression** - Artistic control over parameter behavior
- **Performance optimization** - Curves that can be pre-calculated for efficiency

## Solution Architecture

### Core Concept

The feature introduces a **polymorphic interpolation system** that operates on normalized input (0-1) and produces normalized output (0-1), which is then scaled to the actual parameter range.

```
Raw Expression Value → Normalize (0-1) → Interpolation Method → Denormalize → Final Parameter Value
```

### Key Design Decisions

#### 1. Normalized Interpolation Space
- **All interpolation methods operate on 0-1 normalized space**
- **Input normalization**: Based on expected expression range (`Min`/`Max`)
- **Output scaling**: To actual parameter bounds for VTube Studio
- **Benefits**: Reusable curves, clean separation of concerns, backward compatibility

#### 2. Polymorphic Interpolation Methods
- **Interface-based design**: `IInterpolationMethod` with `Interpolate(double t)` method
- **Extensible**: Easy to add new interpolation types (Linear, Bezier, Exponential, etc.)
- **Type-safe**: Strongly typed implementations with compile-time safety
- **Serializable**: JSON configuration support with custom converter

#### 3. Clean Data Models
- **Pure DTOs**: No behavior in data models, only data
- **Custom JSON converter**: Handles type serialization/deserialization automatically
- **No Type properties**: DTOs don't know about serialization concerns
- **Backward compatibility**: Existing configs continue to work unchanged

#### 4. Backward Compatibility Strategy
- **Linear as default**: When no interpolation specified, use linear interpolation
- **Existing configs work**: No changes required for current configurations
- **Graceful degradation**: Fallback to linear if interpolation fails

## Interpolation Methods

### Linear Interpolation
- **Behavior**: `y = x` (straight line from 0,0 to 1,1)
- **Use case**: Default behavior, simple transformations
- **Configuration**: No additional parameters needed

### Bezier Interpolation
- **Behavior**: Bezier curve with variable control points (2-8 points)
- **Use case**: Smooth, natural parameter responses
- **Configuration**: Array of control points (normalized 0-1 coordinates)
- **Flexibility**: Supports 2-8 control points (linear = 2 points, quadratic = 3 points, cubic = 4 points, higher-order = 5+ points)

### Future Interpolation Methods
- **Exponential**: `y = x^exponent` for acceleration/deceleration effects
- **Logarithmic**: `y = log(x)` for inverse exponential behavior
- **Step**: Threshold-based discrete values
- **Custom**: User-defined mathematical functions

## Technical Implementation

### Data Model Structure

```csharp
public interface IInterpolationDefinition { }

public class LinearInterpolation : IInterpolationDefinition { }

public class BezierInterpolation : IInterpolationDefinition
{
    public List<Point> ControlPoints { get; set; } = new();
}

public class ParameterTransformation
{
    public string Name { get; set; }
    public string ExpressionString { get; set; }
    public double Min { get; set; }  // Input range
    public double Max { get; set; }  // Input range
    public IInterpolationDefinition? Interpolation { get; set; }
}
```

### JSON Serialization Strategy

**Custom Converter Approach:**
- **Type detection**: Converter automatically adds/removes `Type` property
- **Assembly-scoped**: Only looks in SharpBridge assembly for types
- **Automatic discovery**: No converter changes needed for new interpolation types
- **Clean JSON**: `"Type": "BezierInterpolation"` instead of full assembly info

**Example JSON:**
```json
{
  "Name": "EyeBlink",
  "ExpressionString": "EyeLeftY + EyeRightY",
  "Min": -100.0,
  "Max": 100.0,
  "Interpolation": {
    "Type": "BezierInterpolation",
    "ControlPoints": [
      {"X": 0.0, "Y": 0.0},
      {"X": 0.3, "Y": 0.1},
      {"X": 0.7, "Y": 0.9},
      {"X": 1.0, "Y": 1.0}
    ]
  }
}
```

### Transformation Engine Integration

**Modified Flow:**
```csharp
private static double GetRuleValue(ParameterTransformation rule)
{
    var evaluatedValue = Convert.ToDouble(rule.Expression.Evaluate());
    
    // NEW: Interpolation step
    if (rule.Interpolation != null)
    {
        var normalizedInput = NormalizeToRange(evaluatedValue, rule.Min, rule.Max);
        var interpolatedValue = rule.InterpolationMethod.Interpolate(normalizedInput);
        return ScaleToRange(interpolatedValue, rule.OutputMin, rule.OutputMax);
    }
    
    // EXISTING: Linear interpolation (backward compatibility)
    return Math.Clamp(evaluatedValue, rule.OutputMin, rule.OutputMax);
}
```

## Benefits

### For Users
- **Natural parameter responses** - Curves that match human perception
- **Fine-tuned control** - Precise control over parameter behavior
- **Creative expression** - Artistic control over transformation curves
- **Performance optimization** - Pre-calculated curves for efficiency

### For Developers
- **Extensible architecture** - Easy to add new interpolation methods
- **Type-safe implementation** - Compile-time safety for all interpolation types
- **Clean separation** - Data models separate from behavior
- **Backward compatibility** - Existing configurations continue to work

### For System
- **Maintainable code** - Clear interfaces and responsibilities
- **Testable components** - Each interpolation method can be tested independently
- **Future-proof design** - Easy to extend with new interpolation types
- **Performance optimized** - Efficient curve evaluation for real-time use

## Configuration Examples

### Linear Interpolation (Default)
```json
{
  "Name": "SimpleParam",
  "ExpressionString": "HeadPosX",
  "Min": -1.0,
  "Max": 1.0
}
```

### Bezier Interpolation (Ease-in-out)
```json
{
  "Name": "SmoothParam",
  "ExpressionString": "EyeLeftX",
  "Min": -50.0,
  "Max": 50.0,
  "Interpolation": {
    "Type": "BezierInterpolation",
    "ControlPoints": [
      {"X": 0.0, "Y": 0.0},
      {"X": 0.3, "Y": 0.1},
      {"X": 0.7, "Y": 0.9},
      {"X": 1.0, "Y": 1.0}
    ]
  }
}
```

### Bezier Interpolation (Ease-in)
```json
{
  "Name": "AcceleratingParam",
  "ExpressionString": "HeadRotY",
  "Min": -90.0,
  "Max": 90.0,
  "Interpolation": {
    "Type": "BezierInterpolation",
    "ControlPoints": [
      {"X": 0.0, "Y": 0.0},
      {"X": 0.7, "Y": 0.3},
      {"X": 0.9, "Y": 0.7},
      {"X": 1.0, "Y": 1.0}
    ]
  }
}
```

## Validation Rules

### Bezier Interpolation
- **Minimum control points**: 2 (linear curve)
- **Recommended control points**: 4 (cubic curve)
- **Maximum control points**: 8 (for performance reasons)
- **Coordinate range**: All X and Y values must be 0-1 (normalized)
- **Start point**: First control point must be (0,0)
- **End point**: Last control point must be (1,1)
- **Curve types**: 2 points = Linear, 3 points = Quadratic, 4 points = Cubic, 5+ points = Higher-order

### General Rules
- **Input range**: `Min` and `Max` define expected expression output range
- **Output range**: VTube Studio parameter bounds (handled by transformation engine)
- **Backward compatibility**: Missing interpolation = linear interpolation
- **Error handling**: Graceful fallback to linear if interpolation fails

## Future Enhancements

### Additional Interpolation Methods
- **Exponential curves** for acceleration/deceleration effects
- **Logarithmic curves** for inverse exponential behavior
- **Step functions** for discrete parameter values
- **Custom mathematical functions** for advanced users

### UI Enhancements
- **Curve visualization** in configuration UI
- **Curve library** with pre-built common curves
- **Interactive curve editor** for visual curve creation
- **Real-time curve preview** during configuration

### Performance Optimizations
- **Curve pre-calculation** for frequently used curves
- **Caching mechanisms** for repeated evaluations
- **Optimized algorithms** for specific curve types
- **Parallel processing** for multiple parameter evaluations 