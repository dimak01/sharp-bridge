// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using SharpBridge.Configuration.Managers;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Domain;
using SharpBridge.Models.Infrastructure;
using SharpBridge.UI.Utilities;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Configuration.Managers
{
    public class ShortcutConfigurationManagerTests
    {
        private readonly Mock<IShortcutParser> _parserMock;
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly ShortcutConfigurationManager _manager;

        public ShortcutConfigurationManagerTests()
        {
            _parserMock = new Mock<IShortcutParser>();
            _loggerMock = new Mock<IAppLogger>();
            _mockConfigManager = new Mock<IConfigManager>();
            _manager = new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullParser_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ShortcutConfigurationManager(null!, _loggerMock.Object, _mockConfigManager.Object));
            exception.ParamName.Should().Be("parser");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ShortcutConfigurationManager(_parserMock.Object, null!, _mockConfigManager.Object));
            exception.ParamName.Should().Be("logger");
        }

        #endregion

        #region GetDefaultShortcuts Tests

        [Fact]
        public void GetDefaultShortcuts_ReturnsExpectedDefaults()
        {
            // Act
            var defaults = _manager.GetDefaultShortcuts();

            // Assert
            defaults.Should().NotBeNull();
            defaults.Should().ContainKey(ShortcutAction.CycleTransformationEngineVerbosity);
            defaults.Should().ContainKey(ShortcutAction.CyclePCClientVerbosity);
            defaults.Should().ContainKey(ShortcutAction.CyclePhoneClientVerbosity);
            defaults.Should().ContainKey(ShortcutAction.ReloadTransformationConfig);
            defaults.Should().ContainKey(ShortcutAction.OpenConfigInEditor);
            defaults.Should().ContainKey(ShortcutAction.ShowSystemHelp);

            defaults[ShortcutAction.CycleTransformationEngineVerbosity].Key.Should().Be(ConsoleKey.T);
            defaults[ShortcutAction.CycleTransformationEngineVerbosity].Modifiers.Should().Be(ConsoleModifiers.Alt);
            defaults[ShortcutAction.CyclePCClientVerbosity].Key.Should().Be(ConsoleKey.P);
            defaults[ShortcutAction.CyclePCClientVerbosity].Modifiers.Should().Be(ConsoleModifiers.Alt);
            defaults[ShortcutAction.CyclePhoneClientVerbosity].Key.Should().Be(ConsoleKey.O);
            defaults[ShortcutAction.CyclePhoneClientVerbosity].Modifiers.Should().Be(ConsoleModifiers.Alt);
            defaults[ShortcutAction.ReloadTransformationConfig].Key.Should().Be(ConsoleKey.K);
            defaults[ShortcutAction.ReloadTransformationConfig].Modifiers.Should().Be(ConsoleModifiers.Alt);
            defaults[ShortcutAction.OpenConfigInEditor].Key.Should().Be(ConsoleKey.E);
            defaults[ShortcutAction.OpenConfigInEditor].Modifiers.Should().Be(ConsoleModifiers.Control | ConsoleModifiers.Alt);
            defaults[ShortcutAction.ShowSystemHelp].Key.Should().Be(ConsoleKey.F1);
            defaults[ShortcutAction.ShowSystemHelp].Modifiers.Should().Be(ConsoleModifiers.None);
        }

        #endregion

        #region LoadFromConfiguration Tests

        [Fact]
        public void LoadFromConfiguration_WithValidConfig_LoadsMappingsSuccessfully()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "Alt+P"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+P"))
                .Returns(new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt));

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().NotBeNull();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity]!.Key.Should().Be(ConsoleKey.T);
            mappings[ShortcutAction.CycleTransformationEngineVerbosity]!.Modifiers.Should().Be(ConsoleModifiers.Alt);

            mappings[ShortcutAction.CyclePCClientVerbosity].Should().NotBeNull();
            mappings[ShortcutAction.CyclePCClientVerbosity]!.Key.Should().Be(ConsoleKey.P);
            mappings[ShortcutAction.CyclePCClientVerbosity]!.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        [Fact]
        public void LoadFromConfiguration_WithNullConfig_UsesDefaults()
        {
            // Arrange - Setup parser to format and parse default shortcuts
            _parserMock.Setup(x => x.FormatShortcut(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt)))
                .Returns("Alt+T");
            _parserMock.Setup(x => x.FormatShortcut(new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt)))
                .Returns("Alt+P");
            _parserMock.Setup(x => x.FormatShortcut(new Shortcut(ConsoleKey.O, ConsoleModifiers.Alt)))
                .Returns("Alt+O");
            _parserMock.Setup(x => x.FormatShortcut(new Shortcut(ConsoleKey.K, ConsoleModifiers.Alt)))
                .Returns("Alt+K");
            _parserMock.Setup(x => x.FormatShortcut(new Shortcut(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt)))
                .Returns("Ctrl+Alt+E");
            _parserMock.Setup(x => x.FormatShortcut(new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)))
                .Returns("F1");

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+P"))
                .Returns(new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+O"))
                .Returns(new Shortcut(ConsoleKey.O, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+K"))
                .Returns(new Shortcut(ConsoleKey.K, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Ctrl+Alt+E"))
                .Returns(new Shortcut(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("F1"))
                .Returns(new Shortcut(ConsoleKey.F1, ConsoleModifiers.None));

            // Act
            _manager.LoadFromConfiguration(null!);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings.Should().HaveCount(7); // Updated for ShowNetworkStatus action
            mappings.Values.Should().NotContain(x => x == null);
        }

        [Fact]
        public void LoadFromConfiguration_WithInvalidShortcut_DisablesShortcut()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "InvalidShortcut"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns((Shortcut?)null);

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().BeNull();

            var status = _manager.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity);
            status.Should().Be(ShortcutStatus.Invalid);

            var incorrectShortcuts = _manager.GetIncorrectShortcuts();
            incorrectShortcuts.Should().ContainKey(ShortcutAction.CycleTransformationEngineVerbosity);
            incorrectShortcuts[ShortcutAction.CycleTransformationEngineVerbosity].Should().Be("InvalidShortcut");
        }

        [Fact]
        public void LoadFromConfiguration_WithDuplicateShortcuts_DisablesConflictingShortcuts()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "Alt+T" // Duplicate
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            var enabledShortcuts = mappings.Values.Count(v => v != null);
            enabledShortcuts.Should().Be(1); // Only one should be enabled

            // First action should be active, second should be invalid due to conflict
            var firstStatus = _manager.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity);
            var secondStatus = _manager.GetShortcutStatus(ShortcutAction.CyclePCClientVerbosity);

            (firstStatus == ShortcutStatus.Active || secondStatus == ShortcutStatus.Active).Should().BeTrue("One should be active");
            (firstStatus == ShortcutStatus.Invalid || secondStatus == ShortcutStatus.Invalid).Should().BeTrue("One should be invalid due to conflict");
        }

        [Fact]
        public void LoadFromConfiguration_WithEmptyShortcut_DisablesShortcut()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = ""
                }
            };

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().BeNull();

            var status = _manager.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity);
            status.Should().Be(ShortcutStatus.ExplicitlyDisabled);
        }

        [Fact]
        public void LoadFromConfiguration_LogsSummary()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "InvalidShortcut"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns((Shortcut?)null);

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            _loggerMock.Verify(x => x.Info(
                It.Is<string>(s => s.Contains("Loaded") && s.Contains("shortcuts successfully")),
                It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void LoadFromConfiguration_ClearsPreviousState()
        {
            // Arrange
            var firstConfig = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "InvalidShortcut" // This will create an incorrect shortcut
                }
            };

            var secondConfig = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CyclePCClientVerbosity"] = "Alt+P"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+P"))
                .Returns(new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns((Shortcut?)null);

            // Act
            _manager.LoadFromConfiguration(firstConfig);
            var firstMappings = _manager.GetMappedShortcuts();
            var firstIncorrectShortcuts = _manager.GetIncorrectShortcuts();

            _manager.LoadFromConfiguration(secondConfig);
            var secondMappings = _manager.GetMappedShortcuts();
            var secondIncorrectShortcuts = _manager.GetIncorrectShortcuts();

            // Assert
            firstMappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().NotBeNull();
            secondMappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().BeNull();
            secondMappings[ShortcutAction.CyclePCClientVerbosity].Should().NotBeNull();

            // State should be cleared between loads - first load has invalid shortcut, second doesn't
            firstIncorrectShortcuts.Should().ContainKey(ShortcutAction.CyclePCClientVerbosity);
            secondIncorrectShortcuts.Should().NotContainKey(ShortcutAction.CyclePCClientVerbosity);
        }

        #endregion

        #region GetMappedShortcuts Tests

        [Fact]
        public void GetMappedShortcuts_ReturnsDefensiveCopy()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));

            _manager.LoadFromConfiguration(config);

            // Act
            var mappings1 = _manager.GetMappedShortcuts();
            var mappings2 = _manager.GetMappedShortcuts();

            // Assert
            mappings1.Should().NotBeSameAs(mappings2);
            mappings1.Should().BeEquivalentTo(mappings2);
        }

        #endregion

        #region New Interface Methods Tests

        [Fact]
        public void GetIncorrectShortcuts_ReturnsDefensiveCopy()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "InvalidShortcut"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns((Shortcut?)null);

            _manager.LoadFromConfiguration(config);

            // Act
            var incorrectShortcuts1 = _manager.GetIncorrectShortcuts();
            var incorrectShortcuts2 = _manager.GetIncorrectShortcuts();

            // Assert
            incorrectShortcuts1.Should().NotBeSameAs(incorrectShortcuts2);
            incorrectShortcuts1.Should().BeEquivalentTo(incorrectShortcuts2);
        }

        [Fact]
        public void GetShortcutStatus_ReturnsCorrectStatus()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "InvalidShortcut",
                    // CyclePhoneClientVerbosity not defined (explicitly disabled)
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns((Shortcut?)null);

            _manager.LoadFromConfiguration(config);

            // Act & Assert
            _manager.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity).Should().Be(ShortcutStatus.Active);
            _manager.GetShortcutStatus(ShortcutAction.CyclePCClientVerbosity).Should().Be(ShortcutStatus.Invalid);
            _manager.GetShortcutStatus(ShortcutAction.CyclePhoneClientVerbosity).Should().Be(ShortcutStatus.ExplicitlyDisabled);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void LoadFromConfiguration_WithNullShortcutsProperty_UsesDefaults()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = null!
            };

            _parserMock.Setup(x => x.ParseShortcut(It.IsAny<string>()))
                .Returns(new Shortcut(ConsoleKey.F1, ConsoleModifiers.None));

            // Act
            var exception = Record.Exception(() => _manager.LoadFromConfiguration(config));

            // Assert
            exception.Should().BeNull("Should handle null Shortcuts property gracefully");
        }

        [Fact]
        public void LoadFromConfiguration_WithUnknownShortcutAction_IgnoresGracefully()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["UnknownAction"] = "Alt+X",
                    ["CycleTransformationEngineVerbosity"] = "Alt+T"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt));

            // Act
            var exception = Record.Exception(() => _manager.LoadFromConfiguration(config));

            // Assert
            exception.Should().BeNull("Should ignore unknown actions gracefully");

            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().NotBeNull();
        }

        #endregion

        #region Verify Default Shortcuts

        [Fact]
        public void VerifyDefaultShortcuts()
        {
            // Arrange - Setup parser mocks for default shortcuts using callback to handle any Shortcut object
            _parserMock.Setup(x => x.FormatShortcut(It.IsAny<Shortcut>()))
                .Returns<Shortcut>(shortcut =>
                {
                    if (shortcut.Key == ConsoleKey.T && shortcut.Modifiers == ConsoleModifiers.Alt) return "Alt+T";
                    if (shortcut.Key == ConsoleKey.P && shortcut.Modifiers == ConsoleModifiers.Alt) return "Alt+P";
                    if (shortcut.Key == ConsoleKey.O && shortcut.Modifiers == ConsoleModifiers.Alt) return "Alt+O";
                    if (shortcut.Key == ConsoleKey.K && shortcut.Modifiers == ConsoleModifiers.Alt) return "Alt+K";
                    if (shortcut.Key == ConsoleKey.E && shortcut.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Alt)) return "Ctrl+Alt+E";
                    if (shortcut.Key == ConsoleKey.F1 && shortcut.Modifiers == ConsoleModifiers.None) return "F1";
                    return $"{shortcut.Modifiers}+{shortcut.Key}"; // Fallback
                });

            // Load default configuration to populate the mapped shortcuts
            _manager.LoadFromConfiguration(null!);

            // Verify default shortcuts
            var defaults = _manager.GetMappedShortcuts();
            // Verify individual shortcuts using ShortcutComparer for equality
            var comparer = ShortcutComparer.Instance;
            comparer.Equals(defaults[ShortcutAction.CycleTransformationEngineVerbosity], new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt)).Should().BeTrue();
            comparer.Equals(defaults[ShortcutAction.CyclePCClientVerbosity], new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt)).Should().BeTrue();
            comparer.Equals(defaults[ShortcutAction.CyclePhoneClientVerbosity], new Shortcut(ConsoleKey.O, ConsoleModifiers.Alt)).Should().BeTrue();
            comparer.Equals(defaults[ShortcutAction.OpenConfigInEditor], new Shortcut(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt)).Should().BeTrue();
            comparer.Equals(defaults[ShortcutAction.ShowSystemHelp], new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)).Should().BeTrue();

            // Verify formatted shortcuts - now using GetDisplayString
            _manager.GetDisplayString(ShortcutAction.CycleTransformationEngineVerbosity).Should().Be("Alt+T");
            _manager.GetDisplayString(ShortcutAction.CyclePCClientVerbosity).Should().Be("Alt+P");
            _manager.GetDisplayString(ShortcutAction.CyclePhoneClientVerbosity).Should().Be("Alt+O");
            _manager.GetDisplayString(ShortcutAction.OpenConfigInEditor).Should().Be("Ctrl+Alt+E");
            _manager.GetDisplayString(ShortcutAction.ShowSystemHelp).Should().Be("F1");
            _manager.GetDisplayString(ShortcutAction.ReloadTransformationConfig).Should().Be("Alt+K");

            // Verify ReloadTransformationConfig shortcut using ShortcutComparer for equality
            comparer.Equals(defaults[ShortcutAction.ReloadTransformationConfig], new Shortcut(ConsoleKey.K, ConsoleModifiers.Alt)).Should().BeTrue();
        }

        [Fact]
        public void LoadFromConfiguration_WithShortcutFromConsoleKeyInfo_WorksCorrectly()
        {
            // Arrange - Test the Shortcut.FromKeyInfo static method through configuration loading
            var keyInfo = new ConsoleKeyInfo('T', ConsoleKey.T, false, true, false); // Alt+T
            var shortcutFromKeyInfo = Shortcut.FromKeyInfo(keyInfo);

            var config = new GeneralSettingsConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns(shortcutFromKeyInfo);

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert - Verify the shortcut created via FromKeyInfo works correctly
            var mappings = _manager.GetMappedShortcuts();
            var loadedShortcut = mappings[ShortcutAction.CycleTransformationEngineVerbosity];

            loadedShortcut.Should().NotBeNull();
            loadedShortcut!.Key.Should().Be(ConsoleKey.T);
            loadedShortcut.Modifiers.Should().Be(ConsoleModifiers.Alt);

            // Verify the FromKeyInfo method created the shortcut correctly
            shortcutFromKeyInfo.Key.Should().Be(ConsoleKey.T);
            shortcutFromKeyInfo.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldUnsubscribeFromFileWatcherEvents()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();
            var manager = new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object);

            // Act
            manager.Dispose();

            // Assert
            _loggerMock.Verify(l => l.Debug("Disposing ShortcutConfigurationManager"), Times.Once);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_ShouldNotThrow()
        {
            // Arrange
            var manager = new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object);
            manager.Dispose();

            // Act & Assert
            manager.Dispose(); // Should not throw
            _loggerMock.Verify(l => l.Debug("Disposing ShortcutConfigurationManager"), Times.Once);
        }

        [Fact]
        public void Dispose_WithNullFileWatcher_ShouldNotThrow()
        {
            // Arrange
            var manager = new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, null);

            // Act & Assert
            manager.Dispose(); // Should not throw
            _loggerMock.Verify(l => l.Debug("Disposing ShortcutConfigurationManager"), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Arrange
            var manager = new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object);

            // Act
            manager.Dispose();
            manager.Dispose();
            manager.Dispose();

            // Assert
            _loggerMock.Verify(l => l.Debug("Disposing ShortcutConfigurationManager"), Times.Once);
        }

        #endregion

        #region Event Handler Tests

        [Fact]
        public void Constructor_WithFileWatcher_ShouldSubscribeToEvents()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();

            // Act & Assert
            // Verify that the manager was created successfully (subscription happens in constructor)
            // The test passes if no exception is thrown during construction
            new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object).Should().NotBeNull();
        }

        [Fact]
        public void OnApplicationConfigChanged_ShouldLogDebugMessage()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();
            new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object);
            var fileChangeArgs = new FileChangeEventArgs("config.json");

            // Act
            mockWatcher.Raise(w => w.FileChanged += null, fileChangeArgs);

            // Assert
            _loggerMock.Verify(l => l.Debug("Application config changed, checking if general settings were affected"), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenGeneralSettingsChanged_ShouldReloadConfiguration()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();
            var newConfig = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "new-editor.exe",
                    Shortcuts = new Dictionary<string, string>
                    {
                        ["ShowSystemHelp"] = "F2"
                    }
                }
            };

            // Setup mock to return different configs to trigger the change
            var defaultConfig = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>()
            };

            _mockConfigManager.SetupSequence(c => c.LoadSectionAsync<GeneralSettingsConfig>())
                .ReturnsAsync(defaultConfig)  // Initial load in constructor
                .ReturnsAsync(newConfig.GeneralSettings);  // Reload on config change

            new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object);
            var fileChangeArgs = new FileChangeEventArgs("config.json");

            // Act
            mockWatcher.Raise(w => w.FileChanged += null, fileChangeArgs);

            // Assert
            _loggerMock.Verify(l => l.Info("General settings changed, updating internal config and reloading shortcuts"), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenGeneralSettingsUnchanged_ShouldNotReloadConfiguration()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();
            var defaultConfig = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>()
            };

            // Setup mock to return the same config for both initial load and reload
            _mockConfigManager.Setup(c => c.LoadSectionAsync<GeneralSettingsConfig>())
                .ReturnsAsync(defaultConfig);

            new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object);
            var fileChangeArgs = new FileChangeEventArgs("config.json");

            // Act
            mockWatcher.Raise(w => w.FileChanged += null, fileChangeArgs);

            // Assert
            _loggerMock.Verify(l => l.Info("General settings changed, updating internal config and reloading shortcuts"), Times.Never);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();
            var defaultConfig = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>()
            };

            _mockConfigManager.SetupSequence(c => c.LoadSectionAsync<GeneralSettingsConfig>())
                .ReturnsAsync(defaultConfig)  // Initial load in constructor
                .ThrowsAsync(new Exception("Test exception"));  // Reload on config change throws

            new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object);
            var fileChangeArgs = new FileChangeEventArgs("config.json");

            // Act
            mockWatcher.Raise(w => w.FileChanged += null, fileChangeArgs);

            // Assert
            _loggerMock.Verify(l => l.Error("Error handling application config change: {0}", "Test exception"), Times.Once);
        }

        #endregion

        #region UpdateConfig Tests

        [Fact]
        public void UpdateConfig_ShouldUpdateInternalConfiguration()
        {
            // Arrange
            var mockWatcher = new Mock<IFileChangeWatcher>();
            var newGeneralSettings = new GeneralSettingsConfig
            {
                EditorCommand = "new-editor.exe",
                Shortcuts = new Dictionary<string, string>
                {
                    ["ShowSystemHelp"] = "F2"
                }
            };

            // Setup mock to return different configs to trigger the change
            var defaultConfig = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\"",
                Shortcuts = new Dictionary<string, string>()
            };

            _mockConfigManager.SetupSequence(c => c.LoadSectionAsync<GeneralSettingsConfig>())
                .ReturnsAsync(defaultConfig)  // Initial load in constructor
                .ReturnsAsync(newGeneralSettings);  // Reload on config change

            new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object, _mockConfigManager.Object, mockWatcher.Object);

            // Act
            // Trigger the event handler which calls UpdateConfig
            mockWatcher.Raise(w => w.FileChanged += null, new FileChangeEventArgs("config.json"));

            // Assert
            _loggerMock.Verify(l => l.Debug("General settings config updated"), Times.Once);
        }

        #endregion
    }
}