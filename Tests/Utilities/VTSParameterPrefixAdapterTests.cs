using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    /// <summary>
    /// Unit tests for VTSParameterPrefixAdapter
    /// </summary>
    public class VTSParameterPrefixAdapterTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidConfig_InitializesSuccessfully()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");

            // Act
            var adapter = new VTSParameterPrefixAdapter(config);

            // Assert
            adapter.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new VTSParameterPrefixAdapter(null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("config");
        }

        #endregion

        #region AdaptParameters Tests

        [Fact]
        public void AdaptParameters_WithValidParameters_AppliesPrefix()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var parameters = new List<VTSParameter>
            {
                new("TestParam1", -1.0, 1.0, 0.0),
                new("TestParam2", 0.0, 100.0, 50.0)
            };

            // Act
            var result = adapter.AdaptParameters(parameters).ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].Name.Should().Be("_SB_TestParam1");
            result[0].Min.Should().Be(-1.0);
            result[0].Max.Should().Be(1.0);
            result[0].DefaultValue.Should().Be(0.0);
            result[1].Name.Should().Be("_SB_TestParam2");
            result[1].Min.Should().Be(0.0);
            result[1].Max.Should().Be(100.0);
            result[1].DefaultValue.Should().Be(50.0);
        }

        [Fact]
        public void AdaptParameters_WithEmptyPrefix_ReturnsUnchangedParameters()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "");
            var adapter = new VTSParameterPrefixAdapter(config);
            var parameters = new List<VTSParameter>
            {
                new("TestParam1", -1.0, 1.0, 0.0)
            };

            // Act
            var result = adapter.AdaptParameters(parameters).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("TestParam1");
            result[0].Min.Should().Be(-1.0);
            result[0].Max.Should().Be(1.0);
            result[0].DefaultValue.Should().Be(0.0);
        }

        [Fact]
        public void AdaptParameters_WithNullPrefix_ReturnsUnchangedParameters()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: null!);
            var adapter = new VTSParameterPrefixAdapter(config);
            var parameters = new List<VTSParameter>
            {
                new("TestParam1", -1.0, 1.0, 0.0)
            };

            // Act
            var result = adapter.AdaptParameters(parameters).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("TestParam1");
        }

        [Fact]
        public void AdaptParameters_WithEmptyCollection_ReturnsEmptyCollection()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var parameters = new List<VTSParameter>();

            // Act
            var result = adapter.AdaptParameters(parameters).ToList();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void AdaptParameters_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);

            // Act & Assert
            var action = () => adapter.AdaptParameters(null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("parameters");
        }

        [Fact]
        public void AdaptParameters_WithCustomPrefix_AppliesCustomPrefix()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "MyPrefix_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var parameters = new List<VTSParameter>
            {
                new("TestParam", -1.0, 1.0, 0.0)
            };

            // Act
            var result = adapter.AdaptParameters(parameters).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("MyPrefix_TestParam");
        }

        [Fact]
        public void AdaptParameters_PreservesAllParameterProperties()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var originalParameter = new VTSParameter("TestParam", -5.5, 10.25, 2.75);
            var parameters = new List<VTSParameter> { originalParameter };

            // Act
            var result = adapter.AdaptParameters(parameters).ToList();

            // Assert
            result.Should().HaveCount(1);
            var adaptedParameter = result[0];
            adaptedParameter.Name.Should().Be("_SB_TestParam");
            adaptedParameter.Min.Should().Be(originalParameter.Min);
            adaptedParameter.Max.Should().Be(originalParameter.Max);
            adaptedParameter.DefaultValue.Should().Be(originalParameter.DefaultValue);
        }

        #endregion

        #region AdaptTrackingParameters Tests

        [Fact]
        public void AdaptTrackingParameters_WithNullInput_ReturnsEmptyCollection()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);

            // Act
            var result = adapter.AdaptTrackingParameters(null!);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void AdaptTrackingParameters_WithEmptyInput_ReturnsEmptyCollection()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var trackingParams = new List<TrackingParam>();

            // Act
            var result = adapter.AdaptTrackingParameters(trackingParams);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void AdaptTrackingParameters_WithValidInput_AppliesPrefixToIds()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var trackingParams = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5, Weight = 1.0 },
                new TrackingParam { Id = "Param2", Value = 0.8, Weight = 0.5 }
            };

            // Act
            var result = adapter.AdaptTrackingParameters(trackingParams).ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].Id.Should().Be("_SB_Param1");
            result[0].Value.Should().Be(0.5);
            result[0].Weight.Should().Be(1.0);
            result[1].Id.Should().Be("_SB_Param2");
            result[1].Value.Should().Be(0.8);
            result[1].Weight.Should().Be(0.5);
        }

        [Fact]
        public void AdaptTrackingParameters_WithEmptyPrefix_ReturnsOriginalParameters()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "");
            var adapter = new VTSParameterPrefixAdapter(config);
            var trackingParams = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5, Weight = 1.0 }
            };

            // Act
            var result = adapter.AdaptTrackingParameters(trackingParams).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be("Param1");
            result[0].Value.Should().Be(0.5);
            result[0].Weight.Should().Be(1.0);
        }

        [Fact]
        public void AdaptTrackingParameters_WithNullPrefix_ReturnsOriginalParameters()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: null!);
            var adapter = new VTSParameterPrefixAdapter(config);
            var trackingParams = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5, Weight = 1.0 }
            };

            // Act
            var result = adapter.AdaptTrackingParameters(trackingParams).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be("Param1");
            result[0].Value.Should().Be(0.5);
            result[0].Weight.Should().Be(1.0);
        }

        [Fact]
        public void AdaptTrackingParameters_CreatesNewInstances()
        {
            // Arrange
            var config = new VTubeStudioPCConfig(parameterPrefix: "_SB_");
            var adapter = new VTSParameterPrefixAdapter(config);
            var originalParam = new TrackingParam { Id = "TestParam", Value = 0.5, Weight = 1.0 };
            var trackingParams = new List<TrackingParam> { originalParam };

            // Act
            var result = adapter.AdaptTrackingParameters(trackingParams).ToList();

            // Assert
            result.Should().HaveCount(1);
            var adaptedParam = result[0];
            adaptedParam.Should().NotBeSameAs(originalParam);
            adaptedParam.Id.Should().Be("_SB_TestParam");
            adaptedParam.Value.Should().Be(originalParam.Value);
            adaptedParam.Weight.Should().Be(originalParam.Weight);
        }

        #endregion
    }
}
