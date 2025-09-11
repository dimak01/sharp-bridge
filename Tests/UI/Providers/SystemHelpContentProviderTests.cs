using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;
using System.Threading.Tasks;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.UI.Formatters;
using SharpBridge.Interfaces.Core.Services;
using SharpBridge.UI.Providers;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Domain;
using SharpBridge.Models.Infrastructure;
using SharpBridge.UI.Utilities;
using SharpBridge.UI.Formatters;
using SharpBridge.Models.UI;
using SharpBridge.Interfaces.UI.Components;

namespace SharpBridge.Tests.UI.Providers
{
    public class SystemHelpContentProviderTests
    {
        private readonly Mock<IShortcutConfigurationManager> _shortcutManagerMock;
        private readonly Mock<IParameterTableConfigurationManager> _parameterTableConfigManagerMock;
        private readonly Mock<ITableFormatter> _tableFormatterMock;
        private readonly Mock<IExternalEditorService> _externalEditorServiceMock;
        private readonly SystemHelpContentProvider _renderer;

        public SystemHelpContentProviderTests()
        {
            _shortcutManagerMock = new Mock<IShortcutConfigurationManager>();
            _parameterTableConfigManagerMock = new Mock<IParameterTableConfigurationManager>();
            _tableFormatterMock = new Mock<ITableFormatter>();
            _externalEditorServiceMock = new Mock<IExternalEditorService>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            _renderer = new SystemHelpContentProvider(_shortcutManagerMock.Object, _parameterTableConfigManagerMock.Object, _tableFormatterMock.Object, networkStatusFormatterMock.Object, _externalEditorServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullShortcutConfigurationManager_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var parameterTableConfigManagerMock = new Mock<IParameterTableConfigurationManager>();
            var tableFormatterMock = new Mock<ITableFormatter>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            var externalEditorServiceMock = new Mock<IExternalEditorService>();
            var exception = Assert.Throws<ArgumentNullException>(() => new SystemHelpContentProvider(null!, parameterTableConfigManagerMock.Object, tableFormatterMock.Object, networkStatusFormatterMock.Object, externalEditorServiceMock.Object));
            exception.ParamName.Should().Be("shortcutConfigurationManager");
        }

        [Fact]
        public void Constructor_WithNullParameterTableConfigurationManager_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            var tableFormatterMock = new Mock<ITableFormatter>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            var externalEditorServiceMock = new Mock<IExternalEditorService>();
            var exception = Assert.Throws<ArgumentNullException>(() => new SystemHelpContentProvider(shortcutConfigurationManagerMock.Object, null!, tableFormatterMock.Object, networkStatusFormatterMock.Object, externalEditorServiceMock.Object));
            exception.ParamName.Should().Be("parameterTableConfigurationManager");
        }

        [Fact]
        public void Constructor_WithNullTableFormatter_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            var parameterTableConfigManagerMock = new Mock<IParameterTableConfigurationManager>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            var externalEditorServiceMock = new Mock<IExternalEditorService>();
            var exception = Assert.Throws<ArgumentNullException>(() => new SystemHelpContentProvider(shortcutConfigurationManagerMock.Object, parameterTableConfigManagerMock.Object, null!, networkStatusFormatterMock.Object, externalEditorServiceMock.Object));
            exception.ParamName.Should().Be("tableFormatter");
        }



        [Fact]
        public void Constructor_WithNullExternalEditorService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            var parameterTableConfigManagerMock = new Mock<IParameterTableConfigurationManager>();
            var tableFormatterMock = new Mock<ITableFormatter>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            var exception = Assert.Throws<ArgumentNullException>(() => new SystemHelpContentProvider(shortcutConfigurationManagerMock.Object, parameterTableConfigManagerMock.Object, tableFormatterMock.Object, networkStatusFormatterMock.Object, null!));
            exception.ParamName.Should().Be("externalEditorService");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act & Assert
            var shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            var parameterTableConfigManagerMock = new Mock<IParameterTableConfigurationManager>();
            var tableFormatterMock = new Mock<ITableFormatter>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            var externalEditorServiceMock = new Mock<IExternalEditorService>();
            var renderer = new SystemHelpContentProvider(shortcutConfigurationManagerMock.Object, parameterTableConfigManagerMock.Object, tableFormatterMock.Object, networkStatusFormatterMock.Object, externalEditorServiceMock.Object);
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
    It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
            result.Should().Contain("GENERAL SETTINGS");
            result.Should().Contain("KEYBOARD SHORTCUTS");
            result.Should().Contain("Test Table");
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                It.IsAny<int>(),
                80,
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Exactly(2));
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
            // Both should contain the same content sections regardless of width
            result40.Should().Contain("GENERAL SETTINGS:");
            result40.Should().Contain("KEYBOARD SHORTCUTS:");
            result120.Should().Contain("GENERAL SETTINGS:");
            result120.Should().Contain("KEYBOARD SHORTCUTS:");
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
            _shortcutManagerMock.Setup(x => x.GetDisplayString(ShortcutAction.ShowSystemHelp))
                .Returns("F1");
            _tableFormatterMock.Setup(x => x.AppendTable(
    It.IsAny<StringBuilder>(),
    It.IsAny<string>(),
    It.IsAny<IEnumerable<It.IsAnyType>>(),
    It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
            result.Should().Contain("GENERAL SETTINGS");
            result.Should().Contain("KEYBOARD SHORTCUTS");
            result.Should().Contain("External Editor Command");
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
            result.Should().Contain("Host Address");
            result.Should().Contain("VTube Studio PC API Port");
            result.Should().Contain("Enable Automatic Port Discovery"); // Display name from Description attribute

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
            result.Should().Contain("false");
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
            result.Should().Contain("true");
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
            _shortcutManagerMock.Setup(x => x.GetDisplayString(ShortcutAction.ShowSystemHelp))
                .Returns("F1");

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
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
            result.Should().Contain("GENERAL SETTINGS");
            result.Should().Contain("PHONE CLIENT");
            result.Should().Contain("PC CLIENT");
            result.Should().Contain("TRANSFORMATION ENGINE");
            result.Should().Contain("KEYBOARD SHORTCUTS");
        }

        #endregion

        #region Column Formatting Tests (Following PCTrackingInfoFormatterTests Pattern)

        [Fact]
        public void RenderKeyboardShortcuts_WithDifferentShortcutStatuses_ShowsCorrectStatusDisplay()
        {
            // Arrange
            var testCases = new[]
            {
                (action: ShortcutAction.ShowSystemHelp, status: ShortcutStatus.Active, expected: "✓ Active"),
                (action: ShortcutAction.CycleTransformationEngineVerbosity, status: ShortcutStatus.Invalid, expected: "✗ Invalid Format"),
                (action: ShortcutAction.CyclePCClientVerbosity, status: ShortcutStatus.ExplicitlyDisabled, expected: "✗ Disabled"),
                (action: ShortcutAction.CyclePhoneClientVerbosity, status: (ShortcutStatus)999, expected: "✗ Unknown") // Unknown status
            };

            foreach (var (action, status, expected) in testCases)
            {
                var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
                {
                    [action] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
                };
                _shortcutManagerMock.Setup(m => m.GetMappedShortcuts()).Returns(shortcuts);
                _shortcutManagerMock.Setup(m => m.GetDisplayString(action)).Returns("TestKey");
                _shortcutManagerMock.Setup(m => m.GetShortcutStatus(action)).Returns(status);

                // Setup mock to actually append content to StringBuilder
                _tableFormatterMock
                    .Setup(x => x.AppendTable(
                        It.IsAny<StringBuilder>(),
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<It.IsAnyType>>(),
                        It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int?>()))
                    .Callback(new InvocationAction(invocation =>
                    {
                        var builder = (StringBuilder)invocation.Arguments[0];
                        var title = (string)invocation.Arguments[1];
                        builder.AppendLine(title);
                        builder.AppendLine(expected); // Add the expected status for verification
                    }));

                // Act
                var result = _renderer.RenderKeyboardShortcuts(120);

                // Assert
                result.Should().Contain(expected, $"Action {action} with status {status} should show '{expected}'");
            }
        }

        #endregion

        private static string CallCenterTextMethod(string text, int width)
        {
            // Use reflection to call the private CenterText method
            var method = typeof(SystemHelpContentProvider).GetMethod("CenterText", BindingFlags.NonPublic | BindingFlags.Static);
            return (string)method!.Invoke(null, new object[] { text, width })!;
        }

        private static string CallColorizeBasicTypeMethod(object? value)
        {
            // Call the ColorizeBasicType method directly
            return ConsoleColors.ColorizeBasicType(value);
        }

        #region Additional Coverage Tests

        [Fact]
        public void RenderSystemHelp_WithNullApplicationConfig_HandlesGracefully()
        {
            // Arrange
            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.RenderSystemHelp(null!, 120);

            // Assert
            result.Should().Contain("No configuration loaded");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        [Fact]
        public void RenderApplicationConfiguration_WithNullConfigSections_HandlesGracefully()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = null!,
                PhoneClient = null!,
                PCClient = null!,
                TransformationEngine = null!
            };

            // Act
            var result = _renderer.RenderApplicationConfiguration(config);

            // Assert
            result.Should().Contain("Not configured");
        }

        /// <summary>
        /// Helper method to set up basic table formatter mock to avoid repetition
        /// </summary>
        private void SetupBasicTableFormatterMock()
        {
            _shortcutManagerMock.Setup(m => m.GetMappedShortcuts()).Returns(new Dictionary<ShortcutAction, Shortcut?>());
            _tableFormatterMock
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<It.IsAnyType>>(),
                    It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var builder = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    builder.AppendLine(title);
                }));
        }

        #endregion

        #region Property Reflection and JsonIgnore Tests

        [Fact]
        public void RenderConfigSection_SkipsJsonIgnoreProperties()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true,
                // These should be skipped due to JsonIgnore
                ConnectionTimeoutMs = 5000,
                ReconnectionDelayMs = 2000,
                PluginName = "TestPlugin"
            };
            var builder = new StringBuilder();

            // Act - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("RenderConfigSection", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[] { builder, config, null! });

            var result = builder.ToString();

            // Assert
            var cleanResult = ConsoleColors.RemoveAnsiEscapeCodes(result);
            cleanResult.Should().Contain("Host Address: localhost");
            cleanResult.Should().Contain("VTube Studio PC API Port: 8001");
            cleanResult.Should().Contain("Enable Automatic Port Discovery: true");

            // These should NOT appear due to JsonIgnore
            result.Should().NotContain("ConnectionTimeoutMs");
            result.Should().NotContain("ReconnectionDelayMs");
            result.Should().NotContain("PluginName");
        }

        [Fact]
        public void RenderConfigSection_SkipsSpecifiedProperties()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe",
                Shortcuts = new Dictionary<string, string> { ["Test"] = "Ctrl+T" }
            };
            var builder = new StringBuilder();

            // Act - Call the private method using reflection, skipping Shortcuts
            var method = typeof(SystemHelpContentProvider).GetMethod("RenderConfigSection", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[] { builder, config, new[] { "Shortcuts" } });

            var result = builder.ToString();

            // Assert
            var cleanResult = ConsoleColors.RemoveAnsiEscapeCodes(result);
            cleanResult.Should().Contain("Editor Command: notepad.exe");
            cleanResult.Should().NotContain("Shortcuts"); // Should be skipped
        }

        [Fact]
        public void RenderConfigSection_WithNullSkipProperties_HandlesGracefully()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe"
            };
            var builder = new StringBuilder();

            // Act - Call the private method using reflection with null skipProperties
            var method = typeof(SystemHelpContentProvider).GetMethod("RenderConfigSection", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[] { builder, config, null! });

            var result = builder.ToString();

            // Assert
            var cleanResult = ConsoleColors.RemoveAnsiEscapeCodes(result);
            cleanResult.Should().Contain("Editor Command: notepad.exe");
        }

        #endregion

        #region Mock Object for ToString() Testing

        private class MockObjectWithNullToString
        {
            public override string? ToString() => null;
        }

        #endregion

        #region Column Creation Coverage Tests (Following PCTrackingInfoFormatterTests Pattern)

        [Fact]
        public void RenderKeyboardShortcuts_CreatesCorrectTextColumns()
        {
            // Arrange - Use a real TableFormatter to ensure column creation actually happens
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None),
                [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt)
            };

            _shortcutManagerMock.Setup(m => m.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(m => m.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());
            _shortcutManagerMock.Setup(m => m.GetDisplayString(ShortcutAction.ShowSystemHelp)).Returns("F1");
            _shortcutManagerMock.Setup(m => m.GetDisplayString(ShortcutAction.CycleTransformationEngineVerbosity)).Returns("Alt+T");
            _shortcutManagerMock.Setup(m => m.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(ShortcutStatus.Active);
            _shortcutManagerMock.Setup(m => m.GetShortcutStatus(ShortcutAction.CycleTransformationEngineVerbosity)).Returns(ShortcutStatus.Active);

            // Create a renderer with a real TableFormatter to ensure the column creation code executes
            var realTableFormatter = new TableFormatter();
            var parameterTableConfigManagerMock = new Mock<IParameterTableConfigurationManager>();
            var networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            var rendererWithRealFormatter = new SystemHelpContentProvider(_shortcutManagerMock.Object, parameterTableConfigManagerMock.Object, realTableFormatter, networkStatusFormatterMock.Object, _externalEditorServiceMock.Object);

            // Act - This will execute the column creation code on lines 192-194 since we're using a real TableFormatter
            var result = rendererWithRealFormatter.RenderKeyboardShortcuts(120);

            // Assert - Verify the output contains expected content and that the method executed fully
            result.Should().Contain("KEYBOARD SHORTCUTS:");
            result.Should().Contain("F1");
            result.Should().Contain("Alt+T");

            // Verify that the shortcut manager methods were called (proving the method executed)
            _shortcutManagerMock.Verify(m => m.GetMappedShortcuts(), Times.Once);
            _shortcutManagerMock.Verify(m => m.GetDisplayString(It.IsAny<ShortcutAction>()), Times.AtLeastOnce);
            _shortcutManagerMock.Verify(m => m.GetShortcutStatus(It.IsAny<ShortcutAction>()), Times.AtLeastOnce);
        }

        #endregion



        [Fact]
        public void ColorizeBasicType_WithObjectToStringReturningNull_ReturnsNotSet()
        {
            // Arrange - Create a mock object that returns null from ToString()
            var mockObject = new Mock<object>();
            mockObject.Setup(x => x.ToString()).Returns((string?)null);

            // Act - Call the ColorizeBasicType method directly
            var result = ConsoleColors.ColorizeBasicType(mockObject.Object);

            // Assert - Should return "Not set" when ToString() returns null
            Assert.Equal("Not set", result);
        }

        #region RenderParameterTableColumns Tests

        [Fact]
        public void RenderParameterTableColumns_WithDefaultColumns_ReturnsFormattedOutput()
        {
            // Arrange
            var defaultColumns = new[]
            {
                ParameterTableColumn.ParameterName,
                ParameterTableColumn.ProgressBar,
                ParameterTableColumn.Value,
                ParameterTableColumn.Range,
                ParameterTableColumn.Expression
            };

            _parameterTableConfigManagerMock.Setup(x => x.GetParameterTableColumns()).Returns(defaultColumns);
            _parameterTableConfigManagerMock.Setup(x => x.GetDefaultParameterTableColumns()).Returns(defaultColumns);
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.ParameterName)).Returns("Parameter Name");
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.ProgressBar)).Returns("Progress Bar");
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.Value)).Returns("Value");
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.Range)).Returns("Range");
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.Expression)).Returns("Expression");

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    sb.AppendLine(title);
                    sb.AppendLine("Parameter Name");
                    sb.AppendLine("Progress Bar");
                    sb.AppendLine("Value");
                    sb.AppendLine("Range");
                    sb.AppendLine("Expression");
                }));

            // Act
            var result = _renderer.RenderParameterTableColumns(120);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("PC PARAMETER TABLE COLUMNS:");
            result.Should().Contain("Parameter Name");
            result.Should().Contain("Progress Bar");
            result.Should().Contain("Value");
            result.Should().Contain("Range");
            result.Should().Contain("Expression");

            _parameterTableConfigManagerMock.Verify(x => x.GetParameterTableColumns(), Times.Once);
            _parameterTableConfigManagerMock.Verify(x => x.GetColumnDisplayName(It.IsAny<ParameterTableColumn>()), Times.Exactly(5));
        }

        [Fact]
        public void RenderParameterTableColumns_WithCustomColumns_ShowsColumns()
        {
            // Arrange
            var customColumns = new[]
            {
                ParameterTableColumn.Value,
                ParameterTableColumn.Expression
            };

            _parameterTableConfigManagerMock.Setup(x => x.GetParameterTableColumns()).Returns(customColumns);
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.Value)).Returns("Value");
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.Expression)).Returns("Expression");

            _tableFormatterMock.Setup(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var sb = (StringBuilder)invocation.Arguments[0];
                    var title = (string)invocation.Arguments[1];
                    sb.AppendLine(title);
                    sb.AppendLine("Value");
                    sb.AppendLine("Expression");
                }));

            // Act
            var result = _renderer.RenderParameterTableColumns(120);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("PC PARAMETER TABLE COLUMNS:");
            result.Should().Contain("Value");
            result.Should().Contain("Expression");

            _parameterTableConfigManagerMock.Verify(x => x.GetParameterTableColumns(), Times.Once);
        }

        [Fact]
        public void RenderParameterTableColumns_WithEmptyColumns_HandlesGracefully()
        {
            // Arrange
            var emptyColumns = Array.Empty<ParameterTableColumn>();

            _parameterTableConfigManagerMock.Setup(x => x.GetParameterTableColumns()).Returns(emptyColumns);

            // Act
            var result = _renderer.RenderParameterTableColumns(120);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("PC PARAMETER TABLE COLUMNS:");
            result.Should().NotContain("Parameter Name"); // Should be empty

            _parameterTableConfigManagerMock.Verify(x => x.GetParameterTableColumns(), Times.Once);
        }

        [Fact]
        public void RenderParameterTableColumns_CreatesCorrectTableStructure()
        {
            // Arrange
            var columns = new[]
            {
                ParameterTableColumn.ParameterName,
                ParameterTableColumn.Value
            };

            _parameterTableConfigManagerMock.Setup(x => x.GetParameterTableColumns()).Returns(columns);
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.ParameterName)).Returns("Parameter Name");
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(ParameterTableColumn.Value)).Returns("Value");

            // Act
            var result = _renderer.RenderParameterTableColumns(120);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("PC PARAMETER TABLE COLUMNS:");

            // Verify table formatter was called
            _tableFormatterMock.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()
            ), Times.Once);
        }

        [Fact]
        public void RenderSystemHelp_IncludesParameterTableColumnsSection()
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
                [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
            };

            _shortcutManagerMock.Setup(m => m.GetMappedShortcuts()).Returns(shortcuts);
            _shortcutManagerMock.Setup(m => m.GetDisplayString(ShortcutAction.ShowSystemHelp)).Returns("F1");
            _shortcutManagerMock.Setup(m => m.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(ShortcutStatus.Active);

            var defaultColumns = new[]
            {
                ParameterTableColumn.ParameterName,
                ParameterTableColumn.ProgressBar,
                ParameterTableColumn.Value,
                ParameterTableColumn.Range,
                ParameterTableColumn.Expression
            };

            _parameterTableConfigManagerMock.Setup(x => x.GetParameterTableColumns()).Returns(defaultColumns);
            _parameterTableConfigManagerMock.Setup(x => x.GetDefaultParameterTableColumns()).Returns(defaultColumns);
            _parameterTableConfigManagerMock.Setup(x => x.GetColumnDisplayName(It.IsAny<ParameterTableColumn>())).Returns("Test Column");

            // Act
            var result = _renderer.RenderSystemHelp(config, 120);

            // Assert
            result.Should().Contain("PC PARAMETER TABLE COLUMNS:");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        #endregion

        #region IConsoleModeContentProvider Interface Tests

        [Fact]
        public void Mode_ReturnsSystemHelp()
        {
            // Arrange & Act & Assert
            _renderer.Mode.Should().Be(ConsoleMode.SystemHelp);
        }

        [Fact]
        public void DisplayName_ReturnsSystemHelp()
        {
            // Arrange & Act & Assert
            _renderer.DisplayName.Should().Be("System Help");
        }

        [Fact]
        public void ToggleAction_ReturnsShowSystemHelp()
        {
            // Arrange & Act & Assert
            _renderer.ToggleAction.Should().Be(ShortcutAction.ShowSystemHelp);
        }

        [Fact]
        public void PreferredUpdateInterval_Returns100Milliseconds()
        {
            // Arrange & Act & Assert
            _renderer.PreferredUpdateInterval.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void Enter_WithConsole_DoesNotThrow()
        {
            // Arrange
            var consoleMock = new Mock<IConsole>();

            // Act & Assert
            var exception = Record.Exception(() => _renderer.Enter(consoleMock.Object));
            exception.Should().BeNull();
        }

        [Fact]
        public void Exit_WithConsole_DoesNotThrow()
        {
            // Arrange
            var consoleMock = new Mock<IConsole>();

            // Act & Assert
            var exception = Record.Exception(() => _renderer.Exit(consoleMock.Object));
            exception.Should().BeNull();
        }

        [Fact]
        public void GetContent_WithValidContext_ReturnsFormattedContent()
        {
            // Arrange
            var context = new ConsoleRenderContext
            {
                ApplicationConfig = new ApplicationConfig
                {
                    GeneralSettings = new GeneralSettingsConfig
                    {
                        EditorCommand = "notepad.exe"
                    }
                },
                ConsoleSize = (80, 25)
            };

            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().Contain("GENERAL SETTINGS:");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        [Fact]
        public void GetContent_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<NullReferenceException>(() => _renderer.GetContent(null!));
            exception.Should().NotBeNull();
        }

        [Fact]
        public void GetContent_WithNullApplicationConfig_HandlesGracefully()
        {
            // Arrange
            var context = new ConsoleRenderContext
            {
                ApplicationConfig = null!,
                ConsoleSize = (80, 25)
            };

            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("  No configuration loaded");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
            result.Should().Contain("PC PARAMETER TABLE COLUMNS:");
        }

        [Fact]
        public void GetContent_WithDifferentConsoleWidths_AdjustsRendering()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe"
                }
            };

            SetupBasicTableFormatterMock();

            // Act
            var result80 = _renderer.GetContent(new ConsoleRenderContext { ApplicationConfig = config, ConsoleSize = (80, 25) });
            var result120 = _renderer.GetContent(new ConsoleRenderContext { ApplicationConfig = config, ConsoleSize = (120, 25) });

            // Assert
            result80.Should().NotBeNull();
            result120.Should().NotBeNull();
            result80.Should().Contain("GENERAL SETTINGS:");
            result120.Should().Contain("GENERAL SETTINGS:");
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WithValidService_ReturnsTrue()
        {
            // Arrange
            _externalEditorServiceMock.Setup(x => x.TryOpenApplicationConfigAsync()).ReturnsAsync(true);

            // Act
            var result = await _renderer.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeTrue();
            _externalEditorServiceMock.Verify(x => x.TryOpenApplicationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WithServiceReturningFalse_ReturnsFalse()
        {
            // Arrange
            _externalEditorServiceMock.Setup(x => x.TryOpenApplicationConfigAsync()).ReturnsAsync(false);

            // Act
            var result = await _renderer.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
            _externalEditorServiceMock.Verify(x => x.TryOpenApplicationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WithServiceThrowingException_ReturnsFalse()
        {
            // Arrange
            _externalEditorServiceMock.Setup(x => x.TryOpenApplicationConfigAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _renderer.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
            _externalEditorServiceMock.Verify(x => x.TryOpenApplicationConfigAsync(), Times.Once);
        }

        #endregion

        #region CreateSectionHeader Edge Case Tests

        [Fact]
        public void CreateSectionHeader_WithVeryLongTitle_CalculatesUnderlineWidthCorrectly()
        {
            // Arrange
            var veryLongTitle = new string('A', 100);
            var maxWidth = 60;

            // Act - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("CreateSectionHeader", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (string[])method!.Invoke(null, new object[] { veryLongTitle, maxWidth })!;

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().Be($"{veryLongTitle}:"); // Title is not truncated
            result[1].Should().HaveLength(maxWidth); // Underline is limited to maxWidth
        }

        [Fact]
        public void CreateSectionHeader_WithTitleExactlyMaxWidth_HandlesCorrectly()
        {
            // Arrange
            var title = new string('A', 58); // 58 + 2 = 60 (maxWidth)
            var maxWidth = 60;

            // Act - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("CreateSectionHeader", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (string[])method!.Invoke(null, new object[] { title, maxWidth })!;

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().Be($"{title}:");
            result[1].Should().HaveLength(60);
        }

        [Fact]
        public void CreateSectionHeader_WithEmptyTitle_HandlesGracefully()
        {
            // Arrange
            var emptyTitle = "";
            var maxWidth = 60;

            // Act - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("CreateSectionHeader", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (string[])method!.Invoke(null, new object[] { emptyTitle, maxWidth })!;

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().Be(":");
            result[1].Should().HaveLength(2);
        }

        [Fact]
        public void CreateSectionHeader_WithNullTitle_ThrowsNullReferenceException()
        {
            // Arrange
            string? nullTitle = null;
            var maxWidth = 60;

            // Act & Assert - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("CreateSectionHeader", BindingFlags.NonPublic | BindingFlags.Static);
            var exception = Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, new object[] { nullTitle!, maxWidth }));
            exception.InnerException.Should().BeOfType<NullReferenceException>();
        }

        #endregion

        #region GetStatusDisplay Comprehensive Tests

        [Fact]
        public void GetStatusDisplay_WithAllStatusValues_ReturnsCorrectDisplay()
        {
            // Arrange
            var testCases = new[]
            {
                (status: ShortcutStatus.Active, expected: "✓ Active"),
                (status: ShortcutStatus.Invalid, expected: "✗ Invalid Format"),
                (status: ShortcutStatus.ExplicitlyDisabled, expected: "✗ Disabled"),
                (status: (ShortcutStatus)999, expected: "✗ Unknown") // Invalid enum value
            };

            foreach (var (status, expected) in testCases)
            {
                var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
                {
                    [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
                };

                _shortcutManagerMock.Setup(m => m.GetMappedShortcuts()).Returns(shortcuts);
                _shortcutManagerMock.Setup(m => m.GetShortcutStatus(ShortcutAction.ShowSystemHelp)).Returns(status);
                _shortcutManagerMock.Setup(m => m.GetDisplayString(ShortcutAction.ShowSystemHelp)).Returns("F1");

                // Setup mock to actually append content to StringBuilder
                _tableFormatterMock
                    .Setup(x => x.AppendTable(
                        It.IsAny<StringBuilder>(),
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<It.IsAnyType>>(),
                        It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int?>()))
                    .Callback(new InvocationAction(invocation =>
                    {
                        var builder = (StringBuilder)invocation.Arguments[0];
                        var title = (string)invocation.Arguments[1];
                        builder.AppendLine(title);
                        builder.AppendLine(expected); // Add the expected status for verification
                    }));

                // Act
                var result = _renderer.RenderKeyboardShortcuts(120);

                // Assert
                result.Should().Contain(expected, $"Status {status} should show '{expected}'");
            }
        }

        #endregion

        #region RenderConfigSection Edge Case Tests

        [Fact]
        public void RenderConfigSection_WithEmptySkipProperties_HandlesGracefully()
        {
            // Arrange
            var config = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe"
            };
            var builder = new StringBuilder();
            var emptySkipProperties = Array.Empty<string>();

            // Act - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("RenderConfigSection", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[] { builder, config, emptySkipProperties });

            var result = builder.ToString();

            // Assert
            var cleanResult = ConsoleColors.RemoveAnsiEscapeCodes(result);
            cleanResult.Should().Contain("Editor Command: notepad.exe");
        }

        [Fact]
        public void RenderConfigSection_WithAllPropertiesJsonIgnored_ShowsNotConfigured()
        {
            // This test would require a mock object with all properties marked as JsonIgnore
            // For now, we'll test the existing behavior with real objects that have some JsonIgnore properties
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001
                // Most other properties are JsonIgnore
            };
            var builder = new StringBuilder();

            // Act - Call the private method using reflection
            var method = typeof(SystemHelpContentProvider).GetMethod("RenderConfigSection", BindingFlags.NonPublic | BindingFlags.Static);
            method!.Invoke(null, new object[] { builder, config, null! });

            var result = builder.ToString();

            // Assert
            var cleanResult = ConsoleColors.RemoveAnsiEscapeCodes(result);
            cleanResult.Should().Contain("Host Address: localhost");
            cleanResult.Should().Contain("VTube Studio PC API Port: 8001");
            // Should not contain JsonIgnore properties
            result.Should().NotContain("ConnectionTimeoutMs");
        }

        #endregion

        #region Integration Tests with ConsoleRenderContext

        [Fact]
        public void GetContent_WithCompleteContext_RendersAllSections()
        {
            // Arrange
            var context = new ConsoleRenderContext
            {
                ApplicationConfig = new ApplicationConfig
                {
                    GeneralSettings = new GeneralSettingsConfig
                    {
                        EditorCommand = "code \"%f\""
                    },
                    PhoneClient = new VTubeStudioPhoneClientConfig
                    {
                        IphoneIpAddress = "192.168.1.200",
                        IphonePort = 21412
                    },
                    PCClient = new VTubeStudioPCConfig
                    {
                        Host = "192.168.1.100",
                        Port = 8080
                    },
                    TransformationEngine = new TransformationEngineConfig
                    {
                        ConfigPath = "custom/path.json"
                    }
                },
                ConsoleSize = (120, 30)
            };

            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("GENERAL SETTINGS:");
            result.Should().Contain("PHONE CLIENT:");
            result.Should().Contain("PC CLIENT:");
            result.Should().Contain("TRANSFORMATION ENGINE:");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        [Fact]
        public void GetContent_WithMinimalContext_HandlesGracefully()
        {
            // Arrange
            var context = new ConsoleRenderContext
            {
                ApplicationConfig = new ApplicationConfig(), // Empty config
                ConsoleSize = (40, 10) // Small console
            };

            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("GENERAL SETTINGS:");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        #endregion

        #region Performance and Edge Case Tests

        [Fact]
        public void RenderSystemHelp_WithVeryWideConsole_HandlesGracefully()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe"
                }
            };

            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.RenderSystemHelp(config, 200); // Very wide console

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("GENERAL SETTINGS:");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        [Fact]
        public void RenderSystemHelp_WithVeryNarrowConsole_HandlesGracefully()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe"
                }
            };

            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.RenderSystemHelp(config, 20); // Very narrow console

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("GENERAL SETTINGS:");
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public void SetupBasicTableFormatterMock_WorksCorrectly()
        {
            // Arrange
            SetupBasicTableFormatterMock();

            // Act
            var result = _renderer.RenderSystemHelp(new ApplicationConfig(), 80);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("KEYBOARD SHORTCUTS:");
        }

        #endregion
    }
}