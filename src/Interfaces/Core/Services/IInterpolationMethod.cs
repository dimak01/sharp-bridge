namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Interface for interpolation methods that transform normalized input (0-1) to normalized output (0-1)
    /// </summary>
    public interface IInterpolationMethod
    {
        /// <summary>
        /// Interpolates a normalized input value (0-1) to a normalized output value (0-1)
        /// </summary>
        /// <param name="t">Normalized input value between 0 and 1</param>
        /// <returns>Normalized output value between 0 and 1</returns>
        double Interpolate(double t);

        /// <summary>
        /// Gets a human-readable display name for this interpolation method
        /// </summary>
        /// <returns>Display name for UI purposes</returns>
        string GetDisplayName();
    }
}