using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class VTubeStudioPCClientTests
    {
        [Fact]
        public void GetServiceStats_ReturnsValidStats()
        {
            // Arrange
            var client = new VTubeStudioPCClient();
            
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
                Assert.Equal(false, stats.CurrentEntity.FaceFound);
                Assert.NotNull(stats.CurrentEntity.Parameters);
            }
        }
        
        [Fact]
        public void GetServiceStats_AfterConnectAndSend_UpdatesStatistics()
        {
            // Arrange
            var client = new VTubeStudioPCClient();
            
            // Act - Connect and send tracking data
            client.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            
            var trackingData = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new[]
                {
                    new TrackingParam { Id = "Test", Value = 0.5 }
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
            Assert.True(stats.CurrentEntity.FaceFound);
            Assert.NotNull(stats.CurrentEntity.Parameters);
            Assert.Single(stats.CurrentEntity.Parameters);
            Assert.Equal("Test", stats.CurrentEntity.Parameters.First().Id);
            Assert.Equal(0.5, stats.CurrentEntity.Parameters.First().Value);
            
            // Cleanup
            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None)
                .GetAwaiter().GetResult();
            client.Dispose();
        }
    }
} 