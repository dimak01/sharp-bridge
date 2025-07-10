using SharpBridge.Services;
using SharpBridge.Interfaces;
using Xunit;
using System;

namespace SharpBridge.Tests.Services
{
    public class SimpleRecoveryPolicyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidTimeSpan_SetsDelayCorrectly()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromSeconds(5);

            // Act
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Assert
            var actualDelay = policy.GetNextDelay();
            Assert.Equal(expectedDelay, actualDelay);
        }

        [Fact]
        public void Constructor_WithZeroTimeSpan_SetsDelayCorrectly()
        {
            // Arrange
            var expectedDelay = TimeSpan.Zero;

            // Act
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Assert
            var actualDelay = policy.GetNextDelay();
            Assert.Equal(expectedDelay, actualDelay);
        }

        [Fact]
        public void Constructor_WithNegativeTimeSpan_SetsDelayCorrectly()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromSeconds(-1);

            // Act
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Assert
            var actualDelay = policy.GetNextDelay();
            Assert.Equal(expectedDelay, actualDelay);
        }

        [Fact]
        public void Constructor_WithLargeTimeSpan_SetsDelayCorrectly()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromHours(24);

            // Act
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Assert
            var actualDelay = policy.GetNextDelay();
            Assert.Equal(expectedDelay, actualDelay);
        }

        [Fact]
        public void Constructor_WithMillisecondsTimeSpan_SetsDelayCorrectly()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromMilliseconds(500);

            // Act
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Assert
            var actualDelay = policy.GetNextDelay();
            Assert.Equal(expectedDelay, actualDelay);
        }

        #endregion

        #region GetNextDelay Tests

        [Fact]
        public void GetNextDelay_AlwaysReturnsSameDelay()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromSeconds(2);
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var actualDelay = policy.GetNextDelay();
                Assert.Equal(expectedDelay, actualDelay);
            }
        }

        [Fact]
        public void GetNextDelay_WithZeroDelay_AlwaysReturnsZero()
        {
            // Arrange
            var policy = new SimpleRecoveryPolicy(TimeSpan.Zero);

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                var actualDelay = policy.GetNextDelay();
                Assert.Equal(TimeSpan.Zero, actualDelay);
            }
        }

        [Fact]
        public void GetNextDelay_WithNegativeDelay_AlwaysReturnsNegative()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromSeconds(-3);
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                var actualDelay = policy.GetNextDelay();
                Assert.Equal(expectedDelay, actualDelay);
            }
        }

        [Fact]
        public void GetNextDelay_WithPreciseTimeSpan_ReturnsExactValue()
        {
            // Arrange
            var expectedDelay = new TimeSpan(0, 0, 0, 0, 123); // 123 milliseconds
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Act
            var actualDelay = policy.GetNextDelay();

            // Assert
            Assert.Equal(expectedDelay, actualDelay);
            Assert.Equal(123, actualDelay.Milliseconds);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void SimpleRecoveryPolicy_ImplementsIRecoveryPolicy()
        {
            // Arrange
            var policy = new SimpleRecoveryPolicy(TimeSpan.FromSeconds(1));

            // Act & Assert
            Assert.IsAssignableFrom<IRecoveryPolicy>(policy);
        }

        [Fact]
        public void GetNextDelay_ImplementsInterfaceCorrectly()
        {
            // Arrange
            IRecoveryPolicy policy = new SimpleRecoveryPolicy(TimeSpan.FromSeconds(5));

            // Act
            var result = policy.GetNextDelay();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GetNextDelay_WithMaxValueTimeSpan_ReturnsMaxValue()
        {
            // Arrange
            var expectedDelay = TimeSpan.MaxValue;
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Act
            var actualDelay = policy.GetNextDelay();

            // Assert
            Assert.Equal(expectedDelay, actualDelay);
        }

        [Fact]
        public void GetNextDelay_WithMinValueTimeSpan_ReturnsMinValue()
        {
            // Arrange
            var expectedDelay = TimeSpan.MinValue;
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Act
            var actualDelay = policy.GetNextDelay();

            // Assert
            Assert.Equal(expectedDelay, actualDelay);
        }

        [Fact]
        public void GetNextDelay_WithTicksTimeSpan_ReturnsExactValue()
        {
            // Arrange
            var expectedDelay = new TimeSpan(1000); // 1000 ticks
            var policy = new SimpleRecoveryPolicy(expectedDelay);

            // Act
            var actualDelay = policy.GetNextDelay();

            // Assert
            Assert.Equal(expectedDelay, actualDelay);
            Assert.Equal(1000, actualDelay.Ticks);
        }

        #endregion

        #region Multiple Instances Tests

        [Fact]
        public void MultipleInstances_WithDifferentDelays_ReturnCorrectDelays()
        {
            // Arrange
            var delay1 = TimeSpan.FromSeconds(1);
            var delay2 = TimeSpan.FromSeconds(5);
            var policy1 = new SimpleRecoveryPolicy(delay1);
            var policy2 = new SimpleRecoveryPolicy(delay2);

            // Act
            var result1 = policy1.GetNextDelay();
            var result2 = policy2.GetNextDelay();

            // Assert
            Assert.Equal(delay1, result1);
            Assert.Equal(delay2, result2);
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void MultipleInstances_WithSameDelay_ReturnSameDelay()
        {
            // Arrange
            var delay = TimeSpan.FromSeconds(3);
            var policy1 = new SimpleRecoveryPolicy(delay);
            var policy2 = new SimpleRecoveryPolicy(delay);

            // Act
            var result1 = policy1.GetNextDelay();
            var result2 = policy2.GetNextDelay();

            // Assert
            Assert.Equal(delay, result1);
            Assert.Equal(delay, result2);
            Assert.Equal(result1, result2);
        }

        #endregion
    }
}