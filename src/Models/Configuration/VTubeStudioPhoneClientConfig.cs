using System.ComponentModel;
using System.Text.Json.Serialization;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration;

namespace SharpBridge.Models.Configuration;

/// <summary>
/// Configuration for the VTube Studio Phone Client
/// </summary>
public class VTubeStudioPhoneClientConfig : IConfigSection
{
    // ========================================
    // User-Configurable Settings
    // ========================================

    /// <summary>
    /// IP address of the iPhone
    /// </summary>
    [Description("iPhone IP Address")]
    public string IphoneIpAddress { get; set; }

    /// <summary>
    /// Port on the iPhone where VTube Studio is broadcasting
    /// </summary>
    [Description("iPhone VTube Studio App Port")]
    public int IphonePort { get; set; }

    /// <summary>
    /// Local port to receive tracking data on
    /// </summary>
    [Description("Local Port on this PC for iPhone's VTube Studio App to connect to our plugin")]
    public int LocalPort { get; set; }

    // ========================================
    // Internal Settings (Not User-Configurable)
    // ========================================

    /// <summary>
    /// How often to request tracking data (in seconds)
    /// </summary>
    [JsonIgnore]
    public double RequestIntervalSeconds { get; set; }

    /// <summary>
    /// How long to ask the iPhone to send data for (in seconds)
    /// </summary>
    [JsonIgnore]
    public int SendForSeconds { get; set; }

    /// <summary>
    /// Timeout for receiving data (in milliseconds)
    /// </summary>
    [JsonIgnore]
    public int ReceiveTimeoutMs { get; set; }

    /// <summary>
    /// The delay in milliseconds after an error in the main loop before retrying (default: 1000)
    /// </summary>
    [JsonIgnore]
    public int ErrorDelayMs { get; set; }

    /// <summary>
    /// Constructor to ensure all required fields are properly initialized
    /// </summary>
    /// <param name="iphoneIpAddress">IP address of the iPhone (required)</param>
    /// <param name="iphonePort">Port on the iPhone (default: 21412)</param>
    /// <param name="localPort">Local port to receive data (default: 28964)</param>
    public VTubeStudioPhoneClientConfig(string iphoneIpAddress = "", int iphonePort = 21412, int localPort = 28964)
    {
        IphoneIpAddress = iphoneIpAddress;
        IphonePort = iphonePort;
        LocalPort = localPort;

        // Set internal defaults
        RequestIntervalSeconds = 3;
        SendForSeconds = 4;
        ReceiveTimeoutMs = 250;
        ErrorDelayMs = 1000;
    }
}