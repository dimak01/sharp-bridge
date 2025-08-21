using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConsoleRendererTests : IDisposable
    {
        // Test implementation of IFormattableObject for testing
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

        // Another test entity class for testing missing formatter scenarios
        public class OtherEntity : IFormattableObject
        {
            public string Data { get; set; }

            public OtherEntity(string data)
            {
                Data = data;
            }
        }

        // Special test formatter that implements multiple interfaces to test LINQ branch coverage
        public class MultiInterfaceFormatter : IFormatter, IDisposable
        {
            public VerbosityLevel CurrentVerbosity => VerbosityLevel.Normal;

            public VerbosityLevel CycleVerbosity() { return VerbosityLevel.Normal; }

            public string Format(IServiceStats stats) =>
                stats?.CurrentEntity is TestEntity testEntity ? $"Test: {testEntity.Name}" : "Unknown entity";

            public void Dispose() { }
        }

        // Fields for common test objects
        private readonly TestConsole _testConsole;
        private readonly MainStatusContentProvider _renderer;
        private readonly Mock<IFormatter> _mockFormatter;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<ITableFormatter> _mockTableFormatter;
        private readonly Mock<TransformationEngineInfoFormatter> _mockTransformationFormatter;
        private readonly Mock<PhoneTrackingInfoFormatter> _mockPhoneFormatter;
        private readonly Mock<PCTrackingInfoFormatter> _mockPCFormatter;
        private readonly Mock<IShortcutConfigurationManager> _mockShortcutManager;
        private readonly TestEntity _testEntity;
        private readonly ServiceStats _testStats;

        /// <summary>
        /// Setup test dependencies
        /// </summary>
        public ConsoleRendererTests()
        {
            _testConsole = new TestConsole();
            _mockFormatter = new Mock<IFormatter>();
            _mockFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                         .Returns<IServiceStats>(stats => $"Test Entity: {stats.ServiceName}");
            _mockLogger = new Mock<IAppLogger>();
            _mockTableFormatter = new Mock<ITableFormatter>();
            _mockShortcutManager = new Mock<IShortcutConfigurationManager>();

            // Setup shortcut manager defaults
            _mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+X");

            // Create formatter mocks with proper constructor parameters
            var userPreferences = new UserPreferences();
            _mockTransformationFormatter = new Mock<TransformationEngineInfoFormatter>(Mock.Of<IConsole>(), _mockTableFormatter.Object, Mock.Of<IShortcutConfigurationManager>(), userPreferences);
            _mockPhoneFormatter = new Mock<PhoneTrackingInfoFormatter>(Mock.Of<IConsole>(), _mockTableFormatter.Object, Mock.Of<IParameterColorService>(), Mock.Of<IShortcutConfigurationManager>(), userPreferences);
            _mockPCFormatter = new Mock<PCTrackingInfoFormatter>(Mock.Of<IConsole>(), _mockTableFormatter.Object, Mock.Of<IParameterColorService>(), Mock.Of<IShortcutConfigurationManager>(), userPreferences, Mock.Of<IParameterTableConfigurationManager>());

            // Create renderer with mocked dependencies
            _renderer = new MainStatusContentProvider(_mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);

            // Register the test formatter for TestEntity
            _renderer.RegisterFormatter<TestEntity>(_mockFormatter.Object);

            // Setup test entity and stats
            _testEntity = new TestEntity("Test Data", 42);
            _testStats = (ServiceStats)CreateMockServiceStats("TestService", "Running", true, _testEntity);
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose for TestConsole
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange
            var mockLogger = new Mock<IAppLogger>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();

            // Act & Assert
            var renderer = new MainStatusContentProvider(mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            renderer.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new MainStatusContentProvider(_mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            // Act & Assert
            Action act = () => new MainStatusContentProvider(null!, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void RegisterFormatter_WithValidFormatter_RegistersSuccessfully()
        {
            // Arrange
            var renderer = new MainStatusContentProvider(_mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            var mockFormatter = new Mock<IFormatter>();

            // Act & Assert
            Action act = () => renderer.RegisterFormatter<TestEntity>(mockFormatter.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void RegisterFormatter_RegistersAndRetrievesFormatter()
        {
            // Arrange
            var renderer = new MainStatusContentProvider(_mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            var mockFormatter = new Mock<IFormatter>();

            // Act
            renderer.RegisterFormatter<TestEntity>(mockFormatter.Object);
            var retrievedFormatter = renderer.GetFormatter<TestEntity>();

            // Assert
            retrievedFormatter.Should().BeSameAs(mockFormatter.Object);
        }

        [Fact]
        public void GetFormatter_WithUnregisteredType_ReturnsNull()
        {
            // Arrange
            // Create a new renderer without registering formatters
            var renderer = new MainStatusContentProvider(_mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);

            // Act
            var result = renderer.GetFormatter<TestEntity>();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetFormatter_ReturnsCorrectFormatter()
        {
            // Act
            var formatter = _renderer.GetFormatter<TestEntity>();

            // Assert
            formatter.Should().BeSameAs(_mockFormatter.Object);
        }

        [Fact]
        public void ClearConsole_ClearsTestConsoleOutput()
        {
            // Arrange - Add some output
            _testConsole.WriteLines(new[] { "Test output" });
            _testConsole.Output.Should().NotBeNullOrEmpty();

            // Act
            _renderer.ClearConsole();

            // Assert
            _testConsole.Output.Should().BeEmpty();
        }

        /// <summary>
        /// Helper method to create mock service stats
        /// </summary>
        private static IServiceStats CreateMockServiceStats(string serviceName, string status, bool isHealthy, IFormattableObject? entity = null)
        {
            return new ServiceStats(
                serviceName: serviceName,
                status: status,
                currentEntity: entity,
                isHealthy: isHealthy,
                counters: new Dictionary<string, long>()
            );
        }
    }
}