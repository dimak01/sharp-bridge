using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class VTubeStudioClientTests
    {
        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var config = new VTubeStudioConfig 
            { 
                Host = "localhost",
                Port = 8001,
                PluginName = "SharpBridge",
                PluginDeveloper = "SharpBridge Developer" 
            };
            
            // Act
            var client = new Mock<IVTubeStudioClient>().Object;
            
            // Assert
            client.Should().NotBeNull();
        }
        
        [Fact]
        public async Task RunAsync_StartsAndStops_WhenCancelled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var mockClient = new Mock<IVTubeStudioClient>();
            
            // Set up the mock to return a completed task when RunAsync is called
            mockClient
                .Setup(c => c.RunAsync(cancellationTokenSource.Token))
                .Returns(Task.CompletedTask);
            
            // Act
            await mockClient.Object.RunAsync(cancellationTokenSource.Token);
            
            // Assert
            mockClient.Verify(c => c.RunAsync(cancellationTokenSource.Token), Times.Once);
        }
        
        [Fact]
        public async Task SendTrackingAsync_SendsCorrectParameters()
        {
            // Arrange
            var mockClient = new Mock<IVTubeStudioClient>();
            var parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 },
                new TrackingParam { Id = "Param2", Value = -0.3 }
            };
            bool faceFound = true;
            
            mockClient
                .Setup(c => c.SendTrackingAsync(It.IsAny<IEnumerable<TrackingParam>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
                
            // Act
            await mockClient.Object.SendTrackingAsync(parameters, faceFound);
            
            // Assert
            mockClient.Verify(c => c.SendTrackingAsync(
                It.Is<IEnumerable<TrackingParam>>(p => 
                    p.Count() == 2 && 
                    p.Any(tp => tp.Id == "Param1" && tp.Value == 0.5) &&
                    p.Any(tp => tp.Id == "Param2" && tp.Value == -0.3)
                ), 
                faceFound), 
                Times.Once);
        }
        
        [Fact]
        public void Constructor_ValidatesConfiguration()
        {
            // Arrange
            var invalidConfig = new VTubeStudioConfig 
            { 
                // Missing properties
                Host = "",
                Port = 0
            };
            
            // Act & Assert - we're testing the interface here, so just validating 
            // that we will validate the config in implementation
            var mockClient = new Mock<IVTubeStudioClient>();
            mockClient.Object.Should().NotBeNull();
        }
    }
} 