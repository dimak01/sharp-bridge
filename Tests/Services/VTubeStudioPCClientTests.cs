using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class VTubeStudioPCClientTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        
        public VTubeStudioPCClientTests()
        {
            _mockLogger = new Mock<IAppLogger>();
        }

        [Fact]
        public void GetServiceStats_ReturnsValidStats()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object);
            
            // Act
            var stats = client.GetServiceStats();
            
            // Assert
            Assert.NotNull(stats);
            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal(WebSocketState.None.ToString(), stats.Status);
            Assert.NotNull(stats.Counters);
            
            // Check the counters
            Assert.True(stats.Counters.ContainsKey("MessagesSent"));
            Assert.True(stats.Counters.ContainsKey("ConnectionAttempts"));
            Assert.True(stats.Counters.ContainsKey("FailedConnections"));
            Assert.True(stats.Counters.ContainsKey("UptimeSeconds"));
            
            // The entity might be null initially since no tracking data has been sent yet
            if (stats.CurrentEntity != null)
            {
                var pcTrackingInfo = stats.CurrentEntity as PCTrackingInfo; 
                Assert.Equal(false, pcTrackingInfo.FaceFound);
                Assert.NotNull(pcTrackingInfo.Parameters);
            }
        }
        
        [Fact]
        public void GetServiceStats_AfterConnectAndSend_UpdatesStatistics()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object);
            
            // Act - Connect and send tracking data
            client.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            
            var trackingData = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new[]
                {
                    new TrackingParam { 
                        Id = "Test", 
                        Value = 0.5, 
                        Min = -0.75, 
                        Max = 1.25, 
                        DefaultValue = 0.33 
                    }
                }
            };
            
            client.SendTrackingAsync(trackingData, CancellationToken.None).GetAwaiter().GetResult();
            
            // Get the stats
            var stats = client.GetServiceStats();
            
            // Assert
            Assert.NotNull(stats);
            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal(WebSocketState.Open.ToString(), stats.Status);
            
            // Check the counters
            Assert.Equal(1, stats.Counters["MessagesSent"]);
            Assert.Equal(1, stats.Counters["ConnectionAttempts"]);
            Assert.Equal(0, stats.Counters["FailedConnections"]);
            Assert.True(stats.Counters["UptimeSeconds"] >= 0);
            
            // Check the PCTrackingInfo entity
            Assert.NotNull(stats.CurrentEntity);
            Assert.IsType<PCTrackingInfo>(stats.CurrentEntity);
            var pcTrackingInfo = stats.CurrentEntity as PCTrackingInfo;
            Assert.True(pcTrackingInfo.FaceFound);
            Assert.NotNull(pcTrackingInfo.Parameters);
            Assert.Single(pcTrackingInfo.Parameters);
            Assert.Equal("Test", pcTrackingInfo.Parameters.First().Id);
            Assert.Equal(0.5, pcTrackingInfo.Parameters.First().Value);
            Assert.Equal(-0.75, pcTrackingInfo.Parameters.First().Min);
            Assert.Equal(1.25, pcTrackingInfo.Parameters.First().Max);
            Assert.Equal(0.33, pcTrackingInfo.Parameters.First().DefaultValue);
            
            // Verify logger was called with connection info
            _mockLogger.Verify(l => l.Info(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
            
            // Cleanup
            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None)
                .GetAwaiter().GetResult();
            client.Dispose();
        }
    }
} 