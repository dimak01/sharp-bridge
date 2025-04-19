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
        
        // Fields for common test objects
        private TestConsole _testConsole;
        private ConsoleRenderer _renderer;
        private Mock<IFormatter<TestEntity>> _mockFormatter;
        private TestEntity _testEntity;
        private ServiceStats<TestEntity> _testStats;
        
        /// <summary>
        /// Setup for each test
        /// </summary>
        public ConsoleRendererTests()
        {
            // Create test console to capture output
            _testConsole = new TestConsole();
            
            // Create and configure the formatter mock
            _mockFormatter = new Mock<IFormatter<TestEntity>>();
            
            // Set up CurrentVerbosity property
            _mockFormatter.Setup(f => f.CurrentVerbosity)
                .Returns(VerbosityLevel.Normal);
                
            // Set up CycleVerbosity method
            _mockFormatter.Setup(f => f.CycleVerbosity())
                .Verifiable();
            
            // The Format method with just the entity parameter
            _mockFormatter.Setup(f => f.Format(It.IsAny<TestEntity>()))
                .Returns<TestEntity>(entity => $"Test Entity: {entity.Name}, Value: {entity.Value}");
                
            // The Format method with entity and verbosity parameters
            _mockFormatter.Setup(f => f.Format(It.IsAny<TestEntity>(), It.IsAny<VerbosityLevel>()))
                .Returns<TestEntity, VerbosityLevel>((entity, verbosity) => 
                    $"Test Entity: {entity.Name}, Value: {entity.Value}, Verbosity: {verbosity}");
            
            // Create test entity and stats
            _testEntity = new TestEntity("Test", 42);
            
            var counters = new Dictionary<string, long>
            {
                ["Counter1"] = 10,
                ["Counter2"] = 20
            };
            
            _testStats = new ServiceStats<TestEntity>(
                serviceName: "TestService",
                status: "Running",
                currentEntity: _testEntity,
                counters: counters
            );
            
            // Create the renderer with our test console and register our formatter
            _renderer = new ConsoleRenderer(_testConsole);
            _renderer.RegisterFormatter(_mockFormatter.Object);
        }
        
        /// <summary>
        /// Cleanup after tests
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
        
        [Fact]
        public void RegisterFormatter_RegistersAndRetrievesFormatter()
        {
            // Arrange
            var formatter = new Mock<IFormatter<TestEntity>>().Object;
            
            // Act
            _renderer.RegisterFormatter(formatter);
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
            var renderer = new ConsoleRenderer(testConsole);
            
            // Act
            var result = renderer.GetFormatter<TestEntity>();
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public void Update_WithValidStats_CallsFormatterWithCorrectEntity()
        {
            // Arrange
            var stats = new List<IServiceStats<TestEntity>> { _testStats };
            
            // Act
            _renderer.Update(stats);
            
            // Assert
            _mockFormatter.Verify(f => f.Format(_testEntity), Times.Once);
        }
        
        [Fact]
        public void Update_WritesFormattedOutputToConsole()
        {
            // Arrange
            var stats = new List<IServiceStats<TestEntity>> { _testStats };
            
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
    }
} 