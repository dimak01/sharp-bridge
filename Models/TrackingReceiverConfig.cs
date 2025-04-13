namespace SharpBridge.Models;

public class TrackingReceiverConfig
{
    // Communication settings
    public string IphoneIpAddress { get; init; } = string.Empty;
    public int IphonePort { get; init; } = 21412;
    
    // Default to port 21413 as our local listening port (next to iPhone's 21412)
    // This allows us to use a consistent port for firewall rules
    public int LocalPort { get; init; } = 21413;

    // Performance settings
    public int ReceiveBufferSize { get; init; } = 1024;
    public int RequestIntervalSeconds { get; init; } = 10;
    public int ReceiveTimeoutMs { get; init; } = 100;
    public int PollTimeoutMs { get; init; } = 50;
} 