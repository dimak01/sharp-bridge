using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for VTubeStudioPCConfig configuration sections.
    /// </summary>
    public class VTubeStudioPCConfigValidator : IConfigSectionValidator
    {
        /// <summary>
        /// Validates a VTubeStudioPCConfig section's fields and returns validation results.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>Validation result indicating if the section is valid and what fields need attention</returns>
        public ConfigValidationResult ValidateSection(List<ConfigFieldState> fieldsState)
        {
            // TODO: IMPLEMENT PROPER VTubeStudioPCConfig VALIDATION
            // This is currently a placeholder implementation. Need to implement:
            // 1. Field-specific validation (e.g., Host should not be empty, Port should be 1-65535)
            // 2. Business rule validation (e.g., API key format if required, connection timeout ranges)
            // 3. Cross-field validation (e.g., if using SSL, port should typically be 443 or custom)
            // 4. Proper error messages for each validation failure
            // 5. Handle optional vs required fields appropriately

            var missingFields = new List<FieldValidationIssue>();

            foreach (var field in fieldsState)
            {
                if (!field.IsPresent || field.Value == null)
                {
                    missingFields.Add(new FieldValidationIssue(field.FieldName, field.ExpectedType, field.Description));
                }
            }

            return new ConfigValidationResult(missingFields);
        }
    }
}
