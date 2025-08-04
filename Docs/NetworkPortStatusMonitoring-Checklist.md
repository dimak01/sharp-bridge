# Network Port Status Monitoring - Implementation Checklist

## Phase 1: Core Infrastructure

### 1.1 Create Interfaces and Data Models

- [x] **Create `IFirewallAnalyzer` interface**
  - [x] Define `AnalyzeFirewallRules` method signature
  - [x] Create `FirewallAnalysisResult` model with `IsAllowed` and `RelevantRules`
  - [x] Create `FirewallRule` model with Windows API alignment

- [x] **Create `INetworkCommandProvider` interface**
  - [x] Define platform-specific command generation methods
  - [x] Create command generation methods for add/remove firewall rules
  - [x] Create command generation methods for port status checking

- [x] **Create `IPortStatusMonitor` interface**
  - [x] Define `GetNetworkStatusAsync` method
  - [x] Create `NetworkStatus` model with iPhone and PC connection statuses
  - [x] Create `PortStatus` model for individual port information

- [x] **Create `INetworkStatusFormatter` interface**
  - [x] Define `RenderNetworkTroubleshooting` method
  - [x] Ensure no dependency on `IFormatter` (no main console display)

### 1.2 Implement Windows Firewall Analyzer

- [x] **Implement `WindowsFirewallAnalyzer` class**
  - [x] Use `INetFwPolicy2` COM interface for Windows Firewall access
  - [x] Implement rule enumeration for UDP/TCP protocols
  - [x] Implement rule precedence logic
  - [x] Handle network location detection (local vs remote)
  - [x] Return simple `IsAllowed` status with relevant rules

- [x] **Add Windows Firewall dependencies**
  - [x] Add COM interop for `INetFwPolicy2`
  - [x] Add Windows Firewall rule enumeration logic
  - [x] Add rule state checking (enabled/disabled)

### 1.3 Implement Windows Network Command Provider

- [x] **Implement `WindowsNetworkCommandProvider` class**
  - [x] Create `GetAddFirewallRuleCommand` method
  - [x] Create `GetRemoveFirewallRuleCommand` method
  - [x] Create `GetCheckPortStatusCommand` method
  - [x] Create `GetTestConnectivityCommand` method
  - [x] Ensure commands are copy-paste ready for users

- [x] **Add command templates**
  - [x] UDP outbound rule commands
  - [x] TCP/WebSocket rule commands
  - [x] Port status checking commands
  - [x] Rule removal commands

### 1.4 Implement Port Status Monitor

- [x] **Implement `PortStatusMonitor` class**
  - [x] Inject `IFirewallAnalyzer` dependency
  - [x] Implement local port binding checks
  - [x] Implement outbound connectivity tests (no UDP packet sending)
  - [x] Use existing configuration values (`PhoneClient`, `PCClient`)
  - [x] Handle `UsePortDiscovery` setting for monitoring scope
  - [x] Implement continuous polling (5-second intervals)

- [x] **Add network testing logic**
  - [x] Local UDP port 28964 binding test
  - [x] Outbound UDP 21412 connectivity test
  - [x] WebSocket port connectivity test
  - [x] Discovery port connectivity test (when enabled)

## Phase 2: Help System Integration

### 2.1 Implement Network Status Formatter

- [x] **Implement `NetworkStatusFormatter` class**
  - [x] Inject `INetworkCommandProvider` dependency
  - [x] Implement `RenderNetworkTroubleshooting` method
  - [x] Format current status display
  - [x] Format firewall rules display
  - [x] Format platform-specific commands
  - [x] Use existing console color system

- [x] **Add formatting logic**
  - [x] Status indicators (🟢/🔴/🟡)
  - [x] Firewall rule listing
  - [x] Command formatting with proper spacing
  - [x] Integration with existing help formatting

### 2.2 Extend System Help Renderer

- [x] **Update `SystemHelpRenderer` class**
  - [x] Inject `INetworkStatusFormatter` dependency
  - [x] Add `NetworkStatus` parameter to `RenderSystemHelp` method
  - [x] Add network troubleshooting section to help display
  - [x] Integrate with existing help formatting patterns

- [x] **Add help integration**
  - [x] Call `RenderNetworkTroubleshooting` when network status provided
  - [x] Maintain existing help layout and formatting
  - [x] Ensure proper section separation

### 2.3 Integrate Command Provider

- [x] **Connect command generation**
  - [x] Use `INetworkCommandProvider` in `NetworkStatusFormatter`
  - [x] Generate platform-specific commands for each connection
  - [x] Ensure commands use actual configuration values
  - [x] Test command generation with different scenarios

## Phase 3: Service Integration

### 3.1 Update Service Registration

- [x] **Register new services in `ServiceRegistration.cs`**
  - [x] Register `IPortStatusMonitor` as singleton
  - [x] Register `IFirewallAnalyzer` as singleton
  - [x] Register `INetworkCommandProvider` as singleton
  - [x] Register `INetworkStatusFormatter` as singleton

### 3.2 Update Application Orchestrator

- [x] **Integrate with `ApplicationOrchestrator`**
  - [x] Inject `IPortStatusMonitor` dependency
  - [x] Collect network status during initialization
  - [x] Pass network status to help renderer when needed
  - [x] Integrate with existing health monitoring system

### 3.3 Update System Help Integration

- [ ] **Connect help system**
  - [ ] Pass `NetworkStatus` to `SystemHelpRenderer`
  - [ ] Ensure network troubleshooting appears in F1 help
  - [ ] Test help display with different network scenarios

## Phase 4: Testing & Validation

### 4.1 Unit Tests

- [ ] **Create `WindowsFirewallAnalyzerTests`**
  - [ ] Test firewall rule analysis logic
  - [ ] Test rule precedence handling
  - [ ] Test network location detection
  - [ ] Mock Windows Firewall API calls

- [ ] **Create `WindowsNetworkCommandProviderTests`**
  - [ ] Test command generation for different scenarios
  - [ ] Test command formatting and parameters
  - [ ] Verify copy-paste ready commands

- [ ] **Create `PortStatusMonitorTests`**
  - [ ] Test local port binding checks
  - [ ] Test outbound connectivity tests
  - [ ] Test configuration-aware monitoring
  - [ ] Mock network operations

- [ ] **Create `NetworkStatusFormatterTests`**
  - [ ] Test troubleshooting display formatting
  - [ ] Test integration with command provider
  - [ ] Test different network status scenarios

### 4.2 Integration Tests

- [ ] **Test with different network configurations**
  - [ ] Discovery enabled vs disabled
  - [ ] Different iPhone IP addresses
  - [ ] Different PC host configurations
  - [ ] Various firewall rule scenarios

- [ ] **Test help system integration**
  - [ ] Verify network troubleshooting appears in F1 help
  - [ ] Test with different network status scenarios
  - [ ] Verify command generation works correctly

### 4.3 Manual Testing

- [ ] **Test firewall analysis**
  - [ ] Test with existing firewall rules
  - [ ] Test with missing firewall rules
  - [ ] Test with conflicting firewall rules
  - [ ] Test with disabled firewall rules

- [ ] **Test command generation**
  - [ ] Verify commands work when executed
  - [ ] Test add/remove firewall rule commands
  - [ ] Test port status checking commands

## Configuration & Documentation

### 5.1 Configuration Integration

- [ ] **Verify no new configuration required**
  - [ ] Test with existing `PhoneClient` settings
  - [ ] Test with existing `PCClient` settings
  - [ ] Test with `UsePortDiscovery` setting
  - [ ] Ensure feature works with default configuration

### 5.2 Documentation Updates

- [ ] **Update README.md**
  - [ ] Add network troubleshooting section
  - [ ] Document F1 help system features
  - [ ] Add troubleshooting examples

- [ ] **Update ProjectOverview.md**
  - [ ] Add network monitoring to architecture diagram
  - [ ] Document new interfaces and services
  - [ ] Update implementation status

## Success Criteria Validation

- [ ] **Real-time monitoring works**
  - [ ] Port status updates every 5 seconds
  - [ ] No performance impact on main application

- [ ] **Configuration awareness works**
  - [ ] Monitoring adapts to `UsePortDiscovery` setting
  - [ ] Correct ports monitored based on configuration

- [ ] **Firewall analysis works**
  - [ ] Accurate detection of firewall rule issues
  - [ ] Relevant rules displayed correctly
  - [ ] `IsAllowed` status reflects actual connectivity

- [ ] **Platform commands work**
  - [ ] Windows-specific commands generated correctly
  - [ ] Commands are copy-paste ready
  - [ ] Commands work when executed by users

- [ ] **Help integration works**
  - [ ] Network troubleshooting appears in F1 help
  - [ ] Status and commands display correctly
  - [ ] Integration with existing help system

- [ ] **Zero configuration works**
  - [ ] Feature works with existing configuration
  - [ ] No new configuration files required
  - [ ] Sensible defaults work correctly

- [ ] **Non-elevated operation works**
  - [ ] App runs with normal user privileges
  - [ ] No automatic port manipulation
  - [ ] Users execute commands manually

## Notes

- **Priority**: Focus on Windows implementation first
- **Testing**: Emphasize firewall analysis accuracy
- **User Experience**: Ensure commands are easy to copy-paste
- **Performance**: Monitor impact of continuous polling
- **Security**: No automatic firewall rule manipulation

## Implementation Guidelines

### Firewall Analysis
- **Scope**: Both inbound and outbound rules (especially important for UDP)
- **Network Detection**: Map IPs/hosts to available networks (localhost vs remote)
- **Error Handling**: Return "Unknown" status if Windows Firewall API calls fail
- **Models**: Separate Windows API interop models from simplified consumption models

### Port Status Monitoring
- **No Connection Tests**: Don't send packets or establish connections
- **Local Ports**: Check if open/closed, no binding checks
- **Configuration**: Let orchestrator pass configuration values (don't inject IConfigManager)
- **Network Location**: Detect if PC connections are localhost OR remote hosts

### Data Models
- **FirewallRule**: Separate Windows API structure from simplified consumption model
- **NetworkStatus**: Include timestamps for when each check was performed 