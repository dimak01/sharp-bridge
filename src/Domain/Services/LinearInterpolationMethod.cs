using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Services;

namespace SharpBridge.Domain.Services
{
    /// <summary>
    /// Linear interpolation method (y = x)
    /// </summary>
    public class LinearInterpolationMethod : IInterpolationMethod
    {
        /// <summary>
        /// Performs linear interpolation: y = x
        /// </summary>
        /// <param name="t">Normalized input value between 0 and 1</param>
        /// <returns>Same value as input (linear interpolation)</returns>
        public double Interpolate(double t)
        {
            return t;
        }

        /// <summary>
        /// Gets the display name for linear interpolation
        /// </summary>
        /// <returns>"Linear"</returns>
        public string GetDisplayName()
        {
            return "Linear";
        }
    }
}