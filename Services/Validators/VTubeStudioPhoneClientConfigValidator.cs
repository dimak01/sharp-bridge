using System;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigValidator : BaseConfigSectionValidator
    {
        /// <summary>
        /// Initializes a new instance of the VTubeStudioPhoneClientConfigValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        public VTubeStudioPhoneClientConfigValidator(IConfigFieldValidator fieldValidator)
            : base(fieldValidator)
        {
        }

        /// <summary>
        /// Gets the list of field names that should be ignored during validation.
        /// </summary>
        /// <returns>Array of field names to ignore</returns>
        protected override string[] GetIgnoredFields()
        {
            return new[] { "RequestIntervalSeconds", "SendForSeconds", "ReceiveTimeoutMs", "ErrorDelayMs" };
        }

        /// <summary>
        /// Validates a specific field's value according to business rules.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        protected override FieldValidationIssue? ValidateFieldValue(ConfigFieldState field)
        {
            return field.FieldName switch
            {
                "IphoneIpAddress" => FieldValidator.ValidateIpAddress(field),
                "IphonePort" => FieldValidator.ValidatePort(field),
                "LocalPort" => FieldValidator.ValidatePort(field),
                _ => CreateUnknownFieldError(field)
            };
        }

        /// <summary>
        /// Gets the configuration type name for error messages.
        /// </summary>
        /// <returns>The configuration type name</returns>
        protected override string GetConfigTypeName()
        {
            return "VTubeStudioPhoneClientConfig";
        }
    }
}
