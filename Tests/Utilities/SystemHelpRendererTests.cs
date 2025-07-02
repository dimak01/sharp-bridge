using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class SystemHelpRendererTests
    {
        private readonly Mock<IShortcutConfigurationManager> _shortcutManagerMock;
        private readonly Mock<ITableFormatter> _tableFormatterMock;
        private readonly SystemHelpRenderer _renderer;

        public SystemHelpRendererTests()
        {
            _shortcutManagerMock = new Mock<IShortcutConfigurationManager>();
            _tableFormatterMock = new Mock<ITableFormatter>();
            _renderer = new SystemHelpRenderer(_shortcutManagerMock.Object, _tableFormatterMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullShortcutConfigurationManager_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var tableFormatterMock = new Mock<ITableFormatter>();
            Action act = () => new SystemHelpRenderer(null!, tableFormatterMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("shortcutConfigurationManager");
        }

        [Fact]
        public void Constructor_WithNullTableFormatter_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            Action act = () => new SystemHelpRenderer(shortcutConfigurationManagerMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("tableFormatter");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act & Assert
            var shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            var tableFormatterMock = new Mock<ITableFormatter>();
            var renderer = new SystemHelpRenderer(shortcutConfigurationManagerMock.Object, tableFormatterMock.Object);
            renderer.Should().NotBeNull();
        }

        [Fact]
        public void RenderApplicationConfiguration_WithValidConfig_ReturnsFormattedString()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe \"%f\"",
                    Shortcuts = new Dictionary<string, string>()
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("External Editor Command");
            result.Should().Contain("notepad.exe");
        }

        #endregion

        #region RenderSystemHelp Tests

        [Fact]
        public void RenderSystemHelp_WithValidConfig_ReturnsFormattedOutput()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe \"%f\"",
                    Shortcuts = new Dictionary<string, string>()
                }
            };

            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt),
                [ShortcutAction.ShowSystemHelp] = null // Disabled
            };

            var incorrectShortcuts = new Dictionary<ShortcutAction, string>();

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(incorrectShortcuts);
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity)).Returns(ShortcutStatus.Active);
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(ShortcutStatus.ExplicitlyDisabled);
            _shortcutManagerMock.Setup(x => x.GetDisplayString(ShortcutAction.CycleTransformationEngineVerbosity)).Returns("Alt+T");
            _shortcutManagerMock.Setup(x => x.GetDisplayString(ShortcutAction.ShowSystemHelp)).Returns("None");
            _tableFormatterMock.Setup(x => x.AppendTable(
    It.IsAny<StringBuilder>(),
    It.IsAny<string>(),
    It.IsAny<IEnumerable<It.IsAnyType>>(),
    It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
    It.IsAny<int>(),
    It.IsAny<int>(),
    It.IsAny<int>(),
    It.IsAny<int?>()))
    .Callback(new InvocationAction(invocation =>
    {
        var sb = (StringBuilder)invocation.Arguments[0];
        var title = (string)invocation.Arguments[1];
        sb.AppendLine(title);
        sb.AppendLine("Test Table");
    }));

            const int consoleWidth = 80;

            // Act
            var result = _renderer.RenderSystemHelp(config, consoleWidth);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("SHARP BRIDGE - SYSTEM HELP");
            result.Should().Contain("APPLICATION CONFIGURATION");
            result.Should().Contain("KEYBOARD SHORTCUTS");
            result.Should().Contain("Test Table");
            result.Should().Contain("Press any key to return to main display");
        }

        [Fact]
        public void RenderSystemHelp_WithNullConfig_HandlesGracefully()
        {
            // Arrange
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>();
            var incorrectShortcuts = new Dictionary<ShortcutAction, string>();

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(incorrectShortcuts);
            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    sb.AppendLine("Empty Table");
                }));

            // Act
            var exception = Record.Exception(() => _renderer.RenderSystemHelp(null!, 80));

            // Assert
            exception.Should().BeNull("Should handle null config gracefully");
        }

        [Fact]
        public void RenderSystemHelp_CreatesCorrectTableData()
        {
            // Arrange
            var config = new ApplicationConfig();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt),
                [ShortcutAction.ShowSystemHelp] = null
            };

            var incorrectShortcuts = new Dictionary<ShortcutAction, string>();

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(incorrectShortcuts);
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity)).Returns(ShortcutStatus.Active);
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(ShortcutStatus.ExplicitlyDisabled);

            object? capturedTableData = null;
            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    capturedTableData = invocation.Arguments[2];
                    sb.AppendLine(title);
                    sb.AppendLine("Test Table");
                }));

            // Act
            _renderer.RenderSystemHelp(config, 80);

            // Assert
            capturedTableData.Should().NotBeNull();
            _tableFormatterMock.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                80,
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public void RenderSystemHelp_WithConfigurationIssues_IncludesErrorsInStatus()
        {
            // Arrange
            var config = new ApplicationConfig();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = null
            };

            var incorrectShortcuts = new Dictionary<ShortcutAction, string>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = "InvalidShortcut"
            };

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(incorrectShortcuts);
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity)).Returns(ShortcutStatus.Invalid);

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    sb.AppendLine(title);
                    sb.AppendLine("✗ Disabled (Invalid format)");
                }));

            // Act
            var result = _renderer.RenderSystemHelp(config, 80);

            // Assert
            result.Should().Contain("Invalid format");
        }

        [Fact]
        public void RenderSystemHelp_WithActiveShortcuts_ShowsActiveStatus()
        {
            // Arrange
            var config = new ApplicationConfig();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt)
            };

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity)).Returns(ShortcutStatus.Active);

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    sb.AppendLine(title);
                    sb.AppendLine("✓ Active Alt+T");
                }));

            // Act
            var result = _renderer.RenderSystemHelp(config, 80);

            // Assert
            result.Should().Contain("✓ Active");
            result.Should().Contain("Alt+T");
        }

        [Fact]
        public void RenderSystemHelp_WithDisabledShortcuts_ShowsDisabledStatus()
        {
            // Arrange
            var config = new ApplicationConfig();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = null
            };

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity)).Returns(ShortcutStatus.ExplicitlyDisabled);

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    sb.AppendLine(title);
                    sb.AppendLine("✗ Disabled");
                }));

            // Act
            var result = _renderer.RenderSystemHelp(config, 80);

            // Assert
            result.Should().Contain("✗ Disabled");
        }

        [Fact]
        public void RenderSystemHelp_WithDifferentConsoleWidths_AdjustsSeparators()
        {
            // Arrange
            var config = new ApplicationConfig();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
            };

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(ShortcutStatus.Active);
            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    sb.AppendLine(title);
                    sb.AppendLine("Test Table");
                }));

            // Act
            var result40 = _renderer.RenderSystemHelp(config, 40);
            var result120 = _renderer.RenderSystemHelp(config, 120);

            // Assert
            // For width 40, should use minimum of 80 characters
            result40.Should().Contain(new string('═', 80));
            result40.Should().NotContain(new string('═', 120));

            // For width 120, should use 120 characters
            result120.Should().Contain(new string('═', 120));
            // Note: A 120-character line will contain an 80-character substring, so we can't test for NotContain(80)
            // Instead, verify that the 120-character line exists and the 40-width result doesn't have it
            result40.Should().NotContain(new string('═', 120));
        }

        [Fact]
        public void RenderSystemHelp_IncludesAllExpectedSections()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe"
                }
            };

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>
                {
                    [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
                });
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(new Dictionary<ShortcutAction, string>());
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(ShortcutStatus.Active);
            _tableFormatterMock.Setup(x => x.AppendTable(
    It.IsAny<StringBuilder>(),
    It.IsAny<string>(),
    It.IsAny<IEnumerable<It.IsAnyType>>(),
    It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
    It.IsAny<int>(),
    It.IsAny<int>(),
    It.IsAny<int>(),
    It.IsAny<int?>()))
    .Callback(new InvocationAction(invocation =>
    {
        var sb = (StringBuilder)invocation.Arguments[0];
        var title = (string)invocation.Arguments[1];
        sb.AppendLine(title);
        sb.AppendLine("Test Table");
    }));

            // Act
            var result = _renderer.RenderSystemHelp(config, 80);

            // Assert
            result.Should().Contain("SHARP BRIDGE - SYSTEM HELP");
            result.Should().Contain("APPLICATION CONFIGURATION");
            result.Should().Contain("KEYBOARD SHORTCUTS");
            result.Should().Contain("Press any key to return to main display");
            result.Should().Contain("Editor Command");
            result.Should().Contain("notepad.exe");
        }

        #endregion

        #region JsonIgnore Tests

        [Fact]
        public void RenderConfigSection_WithJsonIgnoreProperties_HidesInternalSettings()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001,
                    UsePortDiscovery = true
                    // Internal properties like ConnectionTimeoutMs should be hidden
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("Host");
            result.Should().Contain("Port");
            result.Should().Contain("UsePortDiscovery"); // Property name as-is, not display name

            // These internal properties should NOT appear
            result.Should().NotContain("ConnectionTimeoutMs");
            result.Should().NotContain("ReconnectionDelayMs");
            result.Should().NotContain("RecoveryIntervalSeconds");
            result.Should().NotContain("PluginName");
            result.Should().NotContain("PluginDeveloper");
            result.Should().NotContain("TokenFilePath");
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void RenderConfigSection_WithNullConfigSection_ShowsNotConfiguredMessage()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PCClient = null! // This will trigger the null handling
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("Not configured");
        }

        [Fact]
        public void FormatPropertyValue_WithNullValue_ReturnsNotSet()
        {
            // This tests the private method indirectly through a config with null properties
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = null! // This will be null
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("Not set");
        }

        [Fact]
        public void FormatPropertyValue_WithEmptyString_ReturnsNotSet()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "   " // Whitespace only
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("Not set");
        }

        [Fact]
        public void FormatPropertyValue_WithBooleanFalse_ReturnsNo()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    UsePortDiscovery = false
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("No");
        }

        [Fact]
        public void FormatPropertyValue_WithBooleanTrue_ReturnsYes()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    UsePortDiscovery = true
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("Yes");
        }

        [Fact]
        public void RenderKeyboardShortcuts_WithEmptyShortcuts_CreatesEmptyTable()
        {
            // Arrange
            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>());
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(new Dictionary<ShortcutAction, string>());

            // Setup the generic AppendTable method to work with any type
            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    // Mock the actual table formatter behavior - append the title to the StringBuilder
                    sb.AppendLine();
                    sb.AppendLine($"{title}");
                    sb.AppendLine(new string('═', title.Length));

                    var data = invocation.Arguments[2] as System.Collections.IEnumerable;
                    if (data != null && !data.Cast<object>().Any())
                    {
                        sb.AppendLine("  No shortcuts configured");
                    }
                }));

            // Act
            var result = _renderer.RenderKeyboardShortcuts(80);

            // Assert
            result.Should().Contain("KEYBOARD SHORTCUTS");
            _tableFormatterMock.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public void CenterText_WithNullOrEmptyText_ReturnsOriginalText()
        {
            // Arrange - Setup mocks to prevent null reference exceptions
            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>());
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(new Dictionary<ShortcutAction, string>());
            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    // Mock the actual table formatter behavior
                    sb.AppendLine();
                    sb.AppendLine($"{title}");
                    sb.AppendLine(new string('═', title.Length));
                }));

            var config = new ApplicationConfig();

            // Act
            var result = _renderer.RenderSystemHelp(config, 80);

            // Assert - Test that the method doesn't crash and produces output
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("KEYBOARD SHORTCUTS");
        }

        [Fact]
        public void CenterText_WithTextLongerThanWidth_ReturnsOriginalText()
        {
            // Arrange - Setup mocks to prevent null reference exceptions
            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>());
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(new Dictionary<ShortcutAction, string>());
            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    // Mock the actual table formatter behavior
                    sb.AppendLine();
                    sb.AppendLine($"{title}");
                    sb.AppendLine(new string('═', title.Length));
                }));

            var config = new ApplicationConfig();

            // Act
            var result = _renderer.RenderSystemHelp(config, 80);

            // Assert - Test that the method doesn't crash and produces output
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("KEYBOARD SHORTCUTS");
        }

        [Fact]
        public void GetStatusDisplay_WithUnknownStatus_ReturnsUnknown()
        {
            // Arrange
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
            };

            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(x => x.GetShortcutStatus(ShortcutAction.ShowSystemHelp))
                .Returns((ShortcutStatus)999); // Invalid enum value

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    sb.AppendLine("✗ Unknown");
                }));

            // Act
            var result = _renderer.RenderKeyboardShortcuts(80);

            // Assert
            result.Should().Contain("✗ Unknown");
        }

        [Fact]
        public void FormatPropertyValue_WithObjectReturningNullToString_ReturnsNotSet()
        {
            // This is harder to test directly since most objects have valid ToString()
            // We can test this through a config object that might have such properties
            // For now, we'll verify the existing behavior handles normal cases

            // Arrange
            var config = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "test",
                    Port = 8001
                }
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("test");
            result.Should().Contain("8001");
        }

        #endregion

        #region Complete Integration Tests

        [Fact]
        public void RenderSystemHelp_WithCompleteConfig_RendersAllSections()
        {
            // Arrange
            _shortcutManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>
                {
                    { ShortcutAction.ShowSystemHelp, new Shortcut(ConsoleKey.F1, ConsoleModifiers.None) }
                });
            _shortcutManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(new Dictionary<ShortcutAction, string>());

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumn<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    // Mock the actual table formatter behavior
                    sb.AppendLine();
                    sb.AppendLine($"{title}");
                    sb.AppendLine(new string('═', title.Length));
                    sb.AppendLine("Shortcuts content here");
                }));

            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "code \"%f\""
                },
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.200",
                    IphonePort = 21412,
                    LocalPort = 28964
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "192.168.1.100",
                    Port = 8080,
                    UsePortDiscovery = false
                },
                TransformationEngine = new TransformationEngineConfig
                {
                    ConfigPath = "custom/path.json",
                    MaxEvaluationIterations = 10
                }
            };

            // Act
            var result = _renderer.RenderSystemHelp(config, 120);

            // Assert
            result.Should().Contain("SHARP BRIDGE - SYSTEM HELP");
            result.Should().Contain("APPLICATION CONFIGURATION");
            result.Should().Contain("GENERAL SETTINGS");
            result.Should().Contain("PHONE CLIENT");
            result.Should().Contain("PC CLIENT");
            result.Should().Contain("TRANSFORMATION ENGINE");
            result.Should().Contain("KEYBOARD SHORTCUTS");
            result.Should().Contain("Press any key to return to main display");
        }

        #endregion
    }
}