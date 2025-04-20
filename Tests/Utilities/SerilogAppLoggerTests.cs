using System;
using FluentAssertions;
using Moq;
using Serilog;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class SerilogAppLoggerTests
    {
        private readonly Mock<ILogger> _mockSerilogLogger;
        private readonly SerilogAppLogger _logger;

        public SerilogAppLoggerTests()
        {
            // Setup mock Serilog logger
            _mockSerilogLogger = new Mock<ILogger>();
            
            // Setup method chaining for fluent interface
            _mockSerilogLogger.Setup(l => l.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(_mockSerilogLogger.Object);
                
            _logger = new SerilogAppLogger(_mockSerilogLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new SerilogAppLogger(null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Debug_CallsSerilogDebug()
        {
            // Arrange
            string message = "Test debug message {0}";
            object[] args = new object[] { 123 };
            
            // Act
            _logger.Debug(message, args);
            
            // Assert
            _mockSerilogLogger.Verify(l => l.Debug(message, args), Times.Once);
        }
        
        [Fact]
        public void Info_CallsSerilogInformation()
        {
            // Arrange
            string message = "Test info message {0}";
            object[] args = new object[] { 123 };
            
            // Act
            _logger.Info(message, args);
            
            // Assert
            _mockSerilogLogger.Verify(l => l.Information(message, args), Times.Once);
        }
        
        [Fact]
        public void Warning_CallsSerilogWarning()
        {
            // Arrange
            string message = "Test warning message {0}";
            object[] args = new object[] { 123 };
            
            // Act
            _logger.Warning(message, args);
            
            // Assert
            _mockSerilogLogger.Verify(l => l.Warning(message, args), Times.Once);
        }
        
        [Fact]
        public void Error_CallsSerilogError()
        {
            // Arrange
            string message = "Test error message {0}";
            object[] args = new object[] { 123 };
            
            // Act
            _logger.Error(message, args);
            
            // Assert
            _mockSerilogLogger.Verify(l => l.Error(message, args), Times.Once);
        }
        
        [Fact]
        public void ErrorWithException_CallsSerilogErrorWithException()
        {
            // Arrange
            string message = "Test exception message {0}";
            Exception exception = new Exception("Test exception");
            object[] args = new object[] { 123 };
            
            // Act
            _logger.ErrorWithException(message, exception, args);
            
            // Assert
            _mockSerilogLogger.Verify(l => l.Error(exception, message, args), Times.Once);
        }
    }
} 