# Parameter Evaluation Strategy

## Overview

This document describes the multi-pass approach for evaluating transformation rules in the TransformationEngine, particularly focusing on handling dependencies between custom parameters.

## Current Limitation

In the current implementation, transformation rules can only reference tracking data parameters (head position, rotation, eye positions, blend shapes). They cannot reference other custom parameters defined in the rules themselves.

## Proposed Solution: Multi-pass Evaluation

The solution implements an iterative approach to parameter evaluation:

1. **First Pass**
   - Set all tracking data parameters (blend shapes, head position, etc.)
   - Attempt to evaluate each rule
   - Track successfully evaluated rules and their values
   - Keep track of rules that couldn't be evaluated

2. **Subsequent Passes**
   - For remaining unevaluated rules:
     - Set all tracking data parameters
     - Set all previously evaluated custom parameter values
     - Attempt evaluation
     - Track newly evaluated rules
   - Continue until either:
     - All rules are evaluated
     - No new rules can be evaluated
     - Maximum iteration limit is reached

## Implementation Details

```csharp
// Track evaluated values and remaining rules
var evaluatedValues = new Dictionary<string, double>();
var remainingRules = new List<(string Name, Expression Expression, string ExpressionString, double Min, double Max, double DefaultValue)>();

// First pass - try with tracking data only
foreach (var rule in _rules)
{
    try
    {
        SetParametersFromTrackingData(rule.Expression, trackingData);
        var value = Convert.ToDouble(rule.Expression.Evaluate());
        value = Math.Clamp(value, rule.Min, rule.Max);
        evaluatedValues[rule.Name] = value;
    }
    catch
    {
        remainingRules.Add(rule);
    }
}

// Subsequent passes - try with both tracking data and evaluated values
const int maxIterations = 5;
for (int iteration = 0; iteration < maxIterations && remainingRules.Count > 0; iteration++)
{
    var stillRemaining = new List<(string Name, Expression Expression, string ExpressionString, double Min, double Max, double DefaultValue)>();
    
    foreach (var rule in remainingRules)
    {
        try
        {
            SetParametersFromTrackingData(rule.Expression, trackingData);
            // Add all previously evaluated values
            foreach (var (paramName, paramValue) in evaluatedValues)
            {
                rule.Expression.Parameters[paramName] = paramValue;
            }
            
            var value = Convert.ToDouble(rule.Expression.Evaluate());
            value = Math.Clamp(value, rule.Min, rule.Max);
            evaluatedValues[rule.Name] = value;
        }
        catch
        {
            stillRemaining.Add(rule);
        }
    }
    
    // If no new values were evaluated in this iteration, we can stop
    if (stillRemaining.Count == remainingRules.Count)
    {
        break;
    }
    
    remainingRules = stillRemaining;
}
```

## Advantages

1. **Simplicity**: Easy to understand and implement
2. **Graceful Handling**: Handles circular dependencies by limiting iterations
3. **Efficiency**: Only attempts to evaluate expressions that failed in previous passes
4. **Robustness**: Provides good logging and error handling capabilities

## Performance Considerations

The performance impact should be minimal because:
1. Most expressions will likely be evaluated in the first pass
2. The number of parameters is typically small
3. The iteration limit prevents any potential infinite loops

## Alternative Approaches

While not implemented, these alternatives were considered:

1. **Dependency Graph**
   - Build a directed graph of parameter dependencies
   - Use topological sorting to determine evaluation order
   - More complex but potentially more efficient
   - Would fail fast on circular dependencies

2. **NCalc Custom Functions**
   - Use NCalc's custom function support
   - Create a custom function that looks up parameter values
   - More "native" to NCalc but might be overkill

3. **Expression Preprocessing**
   - Parse expressions to build dependency trees
   - Validate dependencies before evaluation
   - Catches circular dependencies early but adds complexity

## Future Considerations

1. **Circular Dependency Detection**: Could add explicit detection of circular dependencies
2. **Evaluation Order Optimization**: Could implement topological sorting for more efficient evaluation
3. **Custom Function Support**: Could add support for custom functions in expressions
4. **Validation Improvements**: Could add more robust validation of parameter references 