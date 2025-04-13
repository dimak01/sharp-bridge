namespace SharpBridge.Models;

public class TrackingReceiverConfig
{
    // Communication settings
    public string IphoneIpAddress { get; init; } = string.Empty;
    public int IphonePort { get; init; } = 21412;

    // Performance settings
    public int ReceiveBufferSize { get; init; } = 1024;
    public int RequestIntervalSeconds { get; init; } = 10;
    public int ReceiveTimeoutMs { get; init; } = 100;
    public int PollTimeoutMs { get; init; } = 50;
} 