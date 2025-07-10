using System.Collections.Generic;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConfigComparersTests
    {
        #region PhoneClientConfigsEqual Tests

        [Fact]
        public void PhoneClientConfigsEqual_WithNullBoth_ReturnsTrue()
        {
            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(null, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithNullFirst_ReturnsFalse()
        {
            // Arrange
            var config = new VTubeStudioPhoneClientConfig();

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(null, config);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithNullSecond_ReturnsFalse()
        {
            // Arrange
            var config = new VTubeStudioPhoneClientConfig();

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var config = new VTubeStudioPhoneClientConfig();

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config, config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithIdenticalConfigs_ReturnsTrue()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964,
                RequestIntervalSeconds = 3.0,
                SendForSeconds = 4,
                ReceiveTimeoutMs = 100,
                ErrorDelayMs = 1000
            };

            var config2 = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964,
                RequestIntervalSeconds = 3.0,
                SendForSeconds = 4,
                ReceiveTimeoutMs = 100,
                ErrorDelayMs = 1000
            };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentIphoneIpAddress_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "192.168.1.100" };
            var config2 = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "192.168.1.101" };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentIphonePort_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { IphonePort = 21412 };
            var config2 = new VTubeStudioPhoneClientConfig { IphonePort = 21413 };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentLocalPort_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { LocalPort = 28964 };
            var config2 = new VTubeStudioPhoneClientConfig { LocalPort = 28965 };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentRequestIntervalSeconds_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { RequestIntervalSeconds = 3.0 };
            var config2 = new VTubeStudioPhoneClientConfig { RequestIntervalSeconds = 4.0 };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentSendForSeconds_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { SendForSeconds = 4 };
            var config2 = new VTubeStudioPhoneClientConfig { SendForSeconds = 5 };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentReceiveTimeoutMs_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { ReceiveTimeoutMs = 100 };
            var config2 = new VTubeStudioPhoneClientConfig { ReceiveTimeoutMs = 200 };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PhoneClientConfigsEqual_WithDifferentErrorDelayMs_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPhoneClientConfig { ErrorDelayMs = 1000 };
            var config2 = new VTubeStudioPhoneClientConfig { ErrorDelayMs = 2000 };

            // Act
            var result = ConfigComparers.PhoneClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region PCClientConfigsEqual Tests

        [Fact]
        public void PCClientConfigsEqual_WithNullBoth_ReturnsTrue()
        {
            // Act
            var result = ConfigComparers.PCClientConfigsEqual(null, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithNullFirst_ReturnsFalse()
        {
            // Arrange
            var config = new VTubeStudioPCConfig();

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(null, config);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithNullSecond_ReturnsFalse()
        {
            // Arrange
            var config = new VTubeStudioPCConfig();

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var config = new VTubeStudioPCConfig();

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config, config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithIdenticalConfigs_ReturnsTrue()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "SharpBridge",
                PluginDeveloper = "Dimak@Shift",
                TokenFilePath = "auth_token.txt",
                ConnectionTimeoutMs = 5000,
                ReconnectionDelayMs = 2000,
                UsePortDiscovery = true
            };

            var config2 = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "SharpBridge",
                PluginDeveloper = "Dimak@Shift",
                TokenFilePath = "auth_token.txt",
                ConnectionTimeoutMs = 5000,
                ReconnectionDelayMs = 2000,
                UsePortDiscovery = true
            };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentHost_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { Host = "localhost" };
            var config2 = new VTubeStudioPCConfig { Host = "127.0.0.1" };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentPort_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { Port = 8001 };
            var config2 = new VTubeStudioPCConfig { Port = 8002 };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentPluginName_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { PluginName = "SharpBridge" };
            var config2 = new VTubeStudioPCConfig { PluginName = "OtherPlugin" };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentPluginDeveloper_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { PluginDeveloper = "Dimak@Shift" };
            var config2 = new VTubeStudioPCConfig { PluginDeveloper = "OtherDeveloper" };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentTokenFilePath_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { TokenFilePath = "auth_token.txt" };
            var config2 = new VTubeStudioPCConfig { TokenFilePath = "other_token.txt" };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentConnectionTimeoutMs_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { ConnectionTimeoutMs = 5000 };
            var config2 = new VTubeStudioPCConfig { ConnectionTimeoutMs = 10000 };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentReconnectionDelayMs_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { ReconnectionDelayMs = 2000 };
            var config2 = new VTubeStudioPCConfig { ReconnectionDelayMs = 3000 };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PCClientConfigsEqual_WithDifferentUsePortDiscovery_ReturnsFalse()
        {
            // Arrange
            var config1 = new VTubeStudioPCConfig { UsePortDiscovery = true };
            var config2 = new VTubeStudioPCConfig { UsePortDiscovery = false };

            // Act
            var result = ConfigComparers.PCClientConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GeneralSettingsEqual Tests

        [Fact]
        public void GeneralSettingsEqual_WithNullBoth_ReturnsTrue()
        {
            // Act
            var result = ConfigComparers.GeneralSettingsEqual(null, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithNullFirst_ReturnsFalse()
        {
            // Arrange
            var config = new GeneralSettingsConfig();

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(null, config);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithNullSecond_ReturnsFalse()
        {
            // Arrange
            var config = new GeneralSettingsConfig();

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var config = new GeneralSettingsConfig();

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config, config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithIdenticalConfigs_ReturnsTrue()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Alt+E" },
                    { "SaveConfig", "Ctrl+S" }
                }
            };

            var config2 = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Alt+E" },
                    { "SaveConfig", "Ctrl+S" }
                }
            };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithDifferentEditorCommand_ReturnsFalse()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig { EditorCommand = "notepad.exe \"%f\"" };
            var config2 = new GeneralSettingsConfig { EditorCommand = "code.exe \"%f\"" };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithNullShortcutsBoth_ReturnsTrue()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig { Shortcuts = null! };
            var config2 = new GeneralSettingsConfig { Shortcuts = null! };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithNullShortcutsFirst_ReturnsFalse()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig { Shortcuts = null! };
            var config2 = new GeneralSettingsConfig { Shortcuts = new Dictionary<string, string>() };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithNullShortcutsSecond_ReturnsFalse()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig { Shortcuts = new Dictionary<string, string>() };
            var config2 = new GeneralSettingsConfig { Shortcuts = null! };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithDifferentShortcutCounts_ReturnsFalse()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Alt+E" }
                }
            };

            var config2 = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Alt+E" },
                    { "SaveConfig", "Ctrl+S" }
                }
            };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithDifferentShortcutKeys_ReturnsFalse()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Alt+E" }
                }
            };

            var config2 = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    { "SaveConfig", "Ctrl+S" }
                }
            };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithDifferentShortcutValues_ReturnsFalse()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Alt+E" }
                }
            };

            var config2 = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    { "OpenEditor", "Ctrl+E" }
                }
            };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GeneralSettingsEqual_WithEmptyShortcuts_ReturnsTrue()
        {
            // Arrange
            var config1 = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>()
            };

            var config2 = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>()
            };

            // Act
            var result = ConfigComparers.GeneralSettingsEqual(config1, config2);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region TransformationEngineConfigsEqual Tests

        [Fact]
        public void TransformationEngineConfigsEqual_WithNullBoth_ReturnsTrue()
        {
            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(null, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TransformationEngineConfigsEqual_WithNullFirst_ReturnsFalse()
        {
            // Arrange
            var config = new TransformationEngineConfig();

            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(null, config);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransformationEngineConfigsEqual_WithNullSecond_ReturnsFalse()
        {
            // Arrange
            var config = new TransformationEngineConfig();

            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(config, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransformationEngineConfigsEqual_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var config = new TransformationEngineConfig();

            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(config, config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TransformationEngineConfigsEqual_WithIdenticalConfigs_ReturnsTrue()
        {
            // Arrange
            var config1 = new TransformationEngineConfig
            {
                ConfigPath = "Configs/vts_transforms.json",
                MaxEvaluationIterations = 10
            };

            var config2 = new TransformationEngineConfig
            {
                ConfigPath = "Configs/vts_transforms.json",
                MaxEvaluationIterations = 10
            };

            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(config1, config2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TransformationEngineConfigsEqual_WithDifferentConfigPath_ReturnsFalse()
        {
            // Arrange
            var config1 = new TransformationEngineConfig { ConfigPath = "Configs/vts_transforms.json" };
            var config2 = new TransformationEngineConfig { ConfigPath = "Configs/other_transforms.json" };

            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransformationEngineConfigsEqual_WithDifferentMaxEvaluationIterations_ReturnsFalse()
        {
            // Arrange
            var config1 = new TransformationEngineConfig { MaxEvaluationIterations = 10 };
            var config2 = new TransformationEngineConfig { MaxEvaluationIterations = 20 };

            // Act
            var result = ConfigComparers.TransformationEngineConfigsEqual(config1, config2);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}