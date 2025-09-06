# Firewall Rules Table Formatter Implementation Plan

## Overview
This document outlines the plan to convert the current list-based firewall rules display to a table-based format using the existing `ITableFormatter` infrastructure.

## Current State
The firewall rules are currently displayed as a simple list:
```
- Matching rules (4 found):
    [Enabled] Allow SharpBridge iPhone UDP Inbound UDP 28964  (Any → ThisDevice)  Global
    [Disabled] Allow SharpBridge UDP (Any → ThisDevice)  App: D:\streams\vtube\sharp-bridge\bin\debug\net8.0\sharpbridge.exe
    [Disabled] Allow SharpBridge iPhone UDP Inbound UDP 28964  (Any → ThisDevice)  Global
    [Disabled] Allow SharpBridge UDP (Any → ThisDevice)  App: D:\streams\vtube\sharp-bridge\bin\debug\net8.0\sharpbridge.exe
```

## Proposed Table Structure

### Columns
1. **Status** - `[Enabled]` or `[Disabled]` (with coloring)
2. **Action** - `Allow` or `Deny` (with coloring)
3. **Rule Name** - The firewall rule name
4. **Protocol** - `UDP`, `TCP`, etc.
5. **Port** - Port number or `*`/`any` for wildcards
6. **Direction** - `(Any → ThisDevice)` format (keep current arrow format)
7. **Scope** - `Global` or application path (truncated if too long)

### Table Configuration
- **Target Column Count**: 1 (to maintain current single-column appearance)
- **Console Width**: Use the current console width
- **Title**: Include the "Matching rules (X found):" text
- **Indentation**: Apply the indent prefix to the entire table
- **Rule Limit**: Show top 10 rules (increased from 5)
- **Headers**: Display table headers for clarity

## Implementation Steps

### 1. Extend ITableFormatter Interface
- Add an optional `indent` parameter to `AppendTable` method with default value of 0
- This maintains backward compatibility with existing table formatter clients

### 2. Update TableFormatter Implementation
- Modify the `AppendTable` method to accept the new `indent` parameter
- Apply the indent to each line of the table output (title, headers, rows, separators)
- Ensure proper spacing and alignment with the indent

### 3. Create Firewall Rule Table Column Formatters
Create column formatters for each field:

**Status Column:**
- Format: `[Enabled]` or `[Disabled]` with appropriate coloring
- Width: Fixed width to accommodate the longest status

**Action Column:**
- Format: `Allow` or `Deny` with appropriate coloring
- Width: Fixed width for "Allow"/"Deny"

**Rule Name Column:**
- Format: Rule name or "Unnamed Rule" if empty
- Width: Flexible, truncate with ellipsis if too long
- Handle null/empty rule names gracefully

**Protocol Column:**
- Format: `UDP`, `TCP`, etc.
- Width: Fixed width for common protocols

**Port Column:**
- Format: Port number, `*`, or `any` for wildcards
- Width: Fixed width to accommodate port numbers and wildcards

**Direction Column:**
- Format: `(Any → ThisDevice)` format (keep current arrow format)
- Width: Flexible to accommodate various direction formats

**Scope Column:**
- Format: `Global` or truncated application path
- Width: Flexible, truncate long paths with ellipsis
- Handle null/empty application names

### 4. Integrate Table Rendering into NetworkStatusFormatter
- Update `AppendFirewallRules` method to use `ITableFormatter.AppendTable`
- Create the column formatters list using `FirewallRuleTableFormatters.CreateColumnFormatters()`
- Set target column count to 1 (single column layout)
- Pass the indent parameter from the method parameter
- Change rule limit from 5 to 10
- Update the "more rules" message to show "top 10 of X" when applicable
- Modify `AppendFirewallRules` to accept `ITableFormatter` as a parameter
- Update the constructor to inject `ITableFormatter` dependency
- Update all callers to pass the table formatter

### 5. Handle Edge Cases
- Empty rule lists: Show "No explicit rules found" message (current behavior)
- Rule count display: "Matching rules (X found):" or "Matching rules (top 10 of X):"
- Long rule names: Truncate with ellipsis
- Long application paths: Truncate with ellipsis
- Wildcard values: Display as `*` or `any` consistently

### 6. Update Tests
- Modify existing tests to expect table format instead of list format
- Add tests for the new table formatter integration
- Test edge cases (empty rules, long names, etc.)
- Test indentation functionality

## Benefits of This Approach

1. **Consistent UI**: Aligns with the rest of the application's table-based display
2. **Better Readability**: Clear column separation makes scanning easier
3. **Maintainable**: Uses existing infrastructure
4. **Flexible**: Easy to adjust column widths and add/remove columns
5. **Professional**: More polished appearance than current list format

## Design Decisions

- **Headers**: Display table headers for clarity
- **Direction Format**: Keep current arrow format `(Any → ThisDevice)` - it's more than just direction
- **Sorting**: Present rules in whatever current order is (no additional sorting)
- **Rule Limit**: Increased from 5 to 10 rules for better coverage
- **Indentation**: Add as parameter to table formatter for reusability

## Files to Modify

1. `Interfaces/ITableFormatter.cs` - Add indent parameter
2. `Utilities/TableFormatter.cs` - Implement indent functionality
3. `Utilities/NetworkStatusFormatter.cs` - Convert to table format
4. `Tests/Utilities/NetworkStatusFormatterTests.cs` - Update tests
5. `Tests/Utilities/TableFormatterTests.cs` - Add indent tests

## Dependencies

- `ITableFormatter` interface
- `ITableColumnFormatter<T>` interface
- `ConsoleColors` utility for coloring
- `FirewallRule` model
- `FirewallAnalysisResult` model
