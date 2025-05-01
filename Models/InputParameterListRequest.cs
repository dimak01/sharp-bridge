using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Request to get the list of input parameters from VTube Studio
    /// </summary>
    public class InputParameterListRequest : VTSApiRequest<object>
    {
        /// <summary>
        /// Creates a new instance of InputParameterListRequest
        /// </summary>
        public InputParameterListRequest()
        {
            ApiName = "VTubeStudioPublicAPI";
            ApiVersion = "1.0";
            MessageType = "InputParameterListRequest";
        }
    }
} 