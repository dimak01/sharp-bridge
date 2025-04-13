namespace SharpBridge.Services;

public class TrackingReceiverConfig
{
    public int IPhonePort { get; init; } = 21412;
    public int ReceiveBufferSize { get; init; } = 1024;
    public int RequestIntervalSeconds { get; init; } = 10;
    public int ReceiveTimeoutMs { get; init; } = 100;
    public int PollTimeoutMs { get; init; } = 50;
} 