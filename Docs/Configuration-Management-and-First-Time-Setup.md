# Configuration Management and Field-Driven Remediation

### Summary

- **Goal**: Provide a resilient, field-driven approach to application configuration with automatic detection and remediation of any configuration issues (missing fields, wrong types, invalid values).
- **What's covered**: Loading/recreating configs, field-level validation, automatic remediation for any config issues, and saving corrected configurations.
- **Not covered**: Transformation rules remain strictly file-based user content and are shipped as a separate file (not embedded). Their handling is unchanged.

### Scope and Principles

- **Single source of truth** for defaults is code (DTO property initializers). No embedded defaults needed.
- **Field-driven validation** - Each configuration section validates its fields independently using `ConfigFieldState`.
- **Automatic remediation** - Any configuration issues trigger an interactive setup to fix problems.
- **No complex versioning** - We handle breaking changes through field-level detection and remediation.
- **Uniform behavior** for `ApplicationConfig.json` and `UserPreferences.json`.

---

## Startup Flow

1) **Load-or-Create**
   - Attempt to load config from disk; if missing or unreadable, recreate from current code defaults.
   - Apply the same for both `ApplicationConfig.json` and `UserPreferences.json`.

2) **Field-Level Validation**
   - For each configuration section, load raw field data using `GetSectionFieldsAsync<T>()`.
   - Validate each section's fields independently using section-specific validators.
   - Identify any missing, invalid, or problematic fields.

3) **Automatic Remediation (If Needed)**
   - If any section has validation issues, run the remediation setup for that section.
   - Prompt user for missing required fields, fix invalid values, handle type conversions.
   - Continue until all sections are valid.

4) **Save Corrected Configurations**
   - Save any corrected sections back to disk.
   - Ensure all configurations are now valid and complete.

5) **Start Watchers, Then Services**
   - After any potential saves, start file watchers (to avoid self-triggered reload loops).
   - Initialize services (phone/PC clients, transformation engine, console UI) and proceed with normal operation.

---

## Runtime Changes

- **On `ApplicationConfig.json` change:**
  - Reload, validate each section's fields, apply remediation if needed.
  - If any section has issues, re-enter remediation setup.
  - Otherwise, apply the updated config and continue.

- **On `UserPreferences.json` change:**
  - Reload, apply DTO defaults for gaps, continue (no required fields expected).

---

## Configuration Management

### DTOs and Defaults
- Defaults live in DTO property initializers, keeping behavior centralized and version-controlled with the code.
- Each DTO implements `IConfigSection` for consistent handling.
- Example:
```csharp
public class VTubeStudioPCConfig : IConfigSection
{
    [Required]
    [Description("Host Address")]
    public string Host { get; set; } = "localhost";
    
    [Description("Port Number")]
    public int Port { get; set; } = 8001;
}
```

### Field-Driven Architecture
- **ConfigFieldState** captures the raw state of each field (present/missing, value, type, description).
- **Section-specific validators** check fields for their respective configuration sections.
- **Factory-based services** provide validators and remediation services for any section type.
- **Enum-driven iteration** using `ConfigSectionTypes` for type-safe section handling.

### Why Field-Level Remediation
- **Detects any configuration issues** - missing fields, wrong types, invalid values.
- **Handles breaking changes gracefully** - field renames, type changes, new required fields.
- **No complex migration logic** - just fix what's broken and continue.
- **User-friendly** - interactive prompts for any issues that need user input.

---

## Remediation Setup

### Trigger Conditions
- Any configuration section has validation issues (missing required fields, invalid types, etc.).
- Config was recreated and needs initial setup.

### Interaction Model (Console)
- **Section-by-section remediation** - fix issues in one section before moving to the next.
- **Field-specific prompts** - only ask for fields that have problems.
- **Type conversion** - automatically handle simple type changes (e.g., string to int).
- **Validation feedback** - show friendly errors and allow retry.

### Integration Point
- Remediation is invoked during `InitializeAsync` in the orchestrator, before watchers are started and before services are initialized.
- This ensures services receive complete, valid configurations.

---

## Responsibilities & Boundaries

- **ConfigManager**: Load/save sections, provide field-level access via `GetSectionFieldsAsync<T>()`.
- **IConfigSectionValidator**: Validate fields for a specific configuration section.
- **IConfigSectionFirstTimeSetupService**: Remediate issues in a specific configuration section.
- **Factory Services**: Provide validators and remediation services for any section type.
- **ConfigRemediationService**: Orchestrate the overall validation and remediation flow.
- **ApplicationOrchestrator**: Coordinate the sequence in `InitializeAsync` and handle runtime changes via file watchers.

---

## Transformation Rules (Unchanged)

- Transformation rules remain **strictly file-based user content**. The application ships a default rules file alongside the app (not embedded). Users are expected to customize these rules. Handling, hot-reload key bindings, and validation remain as documented elsewhere.

---

## Future Enhancements (Optional)

- **Firewall check and Network Troubleshooting mode** immediately after remediation when connectivity issues are detected.
- **Configuration backup** before making automatic corrections.
- **Configuration health monitoring** to detect and prevent common configuration issues.