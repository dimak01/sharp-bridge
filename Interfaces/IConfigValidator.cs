using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service for validating configuration completeness and identifying required setup fields
    /// </summary>
    public interface IConfigValidator
    {
        /// <summary>
        /// Validates the application configuration and identifies missing required fields
        /// </summary>
        /// <param name="config">The application configuration to validate</param>
        /// <returns>Validation result indicating which fields are missing</returns>
        ConfigValidationResult ValidateConfiguration(ApplicationConfig? config);
    }
}
