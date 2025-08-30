namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service responsible for validating and remediating configuration issues.
    /// This service provides methods to validate configuration and run first-time setup
    /// when required fields are missing. Remediation must be called explicitly.
    /// </summary>
    public interface IConfigRemediationService
    {
        /// <summary>
        /// Performs configuration validation and remediation.
        /// This method must be called explicitly to trigger validation and setup.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration cannot be remediated after 3 attempts.</exception>
        void RemediateConfiguration();
    }
}
