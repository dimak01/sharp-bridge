namespace SharpBridge.Models;

/// <summary>
/// Configuration for the VTube Studio Phone Client
/// </summary>
public class VTubeStudioPhoneClientConfig
{
    /// <summary>
    /// IP address of the iPhone
    /// </summary>
    public string IphoneIpAddress { get; set; } = "192.168.1.100";
    /// <summary>
    /// Port on the iPhone where VTube Studio is broadcasting
    /// </summary>
    public int IphonePort { get; set; } = 28964;
    
    /// <summary>
    /// Local port to receive tracking data on
    /// </summary>
    public int LocalPort { get; set; } = 21412;
    
    /// <summary>
    /// How often to request tracking data (in seconds)
    /// </summary>
    public double RequestIntervalSeconds { get; set; } = 1.0;
    
    /// <summary>
    /// How long to ask the iPhone to send data for (in seconds)
    /// </summary>
    public int SendForSeconds { get; set; } = 3;
    
    /// <summary>
    /// Timeout for receiving data (in milliseconds)
    /// </summary>
    public int ReceiveTimeoutMs { get; set; } = 100;
} 