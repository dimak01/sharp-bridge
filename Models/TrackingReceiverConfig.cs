namespace SharpBridge.Models;

public class TrackingReceiverConfig
{
    // Communication settings
    public string IphoneIpAddress { get; init; } = string.Empty;
    public int IphonePort { get; init; } = 21412;
    
    // Default to port 21413 as our local listening port (next to iPhone's 21412)
    // This allows us to use a consistent port for firewall rules
    public int LocalPort { get; init; } = 21413;

    // Performance settings - matched to Rust implementation
    public int ReceiveBufferSize { get; init; } = 4096;  // Match Rust's 4096 byte buffer
    public int RequestIntervalSeconds { get; init; } = 1; // Match Rust's 1 second request interval
    public int SendForSeconds { get; init; } = 10;       // Match Rust's 10 second data request duration
    public int ReceiveTimeoutMs { get; init; } = 2000;   // Match Rust's 2 second receive timeout
    public int PollTimeoutMs { get; init; } = 50;        // Keep for compatibility
} 