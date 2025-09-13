// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Utilities
{
    /// <summary>
    /// Utility class for creating deep copies of configuration objects
    /// </summary>
    public static class ConfigCloner
    {
        /// <summary>
        /// Creates a deep copy of GeneralSettingsConfig
        /// </summary>
        public static GeneralSettingsConfig Clone(GeneralSettingsConfig original)
        {
            if (original == null) return new GeneralSettingsConfig();

            return new GeneralSettingsConfig
            {
                EditorCommand = original.EditorCommand,
                Shortcuts = new Dictionary<string, string>(original.Shortcuts)
            };
        }

        /// <summary>
        /// Creates a deep copy of VTubeStudioPhoneClientConfig
        /// </summary>
        public static VTubeStudioPhoneClientConfig Clone(VTubeStudioPhoneClientConfig original)
        {
            if (original == null) return new VTubeStudioPhoneClientConfig("127.0.0.1");

            return new VTubeStudioPhoneClientConfig(original.IphoneIpAddress, original.IphonePort, original.LocalPort)
            {
                // Internal Settings
                RequestIntervalSeconds = original.RequestIntervalSeconds,
                SendForSeconds = original.SendForSeconds,
                ReceiveTimeoutMs = original.ReceiveTimeoutMs,
                ErrorDelayMs = original.ErrorDelayMs
            };
        }

        /// <summary>
        /// Creates a deep copy of VTubeStudioPCConfig
        /// </summary>
        public static VTubeStudioPCConfig Clone(VTubeStudioPCConfig original)
        {
            if (original == null) return new VTubeStudioPCConfig();

            return new VTubeStudioPCConfig
            {
                // User-Configurable Settings
                Host = original.Host,
                Port = original.Port,
                UsePortDiscovery = original.UsePortDiscovery,

                // Internal Settings
                PluginName = original.PluginName,
                PluginDeveloper = original.PluginDeveloper,
                TokenFilePath = original.TokenFilePath,
                ConnectionTimeoutMs = original.ConnectionTimeoutMs,
                ReconnectionDelayMs = original.ReconnectionDelayMs,
                RecoveryIntervalSeconds = original.RecoveryIntervalSeconds
            };
        }

        /// <summary>
        /// Creates a deep copy of TransformationEngineConfig
        /// </summary>
        public static TransformationEngineConfig Clone(TransformationEngineConfig original)
        {
            if (original == null) return new TransformationEngineConfig();

            return new TransformationEngineConfig
            {
                ConfigPath = original.ConfigPath,
                MaxEvaluationIterations = original.MaxEvaluationIterations
            };
        }

        /// <summary>
        /// Creates a deep copy of ApplicationConfig
        /// </summary>
        public static ApplicationConfig Clone(ApplicationConfig? original)
        {
            if (original == null) return new ApplicationConfig();

            return new ApplicationConfig
            {
                GeneralSettings = Clone(original.GeneralSettings),
                PhoneClient = Clone(original.PhoneClient),
                PCClient = Clone(original.PCClient),
                TransformationEngine = Clone(original.TransformationEngine)
            };
        }


    }
}
