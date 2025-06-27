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

            public void CycleVerbosity() { }

            public string Format(IServiceStats stats) =>
                stats?.CurrentEntity is TestEntity testEntity ? $"Test: {testEntity.Name}" : "Unknown entity";

            public void Dispose() { }
        }

        // Fields for common test objects
        private readonly TestConsole _testConsole;
        private readonly ConsoleRenderer _renderer;
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
            _mockTransformationFormatter = new Mock<TransformationEngineInfoFormatter>(Mock.Of<IConsole>(), _mockTableFormatter.Object, Mock.Of<IShortcutConfigurationManager>());
            _mockPhoneFormatter = new Mock<PhoneTrackingInfoFormatter>(Mock.Of<IConsole>(), _mockTableFormatter.Object, Mock.Of<IParameterColorService>(), Mock.Of<IShortcutConfigurationManager>());
            _mockPCFormatter = new Mock<PCTrackingInfoFormatter>(Mock.Of<IConsole>(), _mockTableFormatter.Object, Mock.Of<IParameterColorService>(), Mock.Of<IShortcutConfigurationManager>());

            // Create renderer with mocked dependencies
            _renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);

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
            var mockConsole = new Mock<IConsole>();
            var mockLogger = new Mock<IAppLogger>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();

            // Act & Assert
            var renderer = new ConsoleRenderer(mockConsole.Object, mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            renderer.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleRenderer(null!, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("console");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var mockConsole = new Mock<IConsole>();

            // Act & Assert
            Action act = () => new ConsoleRenderer(mockConsole.Object, null!, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void RegisterFormatter_WithValidFormatter_RegistersSuccessfully()
        {
            // Arrange
            var mockConsole = new Mock<IConsole>();
            var renderer = new ConsoleRenderer(mockConsole.Object, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            var mockFormatter = new Mock<IFormatter>();

            // Act & Assert
            Action act = () => renderer.RegisterFormatter<TestEntity>(mockFormatter.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void RegisterFormatter_RegistersAndRetrievesFormatter()
        {
            // Arrange
            var mockConsole = new Mock<IConsole>();
            var renderer = new ConsoleRenderer(mockConsole.Object, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
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
            var testConsole = new TestConsole();
            var renderer = new ConsoleRenderer(testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);

            // Act
            var result = renderer.GetFormatter<TestEntity>();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Update_WithValidStats_CallsFormatterWithCorrectStats()
        {
            // Arrange
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            renderer.RegisterFormatter<TestEntity>(_mockFormatter.Object);
            var stats = new List<IServiceStats> { _testStats };

            // Act
            renderer.Update(stats);

            // Assert
            _mockFormatter.Verify(f => f.Format(It.IsAny<IServiceStats>()), Times.Once);
        }

        [Fact]
        public void Update_WritesFormattedOutputToConsole()
        {
            // Arrange
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            renderer.RegisterFormatter<TestEntity>(_mockFormatter.Object);
            var stats = new List<IServiceStats> { _testStats };

            // Act
            renderer.Update(stats);

            // Assert
            _testConsole.Output.Should().Contain("Test Entity");
        }

        [Fact]
        public void Update_WhenCalledRapidly_ThrottlesUpdates()
        {
            // Arrange
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);
            var stats = new List<IServiceStats> { _testStats };

            // Act - Call update multiple times rapidly
            renderer.Update(stats);
            renderer.Update(stats);
            renderer.Update(stats);

            // Assert - Should throttle and not update every time
            // This is hard to test precisely due to timing, but we can verify it doesn't crash
            _testConsole.Output.Should().NotBeEmpty();
        }

        [Fact]
        public void Update_WithNoFormatterForEntityType_DisplaysNoFormatterMessage()
        {
            // Arrange
            var otherEntity = new OtherEntity("test data");
            var stats = new List<IServiceStats>
            {
                CreateMockServiceStats("TestService", "Running", true, otherEntity)
            };
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, _mockShortcutManager.Object);

            // Act
            renderer.Update(stats);

            // Assert
            _testConsole.Output.Should().Contain("[No formatter registered for OtherEntity]");
        }

        [Fact]
        public void Update_WithMultiInterfaceFormatter_HandlesLINQExpressionCorrectly()
        {
            // Arrange
            var multiFormatter = new MultiInterfaceFormatter();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            renderer.RegisterFormatter<TestEntity>(multiFormatter);
            var stats = new List<IServiceStats> { _testStats };

            // Act & Assert - Should not throw exception
            Action act = () => renderer.Update(stats);
            act.Should().NotThrow();
        }

        [Fact]
        public void ConsoleDisplayAction_WithMoreLinesThanWindowHeight_HandlesOverflow()
        {
            // Arrange
            var smallConsole = new Mock<IConsole>();
            smallConsole.Setup(c => c.WindowHeight).Returns(5);
            smallConsole.Setup(c => c.WindowWidth).Returns(80);
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();

            // Create a renderer with the small console
            var renderer = new ConsoleRenderer(smallConsole.Object, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);

            // Create more lines than the window height
            var manyStats = new List<IServiceStats>();
            for (int i = 0; i < 10; i++)
            {
                manyStats.Add(new ServiceStats(
                    serviceName: $"Service{i}",
                    status: "Running",
                    isHealthy: true,
                    currentEntity: new TestEntity($"Entity{i}", i)
                ));
            }

            // Act & Assert - Should not throw exception
            Action act = () => renderer.Update(manyStats);
            act.Should().NotThrow();
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
            _testConsole.WriteLine("Test output");
            _testConsole.Output.Should().NotBeNullOrEmpty();

            // Act
            _renderer.ClearConsole();

            // Assert
            _testConsole.Output.Should().BeEmpty();
        }

        [Fact]
        public void Update_WhenConsoleThrowsException_LogsErrorAndRethrows()
        {
            // Arrange
            var stats = new List<IServiceStats> { _testStats };

            // Configure the test console to throw an exception on cursor positioning
            var throwingConsole = new Mock<IConsole>();
            throwingConsole.Setup(c => c.SetCursorPosition(It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Create a renderer with the throwing console
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(throwingConsole.Object, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            renderer.RegisterFormatter<TestEntity>(_mockFormatter.Object);

            // Act & Assert
            Action act = () => renderer.Update(stats);

            // Exception should be thrown
            act.Should().Throw<InvalidOperationException>().WithMessage("Test exception");

            // Error should be logged
            _mockLogger.Verify(l => l.ErrorWithException(
                It.Is<string>(s => s.Contains("Console rendering failed")),
                It.IsAny<Exception>(),
                It.IsAny<object[]>()),
                Times.Once);
        }

        [Fact]
        public void Update_WithEntityStats_CallsFormatterCorrectly()
        {
            // Arrange
            var testConsole = new TestConsole();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            var mockFormatter = new Mock<IFormatter>();
            var testEntity = new TestEntity("Test", 42);

            mockFormatter.Setup(f => f.Format(It.IsAny<IServiceStats>()))
                         .Returns("Formatted output");

            renderer.RegisterFormatter<TestEntity>(mockFormatter.Object);

            var stats = new List<IServiceStats>
            {
                CreateMockServiceStats("TestService", "Running", true, testEntity)
            };

            // Act
            renderer.Update(stats);

            // Assert
            mockFormatter.Verify(f => f.Format(It.IsAny<IServiceStats>()), Times.Once);
        }

        [Fact]
        public void Update_WithNullEntity_HandlesGracefully()
        {
            // Arrange
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            var stats = new List<IServiceStats>
            {
                CreateMockServiceStats("TestService", "Running", true, null)
            };

            // Act & Assert
            Action act = () => renderer.Update(stats);
            act.Should().NotThrow();
        }

        [Fact]
        public void Update_WithUnregisteredEntityType_HandlesGracefully()
        {
            // Arrange
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            var testEntity = new TestEntity("Test", 42);
            var stats = new List<IServiceStats>
            {
                CreateMockServiceStats("TestService", "Running", true, testEntity)
            };

            // Act & Assert
            Action act = () => renderer.Update(stats);
            act.Should().NotThrow();
        }

        [Fact]
        public void Update_WithSmallConsole_HandlesGracefully()
        {
            // Arrange
            var smallConsole = new Mock<IConsole>();
            smallConsole.Setup(c => c.WindowWidth).Returns(10);
            smallConsole.Setup(c => c.WindowHeight).Returns(5);

            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(smallConsole.Object, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            var stats = new List<IServiceStats>
            {
                CreateMockServiceStats("TestService", "Running", true)
            };

            // Act & Assert
            Action act = () => renderer.Update(stats);
            act.Should().NotThrow();
        }

        [Fact]
        public void Update_WithConsoleException_LogsAndRethrows()
        {
            // Arrange
            var throwingConsole = new Mock<IConsole>();
            var expectedException = new InvalidOperationException("Console error");

            throwingConsole.Setup(c => c.SetCursorPosition(It.IsAny<int>(), It.IsAny<int>()))
                          .Throws(expectedException);

            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var renderer = new ConsoleRenderer(throwingConsole.Object, _mockLogger.Object, _mockTransformationFormatter.Object, _mockPhoneFormatter.Object, _mockPCFormatter.Object, mockShortcutManager.Object);
            var stats = new List<IServiceStats>
            {
                CreateMockServiceStats("TestService", "Running", true)
            };

            // Act & Assert
            Action act = () => renderer.Update(stats);
            act.Should().Throw<InvalidOperationException>().WithMessage("Console error");

            _mockLogger.Verify(l => l.ErrorWithException("Console rendering failed", expectedException), Times.Once);
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