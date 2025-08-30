using System.Collections.Generic;
using System.Net;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Service for validating configuration completeness and identifying required setup fields
    /// </summary>
    public class ConfigValidator : IConfigValidator
    {
        /// <summary>
        /// Validates the application configuration and identifies missing required fields
        /// </summary>
        /// <param name="config">The application configuration to validate</param>
        /// <returns>Validation result indicating which fields are missing</returns>
        public ConfigValidationResult ValidateConfiguration(ApplicationConfig? config)
        {
            if (config == null)
            {
                // If config is null, all fields are missing
                return ConfigValidationResult.Invalid(
                    MissingField.PhoneIpAddress,
                    MissingField.PhonePort,
                    MissingField.PCHost,
                    MissingField.PCPort);
            }

            var missingFields = new List<MissingField>();

            // Validate phone configuration
            if (config.PhoneClient != null)
            {
                // Check if iPhone IP address is set (null means user needs to configure it)
                if (string.IsNullOrWhiteSpace(config.PhoneClient.IphoneIpAddress) || !IsValidIpAddress(config.PhoneClient.IphoneIpAddress))
                {
                    missingFields.Add(MissingField.PhoneIpAddress);
                }

                if (!IsValidPort(config.PhoneClient.IphonePort))
                {
                    missingFields.Add(MissingField.PhonePort);
                }
            }
            else
            {
                missingFields.Add(MissingField.PhoneIpAddress);
                missingFields.Add(MissingField.PhonePort);
            }

            // Validate PC configuration
            if (config.PCClient != null)
            {
                // Check if PC Host is set (null means user should configure it, but localhost is a reasonable default)
                if (string.IsNullOrWhiteSpace(config.PCClient.Host))
                {
                    missingFields.Add(MissingField.PCHost);
                }

                if (!IsValidPort(config.PCClient.Port))
                {
                    missingFields.Add(MissingField.PCPort);
                }
            }
            else
            {
                missingFields.Add(MissingField.PCHost);
                missingFields.Add(MissingField.PCPort);
            }

            return new ConfigValidationResult(missingFields);
        }

        /// <summary>
        /// Validates if the given string is a valid IP address
        /// </summary>
        /// <param name="ipAddress">IP address string to validate</param>
        /// <returns>True if valid IP address, false otherwise</returns>
        private static bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            // Check if it's a valid IP address format
            return IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// Validates if the given port number is valid
        /// </summary>
        /// <param name="port">Port number to validate</param>
        /// <returns>True if valid port, false otherwise</returns>
        private static bool IsValidPort(int port)
        {
            // Valid port range is 1-65535
            return port > 0 && port <= 65535;
        }
    }
}
