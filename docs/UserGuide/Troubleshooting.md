# Troubleshooting

This guide helps you diagnose and resolve common issues with Sharp Bridge. Follow the troubleshooting steps in order, and check the logs for detailed error information.

## Built-in Diagnostics

Sharp Bridge provides comprehensive built-in diagnostics to help you identify and resolve issues. **Always check the console output first** - most problems are clearly indicated there.

### Console Status Indicators

#### Service Status Colors
- **Green (✓)**: Service is healthy and working
- **Yellow (⚠)**: Service has warnings but is functional  
- **Red (✗)**: Service has errors and needs attention

#### Main Status Mode (Default)
The main console shows real-time status for all components:

**Phone Client Status:**
- Face detection status (✓ Detected / ✗ Not Found)
- Head position and rotation data (when face detected)
- Blend shapes table with current values
- Service health indicator

**PC Client Status:**
- Face detection status (✓ Detected / ✗ Not Found)  
- Parameter prefix configuration
- Parameter table with current values and ranges
- Service health indicator

**Transformation Engine Status:**
- Rules loaded count (Valid/Invalid)
- Configuration file path and status
- "Up to Date" indicator (Yes/No)
- Failed rules table with error details

### Network Status Mode (F2)
Press **F2** to access comprehensive network diagnostics:

**Platform Information:**
- Operating system details
- Last updated timestamp

**iPhone Connection Analysis:**
- Local UDP port status (Allowed/Blocked)
- Outbound UDP connection status
- Default firewall action analysis
- Matching firewall rules table

**PC VTube Studio Connection Analysis:**
- WebSocket TCP connection status
- Discovery UDP connection status  
- Default firewall action analysis
- Matching firewall rules table

**Troubleshooting Commands:**
- Copy-paste firewall commands for iPhone UDP
- Copy-paste firewall commands for PC WebSocket
- Port checking commands
- Connectivity test commands

### System Help Mode (F1)
Press **F1** to access configuration details and shortcuts.

## Common Issues

### Connection Problems

#### iPhone Not Connecting
**What You'll See:**
- Phone Client shows "Disconnected" or "Not Found" status
- Face Status shows "✗ Not Found"
- No blend shapes data in the table

**Quick Diagnosis:**
1. **Check Network Status (F2)** - Look for "Blocked" indicators
2. **Check face detection** - Ensure iPhone camera can see your face
3. **Check VTube Studio app** - Ensure it's running and tracking

**Solutions:**
- **Firewall issues**: Use Network Status mode (F2) to get copy-paste firewall commands
- **Wrong IP address**: Check iPhone IP in System Help mode (F1), update `PhoneClient.IphoneIpAddress`
- **VTube Studio not running**: Start VTube Studio on iPhone first

#### PC Not Connecting  
**What You'll See:**
- PC Client shows "Disconnected" status
- Face Status shows "✗ Not Found"
- No parameter data in the table

**Quick Diagnosis:**
1. **Check Network Status (F2)** - Look for "Blocked" indicators
2. **Check VTube Studio PC** - Ensure it's running and API is enabled
3. **Check parameter prefix** - Verify it matches VTube Studio settings

**Solutions:**
- **Firewall issues**: Use Network Status mode (F2) to get copy-paste firewall commands
- **VTube Studio not running**: Start VTube Studio on PC first
- **API not enabled**: Enable API in VTube Studio settings
- **Permission denied**: Grant VTube Studio API access when prompted (see below)

#### VTube Studio API Permission Dialog
**What You'll See:**
- VTube Studio shows a security dialog asking to grant SharpBridge plugin access
- Dialog shows "SharpBridge" plugin by "Dimak@Shift" requesting API access
- Options: "Deny" (red) or "Allow" (blue)

**What to Do:**
1. **Click "Allow"** to grant SharpBridge access to VTube Studio API
2. **If you clicked "Deny"**: Restart Sharp Bridge to get the dialog again
3. **If dialog doesn't appear**: Check that VTube Studio is running and API is enabled

**Why This Happens:**
- VTube Studio requires explicit permission for plugins to access the API
- This is a one-time setup step for security
- You can revoke access later in VTube Studio settings if needed

#### Windows Firewall Permission Dialog
**What You'll See:**
- Windows Firewall shows a security dialog asking to allow SharpBridge network access
- Dialog shows "Windows Firewall has blocked some features of this app"
- Options: "Allow access" or "Cancel"

**What to Do:**
1. **Click "Allow access"** (recommended) - This allows both incoming and outgoing network connections
2. **If you clicked "Cancel"**: Use Network Status mode (F2) to get copy-paste firewall commands
3. **If dialog doesn't appear**: Check Windows Firewall settings or use in-app diagnostics

**Why This Happens:**
- Sharp Bridge uses both TCP (VTube Studio PC) and UDP (iPhone) connections
- Windows Firewall blocks network access by default for security
- This is a one-time setup step for network communication

### Configuration Issues

#### Transformation Rules Not Working
**What You'll See:**
- Transformation Engine shows "Invalid Rules" count > 0
- "Up to Date: No" indicator
- Failed rules table with error details
- Parameters not updating in VTube Studio

**Quick Diagnosis:**
1. **Check Failed Rules table** - Look for specific error messages
2. **Check "Up to Date" status** - If "No", configuration has issues
3. **Check parameter names** - Ensure they match VTube Studio exactly
4. **Check parameter prefix** - Verify it matches VTube Studio settings

**Solutions:**
- **Invalid JSON**: Fix syntax errors shown in Failed Rules table
- **Wrong parameter names**: Update parameter names to match VTube Studio
- **Expression errors**: Simplify mathematical expressions
- **Reload rules**: Press Alt+K to reload after fixing issues

#### Configuration Not Loading
**What You'll See:**
- Application fails to start with error messages
- "Up to Date: No" indicator in Transformation Engine
- Configuration file path shown in red or with errors

**Quick Diagnosis:**
1. **Check console error messages** - Look for specific file/JSON errors
2. **Check file permissions** - Ensure application can read files
3. **Check JSON syntax** - Use external editor to validate JSON
4. **Check file paths** - Verify all referenced files exist

**Solutions:**
- **Invalid JSON**: Fix syntax errors in configuration files
- **Missing files**: Let application recreate default configurations
- **File permissions**: Run application as administrator
- **Path issues**: Verify all file paths are correct


## Log Analysis

### Log File Location
Logs are stored in the `logs/` directory:
- **File pattern**: `sharp-bridge-.log`
- **Location**: Application base directory
- **Retention**: 31 files (one per day of usage)
- **Size limit**: 1MB per file

### Log Levels
- **Information**: Normal operation, configuration changes, service status
- **Warning**: Recoverable issues, port discovery failures, configuration warnings
- **Error**: Critical failures, connection errors, validation failures

### What Gets Logged
- **Service initialization** - Startup and shutdown events
- **Connection events** - iPhone and PC connection status
- **Configuration changes** - File modifications and hot reload
- **Transformation processing** - Rule loading and validation
- **Error conditions** - Failures and recovery attempts
- **Performance metrics** - Data rates and processing times

### Reading Logs
1. **Open log file** - Use text editor to view log contents
2. **Search for errors** - Look for "ERROR" or "WARN" entries
3. **Check timestamps** - Match log entries with when issues occurred
4. **Look for patterns** - Identify recurring error messages
5. **Check context** - Read surrounding log entries for context

## Recovery Procedures

### Application Won't Start
1. **Check prerequisites** - Verify application can run (self-contained executable)
2. **Check file permissions** - Ensure application directory is accessible
3. **Check configuration** - Verify ApplicationConfig.json is valid
4. **Check logs** - Review log files for startup errors
5. **Restart system** - Reboot if all else fails

### Configuration Corrupted
1. **Backup current files** - Save existing configuration files
2. **Delete configuration files** - Let application recreate defaults
3. **Restart application** - Launch Sharp Bridge to recreate configs
4. **Restore from backup** - Copy working configuration files back
5. **Test functionality** - Verify application works correctly

### Network Issues
1. **Check network connectivity** - Verify iPhone and PC can communicate
2. **Check firewall settings** - Use Network Status mode for diagnostics
3. **Restart network services** - Restart router or network adapters
4. **Check port availability** - Ensure required ports are not blocked
5. **Test with different devices** - Try different iPhone or PC if available

## Using Built-in Diagnostics

### Step-by-Step Troubleshooting
1. **Start with Main Status** - Look for red/yellow status indicators
2. **Check Network Status (F2)** - Look for "Blocked" firewall indicators
3. **Check System Help (F1)** - Verify configuration settings
4. **Check Failed Rules table** - Look for specific error messages
5. **Use copy-paste commands** - Apply firewall fixes from Network Status

### Quick Fixes
- **Firewall issues**: Use Network Status mode (F2) → Copy-paste firewall commands
- **Configuration errors**: Check Failed Rules table → Fix specific errors → Press Alt+K
- **Connection issues**: Check service status colors → Follow specific error guidance
- **Parameter issues**: Check parameter prefix → Verify parameter names match VTube Studio

### When to Check Logs
- **Application crashes** - Check logs for crash details
- **Persistent errors** - When console diagnostics don't show the issue
- **Performance problems** - Check logs for resource usage patterns
- **Configuration corruption** - When files keep getting corrupted

## Prevention

### Regular Maintenance
- **Backup configurations** - Keep copies of working configuration files
- **Monitor logs** - Check logs regularly for warning signs
- **Update software** - Keep VTube Studio updated
- **Clean up files** - Remove old log files and temporary files
- **Test regularly** - Verify application functionality periodically

### Best Practices
- **Use simple configurations** - Avoid overly complex transformation rules
- **Monitor performance** - Watch for resource usage issues
- **Keep backups** - Maintain copies of working configurations
- **Test changes** - Verify modifications work before relying on them
- **Document changes** - Keep notes on configuration modifications
