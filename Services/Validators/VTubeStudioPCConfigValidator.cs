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
            var missingFields = new List<MissingField>();

            foreach (var field in fieldsState)
            {
                if (!field.IsPresent || field.Value == null)
                {
                    missingFields.Add(new MissingField(field.FieldName, field.ExpectedType, field.Description));
                }
            }

            return new ConfigValidationResult(missingFields);
        }
    }
}
