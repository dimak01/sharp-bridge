using System;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Interfaces;
using Xunit;

namespace SharpBridge.Tests.Models
{
    public class EventArgsTests
    {
        #region FileChangeEventArgs Tests

        [Fact]
        public void FileChangeEventArgs_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var filePath = "test.json";

            // Act
            var eventArgs = new FileChangeEventArgs(filePath);

            // Assert
            eventArgs.FilePath.Should().Be(filePath);
            eventArgs.ChangeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void FileChangeEventArgs_ChangeTime_PropertyGetter_ReturnsCorrectValue()
        {
            // Arrange
            var filePath = "config.json";

            // Act
            var eventArgs = new FileChangeEventArgs(filePath);
            var actualChangeTime = eventArgs.ChangeTime; // This covers the ChangeTime property getter

            // Assert
            actualChangeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region RulesChangedEventArgs Tests

        [Fact]
        public void RulesChangedEventArgs_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var filePath = "rules.json";

            // Act
            var eventArgs = new RulesChangedEventArgs(filePath);

            // Assert
            eventArgs.FilePath.Should().Be(filePath);
            eventArgs.ChangeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RulesChangedEventArgs_Constructor_WithNullFilePath_HandlesCorrectly()
        {
            // Act - This covers the null handling branch
            var eventArgs = new RulesChangedEventArgs(null!);

            // Assert - Constructor handles null by setting to empty string
            eventArgs.FilePath.Should().Be(string.Empty);
            eventArgs.ChangeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RulesChangedEventArgs_ChangeTime_PropertyGetter_ReturnsCorrectValue()
        {
            // Arrange
            var filePath = "config.json";

            // Act
            var eventArgs = new RulesChangedEventArgs(filePath);
            var actualChangeTime = eventArgs.ChangeTime; // This covers the ChangeTime property getter

            // Assert
            actualChangeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region ServiceStats Coverage Tests

        [Fact]
        public void ServiceStats_Constructor_InitializesAllProperties()
        {
            // Arrange & Act
            var stats = new ServiceStats("TestService", "Running", null);

            // Assert - Cover all properties to ensure they're tested
            stats.ServiceName.Should().Be("TestService");
            stats.Status.Should().Be("Running");
            stats.CurrentEntity.Should().BeNull();
            stats.IsHealthy.Should().BeTrue(); // Default value
            stats.LastSuccessfulOperation.Should().Be(default(DateTime)); // Default value
            stats.LastError.Should().BeNull(); // Default value
            stats.Counters.Should().NotBeNull(); // Should be initialized
            stats.Counters.Should().BeEmpty(); // Should start empty
        }

        [Fact]
        public void ServiceStats_Constructor_WithAllParameters_SetsAllProperties()
        {
            // Arrange
            var serviceName = "TestService";
            var status = "Running";
            var isHealthy = false;
            var lastSuccessfulOperation = DateTime.Now;
            var lastError = "Test error";
            var counters = new System.Collections.Generic.Dictionary<string, long>
            {
                ["TestCounter"] = 42
            };

            // Act
            var stats = new ServiceStats(serviceName, status, null, isHealthy, lastSuccessfulOperation, lastError, counters);

            // Assert
            stats.ServiceName.Should().Be(serviceName);
            stats.Status.Should().Be(status);
            stats.CurrentEntity.Should().BeNull();
            stats.IsHealthy.Should().Be(isHealthy);
            stats.LastSuccessfulOperation.Should().Be(lastSuccessfulOperation);
            stats.LastError.Should().Be(lastError);
            stats.Counters.Should().BeSameAs(counters);
            stats.Counters["TestCounter"].Should().Be(42);
        }

        #endregion
    }
}