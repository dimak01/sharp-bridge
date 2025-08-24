using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class MainStatusContentProviderTests : IDisposable
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IFormatter> _mockTransformationFormatter;
        private readonly Mock<IFormatter> _mockPhoneFormatter;
        private readonly Mock<IFormatter> _mockPCFormatter;
        private readonly Mock<IShortcutConfigurationManager> _mockShortcutManager;
        private readonly Mock<IExternalEditorService> _mockExternalEditorService;
        private readonly UserPreferences _userPreferences;

        // Test entities for formatter testing
        private readonly TransformationEngineInfo _testTransformationInfo;
        private readonly PhoneTrackingInfo _testPhoneInfo;
        private readonly PCTrackingInfo _testPCInfo;
        private readonly TestEntity _testEntity;

        public MainStatusContentProviderTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            _mockExternalEditorService = new Mock<IExternalEditorService>();
            _userPreferences = new UserPreferences();

            // Setup formatter mocks
            _mockTransformationFormatter = new Mock<IFormatter>();
            _mockPhoneFormatter = new Mock<IFormatter>();
            _mockPCFormatter = new Mock<IFormatter>();

            // Setup test data
            _testTransformationInfo = new TransformationEngineInfo("test-config.json", 0, null, false);
            _testPhoneInfo = new PhoneTrackingInfo();
            _testPCInfo = new PCTrackingInfo();
            _testEntity = new TestEntity("Test Data", 42);

            // Setup default mock behaviors
            _mockTransformationFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                .Returns("Transformation Engine Status");
            _mockPhoneFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                .Returns("Phone Tracking Status");
            _mockPCFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                .Returns("PC Tracking Status");
            _mockExternalEditorService.Setup(s => s.TryOpenTransformationConfigAsync())
                .ReturnsAsync(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup managed resources if needed
            }
        }

        #region Test Entity Classes

        public class TestEntity : IFormattableObject
        {
            public string Name { get; set; }
            public int Value { get; set; }

            public TestEntity(string name, int value)
            {
                Name = name;
                Value = value;
            }
        }

        #endregion

        #region Constructor & Interface Tests (8 tests)

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var provider = new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object);

            // Assert
            provider.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(
                null!,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithNullTransformationFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(
                _mockLogger.Object,
                null!,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("transformationFormatter");
        }

        [Fact]
        public void Constructor_WithNullPhoneFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                null!,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("phoneFormatter");
        }

        [Fact]
        public void Constructor_WithNullPCFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                null!,
                _mockShortcutManager.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("pcFormatter");
        }

        [Fact]
        public void Constructor_WithNullShortcutManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("shortcutManager");
        }

        [Fact]
        public void Constructor_WithExternalEditorService_InitializesCorrectly()
        {
            // Act
            var provider = new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object,
                _mockExternalEditorService.Object);

            // Assert
            provider.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullExternalEditorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object,
                null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("externalEditorService");
        }

        #endregion

        #region Interface Properties Tests (4 tests)

        [Fact]
        public void Mode_ReturnsMain()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            provider.Mode.Should().Be(ConsoleMode.Main);
        }

        [Fact]
        public void DisplayName_ReturnsMainStatus()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            provider.DisplayName.Should().Be("Main Status");
        }

        [Fact]
        public void ToggleAction_ReturnsShowSystemHelp()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            provider.ToggleAction.Should().Be(ShortcutAction.ShowSystemHelp);
        }

        [Fact]
        public void PreferredUpdateInterval_Returns100Milliseconds()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            provider.PreferredUpdateInterval.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        #endregion

        #region GetContent Method Tests (12+ tests)

        [Fact]
        public void GetContent_WithNullContext_ReturnsEmptyArray()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var result = provider.GetContent(null!);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithNullServiceStats_ReturnsEmptyArray()
        {
            // Arrange
            var provider = CreateProvider();
            var context = new ConsoleRenderContext { ServiceStats = null! };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithEmptyServiceStats_ReturnsEmptyArray()
        {
            // Arrange
            var provider = CreateProvider();
            var context = new ConsoleRenderContext { ServiceStats = Array.Empty<IServiceStats>() };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithSingleService_ReturnsFormattedContent()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("TestService", "Running", true, _testTransformationInfo);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Service content + empty line
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithMultipleServices_ReturnsAllServices()
        {
            // Arrange
            var provider = CreateProvider();
            var stats1 = CreateServiceStats("Service1", "Running", true, _testTransformationInfo);
            var stats2 = CreateServiceStats("Service2", "Running", true, _testPhoneInfo);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats1, stats2 } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4); // 2 services + 2 empty lines
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
            result[2].Should().Be("Phone Tracking Status");
            result[3].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithNullService_HandlesGracefully()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = new IServiceStats[] { null!, CreateServiceStats("Service1", "Running", true, _testTransformationInfo) };
            var context = new ConsoleRenderContext { ServiceStats = stats };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Only valid service + empty line
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithNullEntity_UsesNoDataFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("PhoneService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Formatter output + empty line
            result[0].Should().Be("Phone Tracking Status");
            result[1].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithUnregisteredEntityType_ShowsNoFormatterMessage()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("UnknownService", "Running", true, _testEntity);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("=== UnknownService (Running) ===");
            result[1].Should().Contain("[No formatter registered for TestEntity]");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithPhoneService_SelectsPhoneFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("PhoneService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Formatter output + empty line
            result[0].Should().Be("Phone Tracking Status");
            result[1].Should().BeEmpty();
            _mockPhoneFormatter.Verify(f => f.Format(stats), Times.Once); // Should be called for Phone service
        }

        [Fact]
        public void GetContent_WithPCService_SelectsPCFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("PCService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Formatter output + empty line
            result[0].Should().Be("PC Tracking Status");
            result[1].Should().BeEmpty();
            _mockPCFormatter.Verify(f => f.Format(stats), Times.Once); // Should be called for PC service
        }

        [Fact]
        public void GetContent_WithUnknownService_ShowsGenericMessage()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("UnknownService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("=== UnknownService (Running) ===");
            result[1].Should().Contain("No current data available");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithMixedServiceTypes_HandlesCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var stats1 = CreateServiceStats("TransformationService", "Running", true, _testTransformationInfo);
            var stats2 = CreateServiceStats("PhoneService", "Running", true, null);
            var stats3 = CreateServiceStats("PCService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats1, stats2, stats3 } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(6); // 3 services + 3 empty lines
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
            result[2].Should().Be("Phone Tracking Status");
            result[3].Should().BeEmpty();
            result[4].Should().Be("PC Tracking Status");
            result[5].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithMultiLineFormatterOutput_SplitsCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            _mockTransformationFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                .Returns("Line 1" + Environment.NewLine + "Line 2" + Environment.NewLine + "Line 3");

            var stats = CreateServiceStats("TestService", "Running", true, _testTransformationInfo);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4); // 3 lines + empty line
            result[0].Should().Be("Line 1");
            result[1].Should().Be("Line 2");
            result[2].Should().Be("Line 3");
            result[3].Should().BeEmpty();
        }

        #endregion

        #region External Editor Integration Tests (4 tests)

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WithValidService_ReturnsTrue()
        {
            // Arrange
            var provider = CreateProviderWithExternalEditor();

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeTrue();
            _mockExternalEditorService.Verify(s => s.TryOpenTransformationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WhenServiceFails_ReturnsFalse()
        {
            // Arrange
            _mockExternalEditorService.Setup(s => s.TryOpenTransformationConfigAsync()).ReturnsAsync(false);
            var provider = CreateProviderWithExternalEditor();

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WithoutExternalEditorService_ReturnsFalse()
        {
            // Arrange
            var provider = CreateProvider(); // No external editor service

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WhenExceptionOccurs_LogsErrorAndReturnsFalse()
        {
            // Arrange
            _mockExternalEditorService.Setup(s => s.TryOpenTransformationConfigAsync())
                .ThrowsAsync(new InvalidOperationException("Test exception"));
            var provider = CreateProviderWithExternalEditor();

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(l => l.ErrorWithException("Error opening transformation config in external editor", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region Mode Lifecycle Tests (4 tests)

        [Fact]
        public void Enter_DoesNotModifyConsole()
        {
            // Arrange
            var provider = CreateProvider();
            var mockConsole = new Mock<IConsole>();

            // Act
            provider.Enter(mockConsole.Object);

            // Assert
            mockConsole.Verify(c => c.Clear(), Times.Never);
            // Enter is currently a no-op, so we just verify it doesn't crash
        }

        [Fact]
        public void Exit_DoesNotModifyConsole()
        {
            // Arrange
            var provider = CreateProvider();
            var mockConsole = new Mock<IConsole>();

            // Act
            provider.Exit(mockConsole.Object);

            // Assert
            mockConsole.Verify(c => c.Clear(), Times.Never);
            // Exit is currently a no-op, so we just verify it doesn't crash
        }

        [Fact]
        public void Enter_Exit_MultipleCalls_NoSideEffects()
        {
            // Arrange
            var provider = CreateProvider();
            var mockConsole = new Mock<IConsole>();

            // Act
            provider.Enter(mockConsole.Object);
            provider.Exit(mockConsole.Object);
            provider.Enter(mockConsole.Object);
            provider.Exit(mockConsole.Object);

            // Assert
            mockConsole.Verify(c => c.Clear(), Times.Never);
            // Multiple calls should not cause any issues
        }

        [Fact]
        public void Enter_Exit_MaintainsState()
        {
            // Arrange
            var provider = CreateProvider();
            var mockConsole = new Mock<IConsole>();
            var context = new ConsoleRenderContext { ServiceStats = new[] { CreateServiceStats("Test", "Running", true, _testTransformationInfo) } };

            // Act
            provider.Enter(mockConsole.Object);
            var contentBefore = provider.GetContent(context);
            provider.Exit(mockConsole.Object);
            var contentAfter = provider.GetContent(context);

            // Assert
            contentBefore.Should().BeEquivalentTo(contentAfter);
        }

        #endregion

        #region Error Handling & Edge Cases (8 tests)

        [Fact]
        public void GetContent_WithNullServiceInStats_HandlesGracefully()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = new IServiceStats[] { null!, null! };
            var context = new ConsoleRenderContext { ServiceStats = stats };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty(); // All services are null, so empty result
        }

        [Fact]
        public void GetContent_WithFormatterException_ShowsErrorMessage()
        {
            // Arrange
            var provider = CreateProvider();
            _mockTransformationFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                .Throws(new InvalidOperationException("Formatter error"));

            var stats = CreateServiceStats("TestService", "Running", true, _testTransformationInfo);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("=== TestService (Running) ===");
            result[1].Should().Contain("[No formatter registered for TransformationEngineInfo]");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithNullFormatter_ShowsNoFormatterMessage()
        {
            // Arrange
            var provider = CreateProvider();
            // Don't register any formatters for the test entity
            var stats = CreateServiceStats("TestService", "Running", true, _testEntity);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("=== TestService (Running) ===");
            result[1].Should().Contain("[No formatter registered for TestEntity]");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithIncompatibleEntityType_HandlesGracefully()
        {
            // Arrange
            var provider = CreateProvider();
            var incompatibleEntity = new TestEntity("incompatible", 999); // Use TestEntity instead of string
            var stats = CreateServiceStats("TestService", "Running", true, incompatibleEntity);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("=== TestService (Running) ===");
            result[1].Should().Contain("[No formatter registered for TestEntity]");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithEmptyServiceName_HandlesCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("===  (Running) ===");
            result[1].Should().Contain("No current data available");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithNullStatus_HandlesCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("TestService", null!, true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain("=== TestService () ===");
            result[1].Should().Contain("No current data available");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithVeryLongServiceName_HandlesCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var longName = new string('A', 1000);
            var stats = CreateServiceStats(longName, "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain($"=== {longName} (Running) ===");
            result[1].Should().Contain("No current data available");
            result[2].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_WithSpecialCharactersInServiceName_HandlesCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var specialName = "Service@#$%^&*()_+-=[]{}|;':\",./<>?";
            var stats = CreateServiceStats(specialName, "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Header + content + empty line
            result[0].Should().Contain($"=== {specialName} (Running) ===");
            result[1].Should().Contain("No current data available");
            result[2].Should().BeEmpty();
        }

        #endregion

        #region Formatter Integration Tests (6 tests)

        [Fact]
        public void RegisterFormatter_WithValidFormatter_RegistersSuccessfully()
        {
            // Arrange
            var provider = CreateProvider();
            var mockFormatter = new Mock<IFormatter>();

            // Act
            Action act = () => provider.RegisterFormatter<TestEntity>(mockFormatter.Object);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GetFormatter_WithRegisteredType_ReturnsFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var mockFormatter = new Mock<IFormatter>();
            provider.RegisterFormatter<TestEntity>(mockFormatter.Object);

            // Act
            var result = provider.GetFormatter<TestEntity>();

            // Assert
            result.Should().BeSameAs(mockFormatter.Object);
        }

        [Fact]
        public void GetFormatter_WithUnregisteredType_ReturnsNull()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var result = provider.GetFormatter<TestEntity>();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetContent_WithTransformationFormatter_UsesCorrectFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("TransformationService", "Running", true, _testTransformationInfo);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Should().Be("Transformation Engine Status");
            _mockTransformationFormatter.Verify(f => f.Format(stats), Times.Once);
        }

        [Fact]
        public void GetContent_WithPhoneFormatter_UsesCorrectFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("PhoneService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Formatter output + empty line
            result[0].Should().Be("Phone Tracking Status");
            result[1].Should().BeEmpty();
            _mockPhoneFormatter.Verify(f => f.Format(stats), Times.Once);
        }

        [Fact]
        public void GetContent_WithPCFormatter_UsesCorrectFormatter()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = CreateServiceStats("PCService", "Running", true, null);
            var context = new ConsoleRenderContext { ServiceStats = new[] { stats } };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Formatter output + empty line
            result[0].Should().Be("PC Tracking Status");
            result[1].Should().BeEmpty();
            _mockPCFormatter.Verify(f => f.Format(stats), Times.Once);
        }

        #endregion

        #region Integration Tests (4 tests)

        [Fact]
        public void GetContent_CompleteFlow_WithAllServiceTypes_ReturnsCorrectOutput()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = new[]
            {
                CreateServiceStats("TransformationService", "Running", true, _testTransformationInfo),
                CreateServiceStats("PhoneService", "Running", true, null),
                CreateServiceStats("PCService", "Running", true, null)
            };
            var context = new ConsoleRenderContext { ServiceStats = stats };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(6); // 3 services + 3 empty lines
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
            result[2].Should().Be("Phone Tracking Status");
            result[3].Should().BeEmpty();
            result[4].Should().Be("PC Tracking Status");
            result[5].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_CompleteFlow_WithMissingFormatters_ShowsAppropriateMessages()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = new[]
            {
                CreateServiceStats("UnknownService1", "Running", true, _testEntity),
                CreateServiceStats("UnknownService2", "Running", true, _testEntity) // Use TestEntity instead of string
            };
            var context = new ConsoleRenderContext { ServiceStats = stats };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(6); // 2 services + 2 empty lines + 2 extra lines for headers
            result[0].Should().Contain("=== UnknownService1 (Running) ===");
            result[1].Should().Contain("[No formatter registered for TestEntity]");
            result[2].Should().BeEmpty();
            result[3].Should().Contain("=== UnknownService2 (Running) ===");
            result[4].Should().Contain("[No formatter registered for TestEntity]");
            result[5].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_CompleteFlow_WithMixedData_HandlesCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = new[]
            {
                CreateServiceStats("TransformationService", "Running", true, _testTransformationInfo),
                CreateServiceStats("PhoneService", "Stopped", false, null),
                CreateServiceStats("UnknownService", "Error", false, _testEntity)
            };
            var context = new ConsoleRenderContext { ServiceStats = stats };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(7); // 3 services + 3 empty lines + 1 extra line for header
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
            result[2].Should().Be("Phone Tracking Status");
            result[3].Should().BeEmpty();
            result[4].Should().Contain("=== UnknownService (Error) ===");
            result[5].Should().Contain("[No formatter registered for TestEntity]");
            result[6].Should().BeEmpty();
        }

        [Fact]
        public void GetContent_CompleteFlow_WithErrorConditions_RecoversGracefully()
        {
            // Arrange
            var provider = CreateProvider();
            var stats = new[]
            {
                null!, // Null service
                CreateServiceStats("TransformationService", "Running", true, _testTransformationInfo),
                CreateServiceStats("PhoneService", "Running", true, null)
            };
            var context = new ConsoleRenderContext { ServiceStats = stats };

            // Act
            var result = provider.GetContent(context);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4); // 2 valid services + 2 empty lines
            result[0].Should().Be("Transformation Engine Status");
            result[1].Should().BeEmpty();
            result[2].Should().Be("Phone Tracking Status");
            result[3].Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private MainStatusContentProvider CreateProvider()
        {
            return new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object);
        }

        private MainStatusContentProvider CreateProviderWithExternalEditor()
        {
            return new MainStatusContentProvider(
                _mockLogger.Object,
                _mockTransformationFormatter.Object,
                _mockPhoneFormatter.Object,
                _mockPCFormatter.Object,
                _mockShortcutManager.Object,
                _mockExternalEditorService.Object);
        }

        private static IServiceStats CreateServiceStats(string serviceName, string status, bool isHealthy, IFormattableObject? entity)
        {
            return new ServiceStats(
                serviceName: serviceName,
                status: status,
                currentEntity: entity,
                isHealthy: isHealthy,
                counters: new Dictionary<string, long>()
            );
        }

        #endregion
    }
}