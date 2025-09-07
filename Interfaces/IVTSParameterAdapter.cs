using System.Collections.Generic;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for adapting VTS parameters before sending to VTube Studio PC
    /// </summary>
    public interface IVTSParameterAdapter
    {
        /// <summary>
        /// Adapts a collection of VTS parameters by applying transformations (like prefixing)
        /// </summary>
        /// <param name="parameters">Original parameters</param>
        /// <returns>Adapted parameters ready for VTube Studio PC</returns>
        IEnumerable<VTSParameter> AdaptParameters(IEnumerable<VTSParameter> parameters);

        /// <summary>
        /// Adapts a collection of tracking parameters by applying transformations (like prefixing) to their IDs
        /// </summary>
        /// <param name="trackingParams">Original tracking parameters</param>
        /// <returns>Adapted tracking parameters ready for VTube Studio PC</returns>
        IEnumerable<TrackingParam> AdaptTrackingParameters(IEnumerable<TrackingParam> trackingParams);

        /// <summary>
        /// Adapts a single VTS parameter by applying transformations (like prefixing)
        /// </summary>
        /// <param name="parameter">Original parameter</param>
        /// <returns>Adapted parameter ready for VTube Studio PC</returns>
        VTSParameter AdaptParameter(VTSParameter parameter);

        /// <summary>
        /// Adapts a parameter name by applying transformations (like prefixing)
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        string AdaptParameterName(string parameterName);
    }
}
