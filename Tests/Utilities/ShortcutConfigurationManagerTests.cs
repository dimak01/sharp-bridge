using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ShortcutConfigurationManagerTests
    {
        private readonly Mock<IShortcutParser> _parserMock;
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly ShortcutConfigurationManager _manager;

        public ShortcutConfigurationManagerTests()
        {
            _parserMock = new Mock<IShortcutParser>();
            _loggerMock = new Mock<IAppLogger>();
            _manager = new ShortcutConfigurationManager(_parserMock.Object, _loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullParser_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ShortcutConfigurationManager(null!, _loggerMock.Object));
            exception.ParamName.Should().Be("parser");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ShortcutConfigurationManager(_parserMock.Object, null!));
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

            defaults[ShortcutAction.CycleTransformationEngineVerbosity].Should().Be("Alt+T");
            defaults[ShortcutAction.CyclePCClientVerbosity].Should().Be("Alt+P");
            defaults[ShortcutAction.CyclePhoneClientVerbosity].Should().Be("Alt+O");
            defaults[ShortcutAction.ReloadTransformationConfig].Should().Be("Alt+K");
            defaults[ShortcutAction.OpenConfigInEditor].Should().Be("Ctrl+Alt+E");
            defaults[ShortcutAction.ShowSystemHelp].Should().Be("F1");
        }

        #endregion

        #region LoadFromConfiguration Tests

        [Fact]
        public void LoadFromConfiguration_WithValidConfig_LoadsMappingsSuccessfully()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "Alt+P"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+P"))
                .Returns((ConsoleKey.P, ConsoleModifiers.Alt));

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().NotBeNull();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity]!.Value.Key.Should().Be(ConsoleKey.T);
            mappings[ShortcutAction.CycleTransformationEngineVerbosity]!.Value.Modifiers.Should().Be(ConsoleModifiers.Alt);

            mappings[ShortcutAction.CyclePCClientVerbosity].Should().NotBeNull();
            mappings[ShortcutAction.CyclePCClientVerbosity]!.Value.Key.Should().Be(ConsoleKey.P);
            mappings[ShortcutAction.CyclePCClientVerbosity]!.Value.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        [Fact]
        public void LoadFromConfiguration_WithNullConfig_UsesDefaults()
        {
            // Arrange
            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+P"))
                .Returns((ConsoleKey.P, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+O"))
                .Returns((ConsoleKey.O, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+K"))
                .Returns((ConsoleKey.K, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Ctrl+Alt+E"))
                .Returns((ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("F1"))
                .Returns((ConsoleKey.F1, ConsoleModifiers.None));

            // Act
            _manager.LoadFromConfiguration(null!);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings.Should().HaveCount(6);
            mappings.Values.Should().NotContain(x => x == null);
        }

        [Fact]
        public void LoadFromConfiguration_WithInvalidShortcut_DisablesShortcut()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "InvalidShortcut"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns(((ConsoleKey, ConsoleModifiers)?)null);

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().BeNull();

            var issues = _manager.GetConfigurationIssues();
            issues.Should().Contain(issue => issue.Contains("Invalid shortcut format"));
        }

        [Fact]
        public void LoadFromConfiguration_WithDuplicateShortcuts_DisablesConflictingShortcuts()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "Alt+T" // Duplicate
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));

            // Act
            _manager.LoadFromConfiguration(config);

            // Assert
            var mappings = _manager.GetMappedShortcuts();
            var enabledShortcuts = mappings.Values.Count(v => v != null);
            enabledShortcuts.Should().Be(1); // Only one should be enabled

            var issues = _manager.GetConfigurationIssues();
            issues.Should().Contain(issue => issue.Contains("conflicts with"));
        }

        [Fact]
        public void LoadFromConfiguration_WithEmptyShortcut_DisablesShortcut()
        {
            // Arrange
            var config = new ApplicationConfig
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

            var issues = _manager.GetConfigurationIssues();
            issues.Should().Contain(issue => issue.Contains("No shortcut defined"));
        }

        [Fact]
        public void LoadFromConfiguration_LogsSummary()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T",
                    ["CyclePCClientVerbosity"] = "InvalidShortcut"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns(((ConsoleKey, ConsoleModifiers)?)null);

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
            var firstConfig = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T"
                }
            };

            var secondConfig = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CyclePCClientVerbosity"] = "Alt+P"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));
            _parserMock.Setup(x => x.ParseShortcut("Alt+P"))
                .Returns((ConsoleKey.P, ConsoleModifiers.Alt));

            // Act
            _manager.LoadFromConfiguration(firstConfig);
            var firstMappings = _manager.GetMappedShortcuts();
            var firstIssues = _manager.GetConfigurationIssues();

            _manager.LoadFromConfiguration(secondConfig);
            var secondMappings = _manager.GetMappedShortcuts();
            var secondIssues = _manager.GetConfigurationIssues();

            // Assert
            firstMappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().NotBeNull();
            secondMappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().BeNull();
            secondMappings[ShortcutAction.CyclePCClientVerbosity].Should().NotBeNull();

            // Issues should be cleared between loads
            secondIssues.Should().NotBeEquivalentTo(firstIssues);
        }

        #endregion

        #region GetMappedShortcuts Tests

        [Fact]
        public void GetMappedShortcuts_ReturnsDefensiveCopy()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "Alt+T"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));

            _manager.LoadFromConfiguration(config);

            // Act
            var mappings1 = _manager.GetMappedShortcuts();
            var mappings2 = _manager.GetMappedShortcuts();

            // Assert
            mappings1.Should().NotBeSameAs(mappings2);
            mappings1.Should().BeEquivalentTo(mappings2);
        }

        #endregion

        #region GetConfigurationIssues Tests

        [Fact]
        public void GetConfigurationIssues_ReturnsDefensiveCopy()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["CycleTransformationEngineVerbosity"] = "InvalidShortcut"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("InvalidShortcut"))
                .Returns(((ConsoleKey, ConsoleModifiers)?)null);

            _manager.LoadFromConfiguration(config);

            // Act
            var issues1 = _manager.GetConfigurationIssues();
            var issues2 = _manager.GetConfigurationIssues();

            // Assert
            issues1.Should().NotBeSameAs(issues2);
            issues1.Should().BeEquivalentTo(issues2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void LoadFromConfiguration_WithNullShortcutsProperty_UsesDefaults()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = null!
            };

            _parserMock.Setup(x => x.ParseShortcut(It.IsAny<string>()))
                .Returns((ConsoleKey.F1, ConsoleModifiers.None));

            // Act
            var exception = Record.Exception(() => _manager.LoadFromConfiguration(config));

            // Assert
            exception.Should().BeNull("Should handle null Shortcuts property gracefully");
        }

        [Fact]
        public void LoadFromConfiguration_WithUnknownShortcutAction_IgnoresGracefully()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                Shortcuts = new Dictionary<string, string>
                {
                    ["UnknownAction"] = "Alt+X",
                    ["CycleTransformationEngineVerbosity"] = "Alt+T"
                }
            };

            _parserMock.Setup(x => x.ParseShortcut("Alt+T"))
                .Returns((ConsoleKey.T, ConsoleModifiers.Alt));

            // Act
            var exception = Record.Exception(() => _manager.LoadFromConfiguration(config));

            // Assert
            exception.Should().BeNull("Should ignore unknown actions gracefully");

            var mappings = _manager.GetMappedShortcuts();
            mappings[ShortcutAction.CycleTransformationEngineVerbosity].Should().NotBeNull();
        }

        #endregion
    }
}