namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the result of a configuration section remediation operation.
    /// </summary>
    public enum RemediationResult
    {
        /// <summary>
        /// Remediation failed due to user cancellation or an error.
        /// </summary>
        Failed,

        /// <summary>
        /// Remediation succeeded - the section was invalid and has been fixed.
        /// </summary>
        Succeeded,

        /// <summary>
        /// No remediation was needed - the section was already valid.
        /// </summary>
        NoRemediationNeeded
    }
}

