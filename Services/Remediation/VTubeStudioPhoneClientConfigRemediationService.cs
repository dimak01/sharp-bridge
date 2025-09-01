using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Remediation
{
    /// <summary>
    /// Remediation service for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigRemediationService : IConfigSectionRemediationService
    {
        private const int MAX_REMEDIATION_RETRIES = 3;

        private readonly IConfigSectionValidator _validator;

        /// <summary>
        /// Initializes a new instance of the VTubeStudioPhoneClientConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        public VTubeStudioPhoneClientConfigRemediationService(IConfigSectionValidatorsFactory validatorsFactory)
        {
            _validator = validatorsFactory.GetValidator(ConfigSectionTypes.VTubeStudioPhoneClientConfig);
        }

        /// <summary>
        /// Remediates configuration issues for a VTubeStudioPhoneClientConfig section by fixing missing or invalid fields.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>A tuple indicating success and the updated configuration section</returns>
        public async Task<(bool Success, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState)
        {

            var workingFields = new List<ConfigFieldState>(fieldsState);

            var validation = _validator.ValidateSection(workingFields);
            if (validation.IsValid)
            {
                var initialConfig = CreateConfigFromFieldStates(workingFields);
                return (true, initialConfig);
            }

            for (int attempt = 1; attempt <= MAX_REMEDIATION_RETRIES; attempt++)
            {
                var (success, updatedFields) = await TryRemediateOnceAsync(validation.Issues, workingFields);

                if (success && updatedFields != null)
                {
                    workingFields = updatedFields;
                }

                // Re-validate after remediation attempt
                validation = _validator.ValidateSection(workingFields);
                if (validation.IsValid)
                {
                    var config = CreateConfigFromFieldStates(workingFields);
                    return (true, config);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Performs one remediation attempt given the current missing/invalid fields and the working field state.
        /// </summary>
        /// <param name="missingFields">List of missing/invalid fields to remediate</param>
        /// <param name="currentFieldsState">Current working snapshot of fields</param>
        /// <returns>Whether remediation step succeeded and updated field state</returns>
        private static async Task<(bool Success, List<ConfigFieldState> UpdatedFields)> TryRemediateOnceAsync(
            List<FieldValidationIssue> missingFields,
            List<ConfigFieldState> currentFieldsState)
        {
            // TODO: Implement interactive/iterative remediation logic (prompt user, validate inputs, update fields)
            // For now, return unchanged fields to continue the loop until retries are exhausted
            await Task.CompletedTask;
            return (false, currentFieldsState);
        }

        /// <summary>
        /// Creates a VTubeStudioPhoneClientConfig from the field states.
        /// </summary>
        private static VTubeStudioPhoneClientConfig CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
        {
            var config = new VTubeStudioPhoneClientConfig();

            foreach (var field in fieldsState)
            {
                if (field.IsPresent && field.Value != null)
                {
                    switch (field.FieldName)
                    {
                        case "IphoneIpAddress" when field.Value is string ipAddress:
                            config.IphoneIpAddress = ipAddress;
                            break;
                        case "IphonePort" when field.Value is int port:
                            config.IphonePort = port;
                            break;
                        case "LocalPort" when field.Value is int localPort:
                            config.LocalPort = localPort;
                            break;
                    }
                }
            }

            return config;
        }
    }
}
