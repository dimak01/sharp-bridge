## Configuration Management, Versioning, and First-Time Setup

### Summary

- **Goal**: Provide a resilient, uniform approach to application configuration (ApplicationConfig and UserPreferences) with forward-only versioning and a guided first-time setup when required fields are missing.
- **What’s covered**: Loading/recreating configs, version probing and migration, gap-filling via DTO defaults, validation of required fields, and an interactive first-time setup that only asks for missing essentials.
- **Not covered**: Transformation rules remain strictly file-based user content and are shipped as a separate file (not embedded). Their handling is unchanged.

### Scope and Principles

- **Single source of truth** for defaults is code (DTO property initializers). No embedded defaults needed for these two files.
- **Forward-only versioning** with small, idempotent migration steps (vN→vN+1, chained as needed); no downgrades.
- **No write-back upgrades** by default: we upgrade in memory and proceed. Disk is not rewritten unless we explicitly choose to in a future enhancement.
- **Uniform behavior** for `ApplicationConfig.json` and `UserPreferences.json`.
- **Required fields gate**: If mandatory fields are missing (e.g., phone IP), we run a focused first-time setup to collect only those values, save, and continue initialization.

---

## Startup Flow

1) Load-or-Create
- Attempt to load config from disk; if missing or unreadable, recreate from current code defaults.
- Apply the same for both `ApplicationConfig.json` and `UserPreferences.json`.

2) Version Handling
- Probe version via lightweight JSON scan (e.g., JsonDocument/JsonNode).
- If version is missing → treat as broken → recreate from current code defaults.
- If version < current → deserialize into legacy DTO, run chained forward migrations to the current DTO (in memory only).

3) Fill Gaps via DTO Defaults
- For scalars and nested objects, System.Text.Json leaves unspecified fields at DTO-initializer defaults.
- For dictionaries/collections, we keep existing component semantics (e.g., missing shortcut entries are treated as disabled rather than deep-merged).

4) Validate Required Fields (Gate)
- Validate that mandatory connection settings are present:
  - Phone: `IphoneIpAddress` must be non-empty/valid. Ports can use defaults.
  - PC: if `UsePortDiscovery == true` → OK; else `Host` must be non-empty and `Port > 0`.
- If validation fails → run first-time setup to collect only missing/invalid fields.

5) First-Time Setup (If Needed)
- Interactive (console) prompts for only the missing values identified by validation.
- After successful entry, save `ApplicationConfig.json` and re-validate.

6) Start Watchers, Then Services
- After any potential first-time save, start file watchers (to avoid self-triggered reload loops during setup).
- Initialize services (phone/PC clients, transformation engine, console UI) and proceed with normal operation.

Notes:
- If desired later, we can run a firewall check after first-time setup and jump to Network Troubleshooting mode when issues are detected.

---

## Runtime Changes

- On `ApplicationConfig.json` change:
  - Reload, migrate if needed, apply DTO defaults for gaps, then validate.
  - If required fields are now missing, re-enter first-time setup (or present a blocking banner to complete setup before continuing).
  - Otherwise, apply the updated config (e.g., reload shortcuts) and continue.

- On `UserPreferences.json` change:
  - Reload, apply DTO defaults for gaps, continue (no required fields expected).

---

## Configuration Management & Versioning

### DTOs and Defaults
- Defaults live in DTO property initializers, keeping behavior centralized and version-controlled with the code.
- Example (conceptual):
```
public class VTubeStudioPCConfig {
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 8001;
  public bool UsePortDiscovery { get; set; } = true;
}
```

### Versioning Strategy
- Each current DTO has `int Version { get; init; }` and a `CurrentVersion` constant.
- Legacy DTOs (v1, v2, …) live under a `Legacy` sub-namespace/folder and are used only during loading.
- Migration pipeline: v1→v2, v2→v3, …; each step is a small, testable function that returns the next version; idempotent and side-effect-free.
- After migration, validate and proceed without writing back to disk.

### Why No Deep Merge
- For most fields, DTO defaults are sufficient when JSON omits properties.
- For dictionaries/collections (e.g., shortcuts), merging can change semantics. Current policy intentionally treats missing entries as disabled; keep such logic in the owning component (e.g., Shortcut manager).

---

## First-Time Setup

### Trigger Conditions
- Config was recreated (missing/unreadable or missing version), or
- Validation indicates required fields are missing/invalid.

### Interaction Model (Console)
- Prompt sequentially for only the missing fields:
  - Phone IP (required), Phone/Local ports (defaults provided, user can accept or edit).
  - PC host/port only if `UsePortDiscovery == false`; otherwise discovery remains enabled.
- Validate inputs inline (IP format, port ranges), show friendly errors.
- Save `ApplicationConfig.json` after successful entry and resume initialization.

### Integration Point
- The setup is invoked during `InitializeAsync` in the orchestrator, before watchers are started and before services are initialized.
- This ensures services that require these values (e.g., `IVTubeStudioPhoneClient`) receive a complete, valid configuration.

---

## Responsibilities & Boundaries

- **ConfigManager**: load-or-create, save. No embedded defaults. No version policy inside.
- **ConfigMigrationService**: version probe, legacy DTO deserialization, chained forward migrations to current DTO.
- **ConfigValidator**: checks required fields and returns a list of missing/invalid fields (e.g., Phone_IphoneIpAddress, PC_HostOrDiscovery).
- **FirstTimeSetupService**: console prompts for missing fields; updates in-memory config; saves via ConfigManager.
- **ApplicationOrchestrator**: coordinates the sequence in `InitializeAsync` and handles runtime changes via file watchers.

---

## Transformation Rules (Unchanged)

- Transformation rules remain **strictly file-based user content**. The application ships a default rules file alongside the app (not embedded). Users are expected to customize these rules. Handling, hot-reload key bindings, and validation remain as documented elsewhere.

---

## Future Enhancements (Optional)

- **Firewall check and Network Troubleshooting mode** immediately after first-time setup when connectivity issues are detected.
- **Optional write-back upgrades** (atomic with backup) for transparency in major version jumps.