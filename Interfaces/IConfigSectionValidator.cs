using System.Collections.Generic;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for validating configuration sections.
    /// Each configuration section type should have its own validator implementation.
    /// </summary>
    public interface IConfigSectionValidator
    {
        /// <summary>
        /// Validates a configuration section's fields and returns validation results.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>Validation result indicating if the section is valid and what fields need attention</returns>
        ConfigValidationResult ValidateSection(List<ConfigFieldState> fieldsState);
    }
}
