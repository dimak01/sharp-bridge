using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Services
{
    /// <summary>
    /// Unit tests for WindowsFirewallAnalyzer.
    /// Demonstrates how clean and testable the analyzer is now that all COM/P/Invoke dependencies are mocked.
    /// </summary>
    public class WindowsFirewallAnalyzerTests
    {
        private readonly Mock<IFirewallEngine> _mockFirewallEngine;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly WindowsFirewallAnalyzer _analyzer;

        public WindowsFirewallAnalyzerTests()
        {
            _mockFirewallEngine = new Mock<IFirewallEngine>();
            _mockLogger = new Mock<IAppLogger>();
            _analyzer = new WindowsFirewallAnalyzer(_mockFirewallEngine.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullFirewallEngine_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WindowsFirewallAnalyzer(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WindowsFirewallAnalyzer(_mockFirewallEngine.Object, null!));
        }

        [Fact]
        public void AnalyzeFirewallRules_WithFirewallDisabled_ReturnsAllowed()
        {
            // Arrange
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(false);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2); // Private
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(false);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 17, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule>());

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Empty(result.RelevantRules);
            Assert.Equal("Private", result.ProfileName);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithNoRules_UsesDefaultAction()
        {
            // Arrange
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2); // Private
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(true); // Allow inbound
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(false); // Block outbound
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 17, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule>());

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.True(result.IsAllowed); // Should use default inbound action (Allow)
            Assert.Empty(result.RelevantRules);
            Assert.True(result.DefaultActionAllowed);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithBlockingRules_ReturnsBlocked()
        {
            // Arrange
            var blockingRule = new FirewallRule
            {
                Name = "Block SharpBridge UDP",
                Action = "Block",
                IsEnabled = true,
                Direction = "Inbound",
                Protocol = "UDP",
                LocalPort = "28964",
                RemoteAddress = "192.168.1.100"
            };

            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(false);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 17, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule> { blockingRule });

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.False(result.IsAllowed); // Blocking rule takes precedence
            Assert.Single(result.RelevantRules);
            Assert.Equal("Block SharpBridge UDP", result.RelevantRules[0].Name);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithAllowingRules_ReturnsAllowed()
        {
            // Arrange
            var allowingRule = new FirewallRule
            {
                Name = "Allow SharpBridge UDP",
                Action = "Allow",
                IsEnabled = true,
                Direction = "Inbound",
                Protocol = "UDP",
                LocalPort = "28964",
                RemoteAddress = "192.168.1.100"
            };

            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(false); // Default block
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(false);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 17, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule> { allowingRule });

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.True(result.IsAllowed); // Allowing rule overrides default block
            Assert.Single(result.RelevantRules);
            Assert.Equal("Allow SharpBridge UDP", result.RelevantRules[0].Name);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithOutboundConnection_DetectsCorrectDirection()
        {
            // Arrange
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(true); // Outbound default
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(2, 17, 2, "192.168.1.100", "21412", null))
                .Returns(new List<FirewallRule>());

            // Act
            var result = _analyzer.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.True(result.IsAllowed);
            _mockFirewallEngine.Verify(x => x.GetRelevantRules(2, 17, 2, "192.168.1.100", "21412", null), Times.Once);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithDifferentProtocols_HandlesCorrectly()
        {
            // Arrange
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 6, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule>());

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "TCP");

            // Assert
            Assert.True(result.IsAllowed);
            _mockFirewallEngine.Verify(x => x.GetRelevantRules(1, 6, 2, "192.168.1.100", "21412", "28964"), Times.Once);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithException_ReturnsBlocked()
        {
            // Arrange
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Throws(new Exception("COM Error"));

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.False(result.IsAllowed); // Fail-safe: block on error
            Assert.Empty(result.RelevantRules);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithLoopbackInterface_HandlesCorrectly()
        {
            // Arrange
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("localhost")).Returns(1); // Loopback
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(1)).Returns(2); // Private for loopback
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(false);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 17, 2, "localhost", "21412", "28964"))
                .Returns(new List<FirewallRule>());

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "localhost", "21412", "UDP");

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal("Private", result.ProfileName);
        }

        [Fact]
        public void AnalyzeFirewallRules_WithMultipleRules_AppliesPrecedenceCorrectly()
        {
            // Arrange
            var allowRule = new FirewallRule
            {
                Name = "Allow Rule",
                Action = "Allow",
                IsEnabled = true,
                Direction = "Inbound",
                Protocol = "UDP"
            };

            var blockRule = new FirewallRule
            {
                Name = "Block Rule",
                Action = "Block",
                IsEnabled = true,
                Direction = "Inbound",
                Protocol = "UDP"
            };

            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(2, 2)).Returns(false);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, 17, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule> { allowRule, blockRule });

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", "UDP");

            // Assert
            Assert.False(result.IsAllowed); // Block takes precedence over Allow
            Assert.Equal(2, result.RelevantRules.Count);
        }

        [Theory]
        [InlineData("UDP", 17)]
        [InlineData("TCP", 6)]
        [InlineData("ANY", 256)]
        [InlineData("unknown", 256)]
        public void GetProtocolValue_WithDifferentProtocols_ReturnsCorrectValues(string protocol, int expectedValue)
        {
            // This tests the private method via reflection or we can make it internal for testing
            // For now, we'll test it indirectly through the public interface
            _mockFirewallEngine.Setup(x => x.GetFirewallState()).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetBestInterface("192.168.1.100")).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetInterfaceProfile(2)).Returns(2);
            _mockFirewallEngine.Setup(x => x.GetDefaultAction(1, 2)).Returns(true);
            _mockFirewallEngine.Setup(x => x.GetRelevantRules(1, expectedValue, 2, "192.168.1.100", "21412", "28964"))
                .Returns(new List<FirewallRule>());

            // Act
            var result = _analyzer.AnalyzeFirewallRules("28964", "192.168.1.100", "21412", protocol);

            // Assert
            Assert.True(result.IsAllowed);
            _mockFirewallEngine.Verify(x => x.GetRelevantRules(1, expectedValue, 2, "192.168.1.100", "21412", "28964"), Times.Once);
        }
    }
}















