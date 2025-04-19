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
            
            public string Format(IFormattableObject entity) => 
                entity is TestEntity testEntity ? $"Test: {testEntity.Name}" : "Unknown entity";
            
            public string Format(IFormattableObject entity, VerbosityLevel verbosity) => Format(entity);
            
            public void Dispose() { }
        }
        
        // Fields for common test objects
        private TestConsole _testConsole;
        private ConsoleRenderer _renderer;
        private Mock<IFormatter> _mockFormatter;
        private Mock<IAppLogger> _mockLogger;
        private TestEntity _testEntity;
        private ServiceStats _testStats;
        
        /// <summary>
        /// Setup for each test
        /// </summary>
        public ConsoleRendererTests()
        {
            // Create test console to capture output
            _testConsole = new TestConsole();
            
            // Create and configure the logger mock
            _mockLogger = new Mock<IAppLogger>();
            
            // Create and configure the formatter mock
            _mockFormatter = new Mock<IFormatter>();
            
            // Set up CurrentVerbosity property
            _mockFormatter.Setup(f => f.CurrentVerbosity)
                .Returns(VerbosityLevel.Normal);
                
            // Set up CycleVerbosity method
            _mockFormatter.Setup(f => f.CycleVerbosity())
                .Verifiable();
            
            // The Format method with just the entity parameter
            _mockFormatter.Setup(f => f.Format(It.IsAny<IFormattableObject>()))
                .Returns<IFormattableObject>(entity => {
                    if (entity is TestEntity testEntity)
                        return $"Test Entity: {testEntity.Name}, Value: {testEntity.Value}";
                    return "Unknown entity";
                });
                
            // The Format method with entity and verbosity parameters
            _mockFormatter.Setup(f => f.Format(It.IsAny<IFormattableObject>(), It.IsAny<VerbosityLevel>()))
                .Returns<IFormattableObject, VerbosityLevel>((entity, verbosity) => {
                    if (entity is TestEntity testEntity)
                        return $"Test Entity: {testEntity.Name}, Value: {testEntity.Value}, Verbosity: {verbosity}";
                    return $"Unknown entity, Verbosity: {verbosity}";
                });
            
            // Create test entity and stats
            _testEntity = new TestEntity("Test", 42);
            
            var counters = new Dictionary<string, long>
            {
                ["Counter1"] = 10,
                ["Counter2"] = 20
            };
            
            _testStats = new ServiceStats(
                serviceName: "TestService",
                status: "Running",
                currentEntity: _testEntity,
                counters: counters
            );
            
            // Create the renderer with our test console and register our formatter
            _renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object);
            _renderer.RegisterFormatter<TestEntity>(_mockFormatter.Object);
        }
        
        /// <summary>
        /// Cleanup after tests
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
        
        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleRenderer(null, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("console");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleRenderer(_testConsole, null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }
        
        [Fact]
        public void RegisterFormatter_RegistersAndRetrievesFormatter()
        {
            // Arrange
            var formatter = new Mock<IFormatter>().Object;
            
            // Act
            _renderer.RegisterFormatter<TestEntity>(formatter);
            var retrievedFormatter = _renderer.GetFormatter<TestEntity>();
            
            // Assert
            retrievedFormatter.Should().NotBeNull();
            retrievedFormatter.Should().BeSameAs(formatter);
        }
        
        [Fact]
        public void GetFormatter_WithUnregisteredType_ReturnsNull()
        {
            // Arrange
            // Create a new renderer without registering formatters
            var testConsole = new TestConsole();
            var renderer = new ConsoleRenderer(testConsole, _mockLogger.Object);
            
            // Act
            var result = renderer.GetFormatter<TestEntity>();
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public void Update_WithValidStats_CallsFormatterWithCorrectEntity()
        {
            // Arrange
            var stats = new List<IServiceStats> { _testStats };
            
            // Act
            _renderer.Update(stats);
            
            // Assert
            _mockFormatter.Verify(f => f.Format(_testEntity), Times.Once);
        }
        
        [Fact]
        public void Update_WritesFormattedOutputToConsole()
        {
            // Arrange
            var stats = new List<IServiceStats> { _testStats };
            
            // Act
            _renderer.Update(stats);
            
            // Assert
            var output = _testConsole.Output;
            output.Should().NotBeNullOrEmpty();
            output.Should().Contain($"Test Entity: {_testEntity.Name}, Value: {_testEntity.Value}");
            output.Should().Contain("TestService");
            output.Should().Contain("Running");
        }
        
        [Fact]
        public void Update_WhenCalledRapidly_ThrottlesUpdates()
        {
            // Arrange
            var stats = new List<IServiceStats> { _testStats };
            
            // Act - Call update twice in quick succession
            _renderer.Update(stats);
            _renderer.Update(stats); // This should be throttled
            
            // Assert - Formatting should only happen once due to throttling
            _mockFormatter.Verify(f => f.Format(_testEntity), Times.Once);
        }
        
        [Fact]
        public void Update_WithNoFormatterForEntityType_DisplaysNoFormatterMessage()
        {
            // Arrange
            var otherEntity = new OtherEntity("test data");
            var statsWithNoFormatter = new ServiceStats(
                serviceName: "OtherService",
                status: "Running",
                currentEntity: otherEntity,
                counters: new Dictionary<string, long>()
            );
            
            var stats = new List<IServiceStats> { statsWithNoFormatter };
            
            // We need a renderer that doesn't have formatters for OtherEntity
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object);
            
            // Act
            renderer.Update(stats);
            
            // Assert
            _testConsole.Output.Should().Contain($"[No formatter registered for {typeof(OtherEntity).Name}]");
        }
        
        [Fact]
        public void Update_WithMultiInterfaceFormatter_HandlesLINQExpressionCorrectly()
        {
            // Arrange
            var multiFormatter = new MultiInterfaceFormatter();
            var entity = new TestEntity("Test", 42);
            var stats = new ServiceStats(
                serviceName: "TestService",
                status: "Running",
                currentEntity: entity,
                counters: new Dictionary<string, long>()
            );
            
            // Register our special multi-interface formatter
            var renderer = new ConsoleRenderer(_testConsole, _mockLogger.Object);
            renderer.RegisterFormatter<TestEntity>(multiFormatter);
            
            // Act - This should exercise the LINQ expression
            renderer.Update(new List<IServiceStats> { stats });
            
            // Assert - If it gets here without exception, the LINQ worked
            _testConsole.Output.Should().Contain("Test: Test");
        }
        
        [Fact]
        public void ConsoleDisplayAction_WithMoreLinesThanWindowHeight_HandlesOverflow()
        {
            // Arrange
            // Create a console with a small window height
            var smallConsole = new Mock<IConsole>();
            smallConsole.Setup(c => c.WindowHeight).Returns(5);
            smallConsole.Setup(c => c.WindowWidth).Returns(80);
            
            // Create a renderer with the small console
            var renderer = new ConsoleRenderer(smallConsole.Object, _mockLogger.Object);
            
            // Create more lines than the window height
            var manyStats = new List<IServiceStats>();
            for (int i = 0; i < 10; i++)
            {
                manyStats.Add(new ServiceStats(
                    serviceName: $"Service{i}",
                    status: "Running",
                    currentEntity: new TestEntity($"Entity{i}", i),
                    counters: new Dictionary<string, long> { [$"Counter{i}"] = i }
                ));
            }
            
            // Act
            // This will call ConsoleDisplayAction with more lines than the window height
            // The method should handle this by only displaying up to the window height
            Action act = () => renderer.Update(manyStats);
            
            // Assert - should not throw exception
            act.Should().NotThrow();
            
            // Verify SetCursorPosition was called correctly and didn't try to 
            // position beyond the window height
            smallConsole.Verify(c => c.SetCursorPosition(It.IsAny<int>(), 
                It.Is<int>(i => i < smallConsole.Object.WindowHeight)), 
                Times.AtLeast(1));
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
            var renderer = new ConsoleRenderer(throwingConsole.Object, _mockLogger.Object);
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
    }
} 