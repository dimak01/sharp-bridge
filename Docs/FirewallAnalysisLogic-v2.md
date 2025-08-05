# Windows Firewall Analysis Logic v2

## Overview

This document defines the algorithm for analyzing Windows Firewall rules to determine if network connectivity is allowed for SharpBridge's connections. The analysis focuses on **local system firewall settings** rather than remote service availability, using **interface-based analysis** rather than single profile concepts.

## Connection Types Analyzed

### 1. iPhone UDP Connection
- **Inbound**: iPhone → SharpBridge (UDP port 28964)
- **Outbound**: SharpBridge → iPhone (UDP port 28964)

### 2. PC VTube Studio Connection
- **Outbound**: SharpBridge → PC WebSocket (TCP port 8001 or discovered)
- **Outbound**: SharpBridge → PC Discovery (UDP port 47779, if discovery enabled)

## Core Analysis Algorithm

### Step 1: Environment Context
```csharp
// Required context for accurate analysis
var firewallServiceState = GetFirewallServiceState();   // Enabled/Disabled/BlockAll
var activeInterfaces = GetActiveNetworkInterfaces();   // All network interfaces
```

**Why this matters:**
- **Firewall State**: If disabled → everything allowed; if "Block All" → everything blocked unless explicit allow
- **Multiple Interfaces**: Each interface has its own profile (Domain/Private/Public)
- **Interface-Specific Rules**: Rules apply to specific interfaces, not global profiles

### Step 2: Connection Direction Analysis

#### **Inbound Connections (iPhone → SharpBridge)**
```csharp
// SharpBridge is listening on local port 28964
// Note: SharpBridge may be bound to INADDR_ANY (0.0.0.0), not a specific interface
var listeningInterfaces = GetInterfacesForLocalPort("28964");
var inboundRules = new List<FirewallRule>();
foreach (var iface in listeningInterfaces)
{
    var profile = GetProfileForInterface(iface);
    var rules = GetInboundRules(profile, "UDP", "28964");
    inboundRules.AddRange(rules);
}
```

#### **Outbound Connections (SharpBridge → iPhone/PC)**
```csharp
// Use GetBestInterface() to determine the actual interface Windows would use
var bestInterface = GetBestInterfaceForTarget(remoteHost);
if (bestInterface != null)
{
    var profile = GetProfileForInterface(bestInterface);
    var rules = GetOutboundRules(profile, protocol, remoteHost, remotePort);
    return new FirewallAnalysisResult { IsAllowed = AnalyzeRules(rules), RelevantRules = rules };
}
else
{
    // Fallback: analyze all interfaces that could reach the target
    var targetInterfaces = GetAllInterfacesForTarget(remoteHost);
    var allRules = new List<FirewallRule>();
    foreach (var iface in targetInterfaces)
    {
        var profile = GetProfileForInterface(iface);
        var rules = GetOutboundRules(profile, protocol, remoteHost, remotePort);
        allRules.AddRange(rules);
    }
    return new FirewallAnalysisResult { IsAllowed = AnalyzeRules(allRules), RelevantRules = allRules };
}
```

### Step 3: Interface Detection Strategy

#### **For Localhost Connections**
```csharp
private List<NetworkInterface> GetInterfacesForTarget(string targetIP)
{
    if (targetIP == "127.0.0.1" || targetIP == "localhost")
    {
        // Localhost: Check loopback interface + any interface that might route to it
        return GetLoopbackInterfaces();
    }
    
    // Remote IP: Use GetBestInterface() for accurate routing
    var bestInterface = GetBestInterfaceForTarget(targetIP);
    return bestInterface != null ? new List<NetworkInterface> { bestInterface } : GetAllInterfacesForTarget(targetIP);
}
```

#### **Profile Intersection Logic**
```csharp
// Match rules by intersecting rule.Profiles with interface profile
// Handle NET_FW_PROFILE2_ALL (value 7) which matches all profiles
var relevantRules = GetAllRules().Where(rule => 
    (rule.Profiles & interfaceProfile) != 0 || 
    (rule.Profiles & 7) == 7 &&  // NET_FW_PROFILE2_ALL
    rule.Enabled == true).ToList();
```

### Step 4: Rule Precedence Order (Actual Windows Firewall Behavior)

For each connection, Windows Firewall evaluates rules in this **actual precedence order**:

#### **Priority 1: Block Rules (HIGHEST)**
```
Rule: "Block UDP inbound to port 28964 from any"
Scope: Any block rule that matches the connection
Precedence: 1 (overrides ALL allow rules)
```

#### **Priority 2: Allow Rules by Specificity (MEDIUM)**
```
Rule: "Allow UDP inbound to port 28964 from 192.168.1.100"
Scope: Allow rules, sorted by specificity
Precedence: 2 (most specific allow rule wins)
```

#### **Priority 3: Default Policy (LOWEST)**
```
Default: Based on network profile and direction
- Inbound: Block connections (unless explicit allow)
- Outbound: Allow connections (unless explicit block)
Precedence: 3 (lowest priority)
```

### **Specificity Rules (Within Allow/Block Categories)**

1. **Application-specific rules** beat **port/protocol rules**
2. **Specific IP addresses** beat **subnets/ranges**
3. **Specific ports** beat **port ranges**
4. **More specific criteria** beat **less specific criteria**

### Step 5: Rule Type Coverage (100% Complete)

#### **A. Host-Specific Rules**
```
Rule: "Allow UDP from 192.168.1.100 to port 28964"
Rule: "Block TCP to 192.168.1.100"
Precedence: HIGH (exact host match)
```

#### **B. Subnet Rules**
```
Rule: "Allow UDP from 192.168.1.0/24 to port 28964"
Rule: "Block TCP to 192.168.1.0/24"
Precedence: HIGH (subnet match)
```

#### **C. Port-Specific Rules**
```
Rule: "Allow UDP inbound to port 28964 from any"
Rule: "Block TCP outbound to port 8001"
Precedence: MEDIUM (exact port match)
```

#### **D. Port Range Rules**
```
Rule: "Allow UDP inbound to ports 28960-28970 from any"
Rule: "Block TCP outbound to ports 8000-8010"
Precedence: MEDIUM (port range match)
```

#### **E. Protocol-Specific Rules**
```
Rule: "Allow UDP from any to any"
Rule: "Block TCP from any to any"
Precedence: LOW (protocol match only)
```

#### **F. Application Rules**
```
Rule: "Allow SharpBridge.exe through firewall"
Rule: "Block SharpBridge.exe from network access"
Precedence: HIGHEST (application-specific)
```

#### **G. Interface-Specific Rules**
```
Rule: "Allow UDP on WiFi interface only"
Rule: "Block TCP on Ethernet interface"
Precedence: MEDIUM (interface + other criteria)
```

#### **H. Time-Based Rules**
```
Rule: "Allow UDP inbound to port 28964 from 9 AM to 5 PM"
Rule: "Block all connections outside business hours"
Precedence: SAME as base rule type (time is additional filter)
```

### Step 6: The Complete Analysis Algorithm

```csharp
public FirewallAnalysisResult AnalyzeFirewallRules(
    string? localPort, 
    string remoteHost, 
    string remotePort, 
    string protocol)
{
    // 1. Get environment context
    var firewallState = GetFirewallServiceState();
    
    // 2. If firewall is disabled, everything is allowed
    if (firewallState == FirewallState.Disabled)
        return new FirewallAnalysisResult { IsAllowed = true, RelevantRules = new List<FirewallRule>() };
    
    // 3. If firewall is "Block All", everything is blocked unless explicit allow
    if (firewallState == FirewallState.BlockAll)
    {
        var allowRules = GetExplicitAllowRules(localPort, remoteHost, remotePort, protocol);
        return new FirewallAnalysisResult 
        { 
            IsAllowed = allowRules.Any(),
            RelevantRules = allowRules.Take(5).ToList()
        };
    }
    
    // 4. Analyze based on connection direction
    if (localPort != null)
    {
        // Inbound analysis: SharpBridge is listening
        return AnalyzeInboundConnection(localPort, remoteHost, remotePort, protocol);
    }
    else
    {
        // Outbound analysis: SharpBridge is connecting
        return AnalyzeOutboundConnection(remoteHost, remotePort, protocol);
    }
}

private FirewallAnalysisResult AnalyzeInboundConnection(
    string localPort, string remoteHost, string remotePort, string protocol)
{
    // Get interfaces SharpBridge is listening on (may be multiple if bound to INADDR_ANY)
    var listeningInterfaces = GetInterfacesForLocalPort(localPort);
    var allRules = new List<FirewallRule>();
    
    foreach (var iface in listeningInterfaces)
    {
        var profile = GetProfileForInterface(iface);
        var rules = GetInboundRules(profile, protocol, localPort);
        allRules.AddRange(rules);
    }
    
    var effectiveRule = DetermineEffectiveRule(allRules);
    
    return new FirewallAnalysisResult 
    { 
        IsAllowed = effectiveRule.Action == "Allow",
        RelevantRules = allRules.Take(5).ToList()
    };
}

private FirewallAnalysisResult AnalyzeOutboundConnection(
    string remoteHost, string remotePort, string protocol)
{
    // Use GetBestInterface() for accurate routing
    var bestInterface = GetBestInterfaceForTarget(remoteHost);
    if (bestInterface != null)
    {
        var profile = GetProfileForInterface(bestInterface);
        var rules = GetOutboundRules(profile, protocol, remoteHost, remotePort);
        var effectiveRule = DetermineEffectiveRule(rules);
        
        return new FirewallAnalysisResult 
        { 
            IsAllowed = effectiveRule.Action == "Allow",
            RelevantRules = rules.Take(5).ToList()
        };
    }
    else
    {
        // Fallback: analyze all interfaces that could reach the target
        var targetInterfaces = GetAllInterfacesForTarget(remoteHost);
        var allRules = new List<FirewallRule>();
        
        foreach (var iface in targetInterfaces)
        {
            var profile = GetProfileForInterface(iface);
            var rules = GetOutboundRules(profile, protocol, remoteHost, remotePort);
            allRules.AddRange(rules);
        }
        
        var effectiveRule = DetermineEffectiveRule(allRules);
        
        return new FirewallAnalysisResult 
        { 
            IsAllowed = effectiveRule.Action == "Allow",
            RelevantRules = allRules.Take(5).ToList()
        };
    }
}

private FirewallRule DetermineEffectiveRule(List<FirewallRule> rules)
{
    // Filter enabled rules only
    var enabledRules = rules.Where(r => r.Enabled).ToList();
    
    // Sort by precedence and return first matching rule
    var sortedRules = enabledRules.OrderBy(r => GetPrecedenceScore(r)).ToList();
    return sortedRules.FirstOrDefault() ?? GetDefaultPolicy();
}

private int GetPrecedenceScore(FirewallRule rule)
{
    // Lower score = higher precedence
    if (IsApplicationRule(rule)) return 1;                 // HIGHEST
    if (IsExactMatch(rule)) return 2;                     // HIGH
    if (IsSubnetMatch(rule)) return 3;                    // MEDIUM-HIGH
    if (IsPortSpecific(rule)) return 4;                   // MEDIUM
    if (IsPortRangeMatch(rule)) return 5;                 // MEDIUM-LOW
    if (IsProtocolSpecific(rule)) return 6;               // LOW
    return 7;                                              // LOWEST (default)
}
```

### Step 7: Rule Matching Logic

#### **Application Rule Detection**
```csharp
private bool IsApplicationRule(FirewallRule rule)
{
    if (string.IsNullOrEmpty(rule.ApplicationName))
        return false;
    
    // See FirewallAnalysisLogic-AppNameInFirewallRules.md for detailed application path matching
    var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
    var ruleAppPath = CleanApplicationPath(rule.ApplicationName);
    
    return string.Equals(ruleAppPath, currentExePath, StringComparison.OrdinalIgnoreCase);
}

private string CleanApplicationPath(string input)
{
    // Strip quotes and normalize
    return input.Trim().Trim('"').Trim('\'');
}
```

#### **Exact Match Detection**
```csharp
private bool IsExactMatch(FirewallRule rule)
{
    // Rule matches if ALL criteria are specific (not wildcard)
    return rule.Protocol == "UDP" && 
           rule.LocalPort == "28964" && 
           rule.RemotePort == "28964" && 
           rule.RemoteAddress == "192.168.1.100";
}
```

#### **Port Range Detection**
```csharp
private bool IsPortInRange(string port, string rulePort)
{
    if (rulePort.Contains("-"))
    {
        var range = rulePort.Split('-');
        var min = int.Parse(range[0]);
        var max = int.Parse(range[1]);
        var portNum = int.Parse(port);
        return portNum >= min && portNum <= max;
    }
    return port == rulePort;
}
```

#### **Subnet Match Detection**
```csharp
private bool IsHostInSubnet(string host, string subnet)
{
    if (subnet.Contains("/"))
    {
        // Parse CIDR notation (e.g., "192.168.1.0/24")
        var parts = subnet.Split('/');
        var network = parts[0];
        var mask = int.Parse(parts[1]);
        return IsHostInNetwork(host, network, mask);
    }
    return host == subnet;
}
```

#### **Address Normalization**
```csharp
private string NormalizeAddress(string address)
{
    // Handle special Windows Firewall address values
    switch (address?.ToLower())
    {
        case "*":
        case "any":
            return "0.0.0.0";
        case "<localsubnet>":
            return GetLocalSubnet();
        case "<local>":
            return "127.0.0.1";
        default:
            return address;
    }
}
```

## Implementation Strategy

### **Phase 1: Core Analysis (Current Focus)**
1. **Environment Detection**: Firewall state + active interfaces
2. **Connection Direction**: Inbound vs outbound analysis
3. **Interface Detection**: Use GetBestInterface() for accurate routing
4. **Rule Enumeration**: Interface-specific rule filtering (enabled only)
5. **Application Rules**: Check SharpBridge.exe specific rules
6. **Precedence Logic**: Implement the precedence scoring system
7. **Rule Matching**: Handle exact matches, ranges, subnets, etc.

### **Phase 2: Out of Scope (Future Enhancements)**
1. **Time-based Rules**: Handle rules with time restrictions (with timezone awareness)
2. **Group Policy Awareness**: Detect locked/read-only rules

## Testing Scenarios (100% Coverage)

### **Basic Scenarios**
- [ ] No firewall rules → Default behavior
- [ ] Allow rule exists → Connection allowed
- [ ] Block rule exists → Connection blocked
- [ ] Both allow and block rules → Precedence determines result
- [ ] Disabled rules → Ignored in analysis

### **Connection Direction Scenarios**
- [ ] **Inbound analysis**: iPhone → SharpBridge (UDP 28964)
- [ ] **Outbound analysis**: SharpBridge → iPhone (UDP 28964)
- [ ] **Outbound analysis**: SharpBridge → PC (TCP 8001)
- [ ] **Localhost connections**: SharpBridge → localhost (WebSocket)

### **Interface Scenarios**
- [ ] **Single interface**: Simple analysis
- [ ] **Multiple interfaces**: All relevant interfaces checked
- [ ] **Loopback interface**: Localhost connections
- [ ] **VPN interfaces**: Unexpected routing behavior
- [ ] **Interface binding**: SharpBridge bound to specific interface
- [ ] **GetBestInterface()**: Accurate routing determination

### **Rule Type Scenarios**
- [ ] **Application rules**: SharpBridge.exe specific permissions
- [ ] **Exact matches**: Specific host + port + protocol
- [ ] **Subnet rules**: CIDR notation (192.168.1.0/24)
- [ ] **Port ranges**: Port ranges (28960-28970)
- [ ] **Protocol rules**: Protocol-only rules (any UDP)
- [ ] **Interface rules**: Interface-specific rules
- [ ] **Time-based rules**: Rules with time restrictions
- [ ] **Profile ALL rules**: Rules that apply to all profiles

### **Environment Scenarios**
- [ ] **Network profiles**: Domain/Private/Public rule sets per interface
- [ ] **Firewall states**: Enabled/Disabled/BlockAll
- [ ] **Domain policies**: Group policy controlled rules
- [ ] **Service unavailable**: Windows Firewall service down
- [ ] **Multiple profiles**: Different profiles per interface
- [ ] **Localhost behavior**: Loopback interface analysis

### **Edge Cases**
- [ ] **VPN scenarios**: Unexpected routing behavior
- [ ] **Address normalization**: Wildcards and special values
- [ ] **Profile conflicts**: Multiple active profiles
- [ ] **Group policy locks**: Read-only rules
- [ ] **Time zone handling**: Time-based rule evaluation
- [ ] **Interface selection**: SharpBridge binding strategy
- [ ] **INADDR_ANY binding**: SharpBridge bound to all interfaces

## Success Criteria

### **For Each Connection Type:**
1. ✅ **Correctly identifies if firewall allows the connection**
2. ✅ **Reports relevant firewall rules affecting the connection**
3. ✅ **Handles all rule types (exact, subnet, range, protocol, etc.)**
4. ✅ **Considers interface-specific profiles and firewall state**
5. ✅ **Applies proper precedence order**
6. ✅ **Uses GetBestInterface() for accurate routing**
7. ✅ **Handles localhost scenarios properly**
8. ✅ **Distinguishes inbound vs outbound analysis**

### **For Implementation:**
1. ✅ **Covers 100% of Windows Firewall rule types**
2. ✅ **Handles edge cases (disabled firewall, group policy, etc.)**
3. ✅ **Provides actionable troubleshooting information**
4. ✅ **Maintains performance (efficient rule enumeration)**
5. ✅ **Supports enterprise environments with complex profiles**
6. ✅ **Uses interface-based analysis instead of single profile concept**
7. ✅ **Handles NET_FW_PROFILE2_ALL rules correctly** 