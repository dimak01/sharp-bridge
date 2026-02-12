// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration.Services.Validators;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Services.Validators
{
    /// <summary>
    /// Validator for VTubeStudioPCConfig configuration sections.
    /// </summary>
    public class VTubeStudioPCConfigValidator : BaseConfigSectionValidator
    {
        /// <summary>
        /// Initializes a new instance of the VTubeStudioPCConfigValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        public VTubeStudioPCConfigValidator(IConfigFieldValidator fieldValidator)
            : base(fieldValidator)
        {
        }

        /// <summary>
        /// Gets the list of field names that should be ignored during validation.
        /// </summary>
        /// <returns>Array of field names to ignore</returns>
        protected override string[] GetIgnoredFields()
        {
            return new[] { "ConnectionTimeoutMs", "ReconnectionDelayMs", "RecoveryIntervalSeconds" };
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
                "Host" => FieldValidator.ValidateHost(field),
                "Port" => FieldValidator.ValidatePort(field),
                "UsePortDiscovery" => FieldValidator.ValidateBoolean(field),
                _ => CreateUnknownFieldError(field)
            };
        }

        /// <summary>
        /// Gets the configuration type name for error messages.
        /// </summary>
        /// <returns>The configuration type name</returns>
        protected override string GetConfigTypeName()
        {
            return "VTubeStudioPCConfig";
        }
    }
}
