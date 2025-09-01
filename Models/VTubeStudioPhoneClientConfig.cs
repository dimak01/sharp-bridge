using System.ComponentModel;
using System.Text.Json.Serialization;
using SharpBridge.Interfaces;

namespace SharpBridge.Models;

/// <summary>
/// Configuration for the VTube Studio Phone Client
/// </summary>
public class VTubeStudioPhoneClientConfig : IConfigSection
{
    // ========================================
    // User-Configurable Settings
    // ========================================

    /// <summary>
    /// IP address of the iPhone (defaults to 127.0.0.1, null indicates user needs to set this)
    /// </summary>
    [Description("iPhone IP Address")]
    public string? IphoneIpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Port on the iPhone where VTube Studio is broadcasting
    /// </summary>
    [Description("iPhone Port")]
    public int IphonePort { get; set; } = 21412;

    /// <summary>
    /// Local port to receive tracking data on
    /// </summary>
    [Description("Local Port")]
    public int LocalPort { get; set; } = 28964;

    // ========================================
    // Internal Settings (Not User-Configurable)
    // ========================================

    /// <summary>
    /// How often to request tracking data (in seconds)
    /// </summary>
    [JsonIgnore]
    public double RequestIntervalSeconds { get; set; } = 3;

    /// <summary>
    /// How long to ask the iPhone to send data for (in seconds)
    /// </summary>
    [JsonIgnore]
    public int SendForSeconds { get; set; } = 4;

    /// <summary>
    /// Timeout for receiving data (in milliseconds)
    /// </summary>
    [JsonIgnore]
    public int ReceiveTimeoutMs { get; set; } = 100;

    /// <summary>
    /// The delay in milliseconds after an error in the main loop before retrying (default: 1000)
    /// </summary>
    [JsonIgnore]
    public int ErrorDelayMs { get; set; } = 1000;
}