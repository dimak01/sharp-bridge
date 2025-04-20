using System;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConsoleAppLoggerTests
    {
        private readonly Mock<IConsole> _mockConsole;
        private readonly ConsoleAppLogger _logger;

        public ConsoleAppLoggerTests()
        {
            // Setup mock console
            _mockConsole = new Mock<IConsole>();
            _logger = new ConsoleAppLogger(_mockConsole.Object);
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleAppLogger(null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("console");
        }

        [Fact]
        public void Debug_WritesToConsole_WithCorrectFormat()
        {
            // Act
            _logger.Debug("Test message {0}", 123);

            // Assert
            VerifyConsoleWriteLine("DEBUG", "Test message 123");
        }

        [Fact]
        public void Info_WritesToConsole_WithCorrectFormat()
        {
            // Act
            _logger.Info("Test message {0}", 123);

            // Assert
            VerifyConsoleWriteLine("INFO", "Test message 123");
        }

        [Fact]
        public void Warning_WritesToConsole_WithCorrectFormat()
        {
            // Act
            _logger.Warning("Test message {0}", 123);

            // Assert
            VerifyConsoleWriteLine("WARN", "Test message 123");
        }

        [Fact]
        public void Error_WritesToConsole_WithCorrectFormat()
        {
            // Act
            _logger.Error("Test message {0}", 123);

            // Assert
            VerifyConsoleWriteLine("ERROR", "Test message 123");
        }

        [Fact]
        public void ErrorWithException_WritesToConsole_WithExceptionDetails()
        {
            // Arrange
            var testException = new Exception("Test exception message");

            // Act
            _logger.ErrorWithException("Test message {0}", testException, 123);

            // Assert
            _mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => 
                s.Contains("[ERROR]") && 
                s.Contains("Test message 123") && 
                s.Contains("Exception: Test exception message"))), 
                Times.Once);
        }

        [Fact]
        public void ErrorWithException_WithNullException_WritesMessageWithoutExceptionDetails()
        {
            // Act
            _logger.ErrorWithException("Test message {0}", null, 123);

            // Assert
            VerifyConsoleWriteLine("ERROR", "Test message 123");
        }

        private void VerifyConsoleWriteLine(string level, string message)
        {
            _mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => 
                s.Contains($"[{level}]") && 
                s.Contains(message))), 
                Times.Once);
        }
    }
} 