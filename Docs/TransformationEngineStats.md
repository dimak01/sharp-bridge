# TransformationEngine Statistics Design

## Overview

This document outlines the design for implementing `IServiceStatsProvider` in the `TransformationEngine` to enable real-time statistics display in the console UI, alongside the existing phone and PC client statistics.

## Current State

The `TransformationEngine` is a core component responsible for:
- Loading transformation rules from JSON configuration files
- Validating rule syntax and bounds
- Evaluating rules against tracking data to produce VTube Studio parameters
- Supporting hot-reload of configuration via Alt+K keyboard shortcut

Currently, the transformation engine does **not** implement `IServiceStatsProvider`, making its health and performance invisible in the console UI.

## Goals

1. **Visibility**: Display transformation engine health and performance in real-time console UI
2. **Debugging**: Show rule validation errors, evaluation failures, and dependency issues
3. **Monitoring**: Track configuration reload attempts and operational status
4. **Consistency**: Follow existing patterns used by phone and PC clients

## Statistics Design

### Core Metrics (Flat List)

#### **Core Rule Stats**
- **Rules Loaded**: "Total: 15, Valid: 12, Invalid: 3, Uptime: 2h 15m"
  - Combines rule counts and uptime since last successful rule load
- **Config File**: "Path: /config/rules.json, Status: Loaded"
  - Shows config file location and current load status
- **Hot Reloads**: "Attempts: 3, Successful: 2"
  - Tracks configuration reload operations (Alt+K usage)

#### **Operational Status**
- **Current Status**: Overall engine state (see status values below)
- **Last Successful Transformation**: Timestamp for health monitoring
- **Last Error**: Most recent error message for debugging

#### **Problem Details**
- **Rules Abandoned**: Table showing rules that couldn't evaluate (dependency issues, circular references, etc.)
- **Invalid Rules**: Table showing rules that failed validation (syntax errors, bound issues, etc.)

### Status Values

The transformation engine operates in a **graceful degradation** model - it continues working with whatever valid rules are available:

- **"Ready"**: All loaded rules are valid and evaluating successfully
- **"Partial"**: Some rules work, others have issues (mixed operational state)
- **"Config Error (Cached)"**: Hot-reload failed, continuing with previously loaded rules
- **"No Valid Rules"**: Configuration loaded but no rules passed validation
- **"Never Loaded"**: Initial state, no configuration load attempted
- **"Config Missing"**: Configuration file not found, no fallback rules available

### Config File Status Values

- **"Loaded"**: File found, parsed, rules extracted successfully
- **"Not Found"**: Config file doesn't exist at specified path
- **"Access Error"**: File exists but can't be read (permissions, lock, etc.)
- **"Parse Error"**: File exists but contains invalid JSON
- **"Empty"**: File parsed but contains no rules
- **"Never Loaded"**: Initial state, no load attempt made yet

## Design Decisions

### 1. Graceful Degradation
- **Decision**: Keep operating with valid rules when some rules fail
- **Rationale**: Partial functionality is better than complete failure
- **Impact**: Status reflects mixed states ("Partial"), detailed tables show specific issues

### 2. Persistent Evaluation Errors
- **Decision**: Track evaluation failures separately from validation errors
- **Rationale**: Evaluation errors persist until configuration changes, unlike transient network errors
- **Impact**: "Rules Abandoned" table shows dependency and evaluation issues

### 3. Hot Reload Error Handling
- **Decision**: Continue with cached rules when hot-reload fails
- **Rationale**: Maintains service availability during configuration experiments
- **Impact**: Status shows "Config Error (Cached)" with error details in Last Error

### 4. CurrentEntity Type
- **Decision**: Create a dedicated `TransformationEngineInfo` entity type
- **Rationale**: Transformation engine has different data needs than phone/PC clients
- **Impact**: Will need a specialized formatter for transformation-specific display

### 5. Console Display Integration
- **Decision**: Follow existing patterns (Alt+T for verbosity cycling)
- **Rationale**: Consistency with Alt+P (PC) and Alt+O (Phone) shortcuts
- **Impact**: Users get familiar interaction patterns

## Implementation Approach

### Architecture Decisions

**Follow Existing Service Patterns**: Use the same approach as `VTubeStudioPhoneClient` and `VTubeStudioPCClient` - individual tracking fields in the service class, populate `ServiceStats.Counters` in `GetServiceStats()`, and keep `IFormattableObject` entity simple.

**No Statistics Duplication**: Counters live in `ServiceStats.Counters`, current state lives in `TransformationEngineInfo`. Avoid duplicating data between internal tracking and external representation.

**Consistent Interface Implementation**: TransformationEngine implements `IServiceStatsProvider` using the same patterns as existing services for maintainability and consistency.

### Implementation Steps

1. **Add Individual Statistics Fields**: Track transformation counts, reload attempts, error states, timestamps as private fields in TransformationEngine class
2. **Implement IServiceStatsProvider**: Create `GetServiceStats()` method that populates `ServiceStats.Counters` from tracking fields
3. **Create TransformationEngineInfo Entity**: Simple `IFormattableObject` containing current state (config path, rule lists, status) - no counters or statistics logic
4. **Create TransformationEngineInfoFormatter**: Implement formatter with tabular display for rule problems, following existing formatter patterns
5. **Update ApplicationOrchestrator**: Include transformation engine stats in console display alongside phone and PC client stats
6. **Add Keyboard Shortcut**: Register Alt+T for transformation engine verbosity cycling, maintaining UI consistency

## Questions Answered

This design addresses key user questions:
1. **"Are my rules working?"** → Valid/Invalid rule counts, evaluation success tracking
2. **"Is the engine healthy?"** → Status display, last successful operation, error visibility
3. **"What's broken when it fails?"** → Detailed error tables, specific rule failure reasons
4. **"Is hot-reload working?"** → Reload attempt tracking, cached rule status 