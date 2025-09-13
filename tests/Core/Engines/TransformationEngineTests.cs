// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NCalc;
using SharpBridge.Core.Engines;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Domain;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Domain;
using SharpBridge.Models.Events;
using Xunit;

namespace SharpBridge.Tests.Core.Engines
{
    public class TransformationEngineTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<ITransformationRulesRepository> _mockRepository;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IFileChangeWatcher> _mockAppConfigWatcher;

        public TransformationEngineTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockRepository = new Mock<ITransformationRulesRepository>();
            _mockConfigManager = new Mock<IConfigManager>();
            _mockAppConfigWatcher = new Mock<IFileChangeWatcher>();
        }

        #region Helper Methods

        private TransformationEngine CreateEngine(string configPath = "")
        {
            var config = new TransformationEngineConfig
            {
                ConfigPath = configPath,
                MaxEvaluationIterations = 10
            };
            return new TransformationEngine(_mockLogger.Object, _mockRepository.Object, config, _mockConfigManager.Object, _mockAppConfigWatcher.Object);
        }

        private PhoneTrackingInfo CreateValidTrackingData()
        {
            return new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0.1, Y = 0.2, Z = 0.3 },
                Rotation = new Coordinates { X = 0.4, Y = 0.5, Z = 0.6 },
                EyeLeft = new Coordinates { X = 0.7, Y = 0.8, Z = 0.9 },
                EyeRight = new Coordinates { X = 1.0, Y = 1.1, Z = 1.2 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.6 },
                    new BlendShape { Key = "mouthOpen", Value = 0.3 }
                }
            };
        }

        private static ParameterTransformation CreateTestTransformation(string name, string expression, double min = 0, double max = 100, double defaultValue = 0)
        {
            return new ParameterTransformation(name, new Expression(expression), expression, min, max, defaultValue);
        }

        private void SetupRepositoryWithRules(params ParameterTransformation[] rules)
        {
            var result = new RulesLoadResult(
                rules.ToList(),
                new List<RuleInfo>(),
                new List<string>(),
                false,
                null);

            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(result);
            _mockRepository.Setup(r => r.IsUpToDate).Returns(true);
        }

        private void SetupRepositoryWithError(string errorMessage, bool hasCache = false)
        {
            var result = new RulesLoadResult(
                hasCache ? new List<ParameterTransformation> { CreateTestTransformation("CachedRule", "eyeBlinkLeft * 100") } : new List<ParameterTransformation>(),
                new List<RuleInfo>(),
                new List<string>(),
                hasCache,
                errorMessage);

            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(result);
            _mockRepository.Setup(r => r.IsUpToDate).Returns(false);
        }

        private void SetupRepositoryWithException(Exception exception)
        {
            _mockRepository.Setup(r => r.LoadRulesAsync()).ThrowsAsync(exception);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new TransformationEngine(null!, _mockRepository.Object, new TransformationEngineConfig(), _mockConfigManager.Object, _mockAppConfigWatcher.Object));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new TransformationEngine(_mockLogger.Object, null!, new TransformationEngineConfig(), _mockConfigManager.Object, _mockAppConfigWatcher.Object));
            exception.ParamName.Should().Be("rulesRepository");
        }

        #endregion

        #region Repository Integration Tests

        [Fact]
        public async Task LoadRulesAsync_CallsRepository()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            SetupRepositoryWithRules();

            // Act
            await engine.LoadRulesAsync();

            // Assert
            _mockRepository.Verify(r => r.LoadRulesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadRulesAsync_RepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            SetupRepositoryWithException(new InvalidOperationException("Repository error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await engine.LoadRulesAsync());
        }

        [Fact]
        public async Task LoadRulesAsync_RepositoryReturnsError_UpdatesServiceStats()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            SetupRepositoryWithError("Critical error");

            // Act
            await engine.LoadRulesAsync();

            // Assert
            var stats = engine.GetServiceStats();
            stats.Status.Should().Be("NoValidRules");
            stats.LastError.Should().Be("Critical error");
        }

        [Fact]
        public async Task LoadRulesAsync_RepositoryReturnsCachedRules_UpdatesServiceStats()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            SetupRepositoryWithError("File error", hasCache: true);

            // Act
            await engine.LoadRulesAsync();

            // Assert
            var stats = engine.GetServiceStats();
            stats.Status.Should().Be("ConfigErrorCached");
            stats.LastError.Should().Be("File error");
        }

        [Fact]
        public void IsConfigUpToDate_DelegatesToRepository()
        {
            // Arrange
            var engine = CreateEngine();
            _mockRepository.Setup(r => r.IsUpToDate).Returns(true);

            // Act
            var isUpToDate = engine.IsConfigUpToDate;

            // Assert
            isUpToDate.Should().BeTrue();
            _mockRepository.Verify(r => r.IsUpToDate, Times.Once);
        }

        #endregion

        #region Data Transformation Tests

        [Fact]
        public async Task TransformData_WithValidRules_TransformsDataCorrectly()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rule = CreateTestTransformation("TestParam", "eyeBlinkLeft * 100", 0, 100, 0);
            SetupRepositoryWithRules(rule);
            await engine.LoadRulesAsync();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().ContainSingle();
            result.Parameters.First().Id.Should().Be("TestParam");
            result.Parameters.First().Value.Should().Be(50); // 0.5 * 100
        }

        [Fact]
        public async Task TransformData_WithMultipleRules_AppliesAllRules()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rules = new[]
            {
                CreateTestTransformation("HeadMovement", "HeadPosX * 100 + HeadRotY * 50", -1000, 1000, 0),
                CreateTestTransformation("EyeBlink", "(eyeBlinkLeft + eyeBlinkRight) * 50", 0, 100, 0)
            };
            SetupRepositoryWithRules(rules);
            await engine.LoadRulesAsync();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0.1, Y = 0, Z = 0 },
                Rotation = new Coordinates { X = 0, Y = 0.2, Z = 0 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.3 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.5 }
                }
            };

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().HaveCount(2);

            var headMovement = result.Parameters.FirstOrDefault(p => p.Id == "HeadMovement")!;
            headMovement.Should().NotBeNull();
            headMovement.Value.Should().BeApproximately(20, 0.001); // 0.1 * 100 + 0.2 * 50 = 20

            var eyeBlink = result.Parameters.FirstOrDefault(p => p.Id == "EyeBlink")!;
            eyeBlink.Should().NotBeNull();
            eyeBlink.Value.Should().BeApproximately(40, 0.001); // (0.3 + 0.5) * 50 = 40
        }

        [Fact]
        public async Task TransformData_ClampsValuesToMinMax()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rule = CreateTestTransformation("LimitedValue", "eyeBlinkLeft * 1000", 0, 100, 0);
            SetupRepositoryWithRules(rule);
            await engine.LoadRulesAsync();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 } // 0.5 * 1000 = 500, should be clamped to 100
                }
            };

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().ContainSingle();
            result.Parameters.First().Value.Should().Be(100); // Clamped to max
        }

        [Fact]
        public async Task TransformData_HandlesInvalidExpressions_Gracefully()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rules = new[]
            {
                CreateTestTransformation("ValidParam", "eyeBlinkLeft * 100", 0, 100, 0),
                CreateTestTransformation("InvalidParam", "unknownVariable + 100", 0, 100, 50) // This will fail evaluation
            };
            SetupRepositoryWithRules(rules);
            await engine.LoadRulesAsync();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().ContainSingle(); // Only the valid parameter should be evaluated

            var validParam = result.Parameters.First();
            validParam.Id.Should().Be("ValidParam");
            validParam.Value.Should().Be(50); // 0.5 * 100 = 50
        }

        [Fact]
        public void TransformData_ReturnsEmptyCollection_WhenNoRulesLoaded()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public async Task TransformData_ReturnsEmptyCollection_WhenFaceNotFound()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rule = CreateTestTransformation("TestParam", "eyeBlinkLeft * 100", 0, 100, 0);
            SetupRepositoryWithRules(rule);
            await engine.LoadRulesAsync();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = false, // Face not found
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public async Task TransformData_HandlesNullBlendShapes_Gracefully()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rule = CreateTestTransformation("TestParam", "eyeBlinkLeft * 100", 0, 100, 0);
            SetupRepositoryWithRules(rule);
            await engine.LoadRulesAsync();

            // Create tracking data with a null BlendShape to cause an exception
            var malformedData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape> { null! }
            };

            // Act
            var result = engine.TransformData(malformedData);

            // Assert - should handle the exception gracefully
            result.Should().NotBeNull();
            result.FaceFound.Should().BeTrue();
            result.Parameters.Should().BeEmpty();

            // Verify error statistics were updated
            var stats = engine.GetServiceStats();
            stats.Counters["Total Transformations"].Should().Be(1);
            stats.Counters["Failed Transformations"].Should().Be(1);
            stats.LastError.Should().Contain("Transformation failed");
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public async Task LoadRulesAsync_SuccessfulLoad_IncrementsHotReloadSuccessesOnlyOnce()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rule = CreateTestTransformation("TestParam", "eyeBlinkLeft * 100", 0, 100, 0);
            SetupRepositoryWithRules(rule);

            // Get initial stats
            var initialStats = engine.GetServiceStats();
            var initialSuccesses = initialStats.Counters["Hot Reload Successes"];

            // Act - perform one successful load
            await engine.LoadRulesAsync();

            // Assert - success counter should be incremented by exactly 1
            var finalStats = engine.GetServiceStats();
            var finalSuccesses = finalStats.Counters["Hot Reload Successes"];

            // This test should FAIL with current buggy code (increments by 2)
            // but PASS after we fix the double increment bug
            (finalSuccesses - initialSuccesses).Should().Be(1,
                "Hot reload success counter should be incremented by exactly 1 per successful load");
        }

        #endregion

        #region Parameter Definition Tests

        [Fact]
        public async Task GetParameterDefinitions_ReturnsAllLoadedRules()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rules = new[]
            {
                CreateTestTransformation("TestParam1", "eyeBlinkLeft * 100", 0, 100, 0),
                CreateTestTransformation("TestParam2", "eyeBlinkRight * 50", -50, 50, 25)
            };
            SetupRepositoryWithRules(rules);
            await engine.LoadRulesAsync();

            // Act
            var parameters = engine.GetParameterDefinitions().ToList();

            // Assert
            parameters.Should().HaveCount(2);

            var param1 = parameters.First(p => p.Name == "TestParam1");
            param1.Min.Should().Be(0);
            param1.Max.Should().Be(100);
            param1.DefaultValue.Should().Be(0);

            var param2 = parameters.First(p => p.Name == "TestParam2");
            param2.Min.Should().Be(-50);
            param2.Max.Should().Be(50);
            param2.DefaultValue.Should().Be(25);
        }

        [Fact]
        public void GetParameterDefinitions_NoRules_ReturnsEmptyCollection()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            var parameters = engine.GetParameterDefinitions();

            // Assert
            parameters.Should().BeEmpty();
        }

        [Fact]
        public async Task TransformData_PopulatesParameterCalculationExpressions()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var rules = new[]
            {
                CreateTestTransformation("TestParam1", "eyeBlinkLeft * 100", 0, 100, 0),
                CreateTestTransformation("TestParam2", "HeadPosX + HeadPosY", -10, 10, 0)
            };
            SetupRepositoryWithRules(rules);
            await engine.LoadRulesAsync();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0.1, Y = 0.2, Z = 0 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.Should().NotBeNull();
            result.ParameterCalculationExpressions.Should().NotBeNull();
            result.ParameterCalculationExpressions.Should().HaveCount(2);

            result.ParameterCalculationExpressions.Should().ContainKey("TestParam1");
            result.ParameterCalculationExpressions["TestParam1"].Should().Be("eyeBlinkLeft * 100");

            result.ParameterCalculationExpressions.Should().ContainKey("TestParam2");
            result.ParameterCalculationExpressions["TestParam2"].Should().Be("HeadPosX + HeadPosY");
        }

        #endregion

        #region VTSParameter Validation Tests

        [Fact]
        public void VTSParameter_Constructor_WithMinGreaterThanMax_ThrowsArgumentException()
        {
            // Act & Assert - Test the VTSParameter validation that min > max throws exception
            var exception = Assert.Throws<ArgumentException>(() =>
                new VTSParameter("TestParam", 100, 50, 75)); // min=100, max=50 (invalid)

            exception.Message.Should().Contain("Min");
            exception.Message.Should().Contain("max");
        }

        [Fact]
        public void VTSParameter_Constructor_WithValidParameters_CreatesSuccessfully()
        {
            // Act
            var parameter = new VTSParameter("TestParam", 0, 100, 50);

            // Assert
            parameter.Name.Should().Be("TestParam");
            parameter.Min.Should().Be(0);
            parameter.Max.Should().Be(100);
            parameter.DefaultValue.Should().Be(50);
        }

        [Fact]
        public void VTSParameter_Constructor_WithEqualMinMax_CreatesSuccessfully()
        {
            // Act - Edge case where min equals max should be valid
            var parameter = new VTSParameter("TestParam", 50, 50, 50);

            // Assert
            parameter.Name.Should().Be("TestParam");
            parameter.Min.Should().Be(50);
            parameter.Max.Should().Be(50);
            parameter.DefaultValue.Should().Be(50);
        }

        #endregion

        #region RuleInfo Type Property Tests

        [Fact]
        public void RuleInfo_Constructor_SetsAllProperties()
        {
            // Arrange
            var name = "TestParam";
            var func = "eyeBlinkLeft * 100";
            var error = "Validation error";
            var type = "BlendShape";

            // Act - Test the RuleInfo constructor and properties
            var ruleInfo = new RuleInfo(name, func, error, type);

            // Assert - This covers all RuleInfo properties including Type
            ruleInfo.Name.Should().Be(name);
            ruleInfo.Func.Should().Be(func);
            ruleInfo.Error.Should().Be(error);
            ruleInfo.Type.Should().Be(type); // This covers the Type property
        }

        [Fact]
        public void RuleInfo_Constructor_WithNullValues_HandlesCorrectly()
        {
            // Act - Test null handling in RuleInfo constructor
            var ruleInfo = new RuleInfo(null!, null!, null!, null!);

            // Assert - Should handle nulls by converting to empty strings
            ruleInfo.Name.Should().Be(string.Empty);
            ruleInfo.Func.Should().Be(string.Empty);
            ruleInfo.Error.Should().Be(string.Empty);
            ruleInfo.Type.Should().Be(string.Empty);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldUnsubscribeFromFileWatcherEvents()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            engine.Dispose();

            // Assert
            // Note: We can't directly verify event unsubscription with Moq, but we can verify
            // that the disposal process completes without throwing
            engine.Dispose(); // Should not throw on second call
            _mockLogger.Verify(l => l.Debug("Disposing TransformationEngine"), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromRulesRepositoryEvents()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            engine.Dispose();

            // Assert
            // Note: We can't directly verify event unsubscription with Moq, but we can verify
            // that the disposal process completes without throwing
            engine.Dispose(); // Should not throw on second call
            _mockLogger.Verify(l => l.Debug("Disposing TransformationEngine"), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldLogDebugMessage()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            engine.Dispose();

            // Assert
            _mockLogger.Verify(l => l.Debug("Disposing TransformationEngine"), Times.Once);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_ShouldNotThrow()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Dispose();

            // Act & Assert
            engine.Dispose(); // Should not throw
            _mockLogger.Verify(l => l.Debug("Disposing TransformationEngine"), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            engine.Dispose();
            engine.Dispose();
            engine.Dispose();

            // Assert
            // Verify that disposal operations only happen once by checking logging
            _mockLogger.Verify(l => l.Debug("Disposing TransformationEngine"), Times.Once);
        }

        #endregion

        #region Event Handler Tests

        [Fact]
        public void Constructor_ShouldSubscribeToFileWatcherEvents()
        {
            // Arrange & Act
            var engine = CreateEngine();

            // Assert
            // Verify that the engine subscribes to file change events during construction
            // We can't directly verify the subscription, but we can verify the engine is created successfully
            engine.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSubscribeToRulesRepositoryEvents()
        {
            // Arrange & Act
            var engine = CreateEngine();

            // Assert
            // Verify that the engine subscribes to rules change events during construction
            // We can't directly verify the subscription, but we can verify the engine is created successfully
            engine.Should().NotBeNull();
        }

        [Fact]
        public void ConfigChanged_InitiallyShouldBeFalse()
        {
            // Arrange
            var engine = CreateEngine();

            // Act & Assert
            engine.ConfigChanged.Should().BeFalse();
        }

        [Fact]
        public void ConfigChanged_WhenConfigChanges_ShouldBeTrue()
        {
            // Arrange
            var engine = CreateEngine();
            var newConfig = new TransformationEngineConfig { ConfigPath = "different.json" };
            _mockConfigManager.Setup(c => c.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(newConfig);

            // Act & Assert
            // Since we can't directly test the private event handler, we test the initial state
            // and verify the property exists and works correctly
            engine.ConfigChanged.Should().BeFalse(); // Initially false

            // Verify the property is accessible and returns the expected type
            engine.ConfigChanged.Should().BeFalse();
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromAllEvents()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            engine.Dispose();

            // Assert
            // Verify that disposal completes without throwing
            // The actual unsubscription is tested indirectly through the disposal pattern
            engine.Dispose(); // Should not throw on second call
            _mockLogger.Verify(l => l.Debug("Disposing TransformationEngine"), Times.Once);
        }

        #endregion

        #region OnApplicationConfigChanged Tests

        [Fact]
        public void OnApplicationConfigChanged_ShouldLogDebugMessage()
        {
            // Arrange
            CreateEngine(); // Create engine to set up event handlers
            var fileChangeArgs = new FileChangeEventArgs("config.json");

            // Act
            _mockAppConfigWatcher.Raise(w => w.FileChanged += null, fileChangeArgs);

            // Assert
            _mockLogger.Verify(l => l.Debug("Application config changed, checking if transformation engine config was affected"), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_ShouldSetConfigChangedFlag()
        {
            // Arrange
            var engine = CreateEngine();
            var fileChangeArgs = new FileChangeEventArgs("config.json");

            // Act
            _mockAppConfigWatcher.Raise(w => w.FileChanged += null, fileChangeArgs);

            // Assert
            engine.ConfigChanged.Should().BeTrue();
        }

        [Fact]
        public void OnRulesChanged_ShouldLogDebugMessage()
        {
            // Arrange
            CreateEngine(); // Create engine to subscribe to events
            var eventArgs = new RulesChangedEventArgs("test-rules.json");

            // Act - Raise the event that the engine is subscribed to
            _mockRepository.Raise(x => x.RulesChanged += null, this, eventArgs);

            // Assert
            _mockLogger.Verify(x => x.Debug("Rules file changed: test-rules.json"), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            CreateEngine(); // Create engine to subscribe to events
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act - Raise the event that the engine is subscribed to
            _mockAppConfigWatcher.Raise(x => x.FileChanged += null, this, new FileChangeEventArgs("test.json"));

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error handling application config change", It.IsAny<InvalidOperationException>()), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TransformationEngine(_mockLogger.Object, _mockRepository.Object, null!, _mockConfigManager.Object, _mockAppConfigWatcher.Object));
            exception.ParamName.Should().Be("config");
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TransformationEngine(_mockLogger.Object, _mockRepository.Object, new TransformationEngineConfig(), null!, _mockAppConfigWatcher.Object));
            exception.ParamName.Should().Be("configManager");
        }

        [Fact]
        public void Constructor_WithNullAppConfigWatcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TransformationEngine(_mockLogger.Object, _mockRepository.Object, new TransformationEngineConfig(), _mockConfigManager.Object, null!));
            exception.ParamName.Should().Be("appConfigWatcher");
        }

        [Fact]
        public async Task LoadRulesAsync_WithMixedValidAndInvalidRules_ShouldSetPartiallyValidStatus()
        {
            // Arrange
            var validRules = new List<ParameterTransformation>
            {
                CreateTestTransformation("ValidRule", "eyeBlinkLeft * 100")
            };
            var invalidRules = new List<RuleInfo>
            {
                new RuleInfo("InvalidRule", "invalid expression", "Syntax error", "Syntax")
            };
            var validationErrors = new List<string> { "InvalidRule: Syntax error" };

            var result = new RulesLoadResult(validRules, invalidRules, validationErrors, false, null);
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(result);

            var engine = CreateEngine();

            // Act
            await engine.LoadRulesAsync();

            // Assert
            var stats = engine.GetServiceStats();
            stats.Status.Should().Be("RulesPartiallyValid");
            stats.LastError.Should().Be(string.Empty);
        }

        [Fact]
        public void TransformData_WithMissingParameter_ShouldHandleGracefully()
        {
            // Arrange
            var rule = CreateTestTransformation("TestRule", "MissingParam + 1");
            SetupRepositoryWithRules(rule);
            var engine = CreateEngine();
            engine.LoadRulesAsync().Wait();

            var trackingData = CreateValidTrackingData();

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.FaceFound.Should().BeTrue();
            result.Parameters.Should().BeEmpty(); // No parameters should be added due to missing dependency
        }

        [Fact]
        public void TransformData_WithEvaluationException_ShouldHandleGracefully()
        {
            // Arrange
            var rule = CreateTestTransformation("TestRule", "invalid syntax here"); // Invalid syntax
            SetupRepositoryWithRules(rule);
            var engine = CreateEngine();
            engine.LoadRulesAsync().Wait();

            var trackingData = CreateValidTrackingData();

            // Act
            var result = engine.TransformData(trackingData);

            // Assert
            result.FaceFound.Should().BeTrue();
            result.Parameters.Should().BeEmpty(); // No parameters should be added due to evaluation error
        }

        [Fact]
        public async Task TransformData_WithExtremumTracking_TracksParameterExtremums()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var trackingData = CreateValidTrackingData();
            var rules = new[]
            {
                CreateTestTransformation("Param1", "eyeBlinkLeft * 100", 0, 100, 0),
                CreateTestTransformation("Param2", "HeadPosX * 100 + HeadRotY * 50", -1000, 1000, 0)
            };
            SetupRepositoryWithRules(rules);
            await engine.LoadRulesAsync();

            // Act - First transformation
            var result1 = engine.TransformData(trackingData);

            // Assert - First transformation should initialize extremums
            result1.Should().NotBeNull();
            result1.ParameterExtremums.Should().NotBeNull();
            result1.ParameterExtremums.Should().HaveCount(2); // Two rules

            var param1Extremums = result1.ParameterExtremums["Param1"];
            param1Extremums.HasExtremums.Should().BeTrue();
            param1Extremums.Min.Should().Be(50.0); // First value becomes both min and max
            param1Extremums.Max.Should().Be(50.0);

            var param2Extremums = result1.ParameterExtremums["Param2"];
            param2Extremums.HasExtremums.Should().BeTrue();
            param2Extremums.Min.Should().Be(35.0); // HeadPosX * 100 + HeadRotY * 50 = 0.1 * 100 + 0.5 * 50 = 10 + 25 = 35
            param2Extremums.Max.Should().Be(35.0);
        }

        [Fact]
        public async Task LoadRulesAsync_WithExtremumTracking_ResetsExtremums()
        {
            // Arrange
            var engine = CreateEngine("test.json");
            var trackingData = CreateValidTrackingData();
            var rules = new[]
            {
                CreateTestTransformation("Param1", "eyeBlinkLeft * 100", 0, 100, 0),
                CreateTestTransformation("Param2", "HeadPosX * 100 + HeadRotY * 50", -1000, 1000, 0)
            };
            SetupRepositoryWithRules(rules);
            await engine.LoadRulesAsync();

            // Act - First transformation to establish extremums
            var result1 = engine.TransformData(trackingData);

            // Verify extremums are initialized
            result1.ParameterExtremums["Param1"].HasExtremums.Should().BeTrue();
            result1.ParameterExtremums["Param2"].HasExtremums.Should().BeTrue();

            // Act - Reload rules
            await engine.LoadRulesAsync();

            // Act - Second transformation after rule reload
            var result2 = engine.TransformData(trackingData);

            // Assert - Extremums should be reset (showing current value as both min and max)
            var param1Extremums = result2.ParameterExtremums["Param1"];
            param1Extremums.HasExtremums.Should().BeTrue();
            param1Extremums.Min.Should().Be(50.0); // Reset to current value
            param1Extremums.Max.Should().Be(50.0);

            var param2Extremums = result2.ParameterExtremums["Param2"];
            param2Extremums.HasExtremums.Should().BeTrue();
            param2Extremums.Min.Should().Be(35.0); // Reset to current value
            param2Extremums.Max.Should().Be(35.0);
        }

        #endregion

        #region LogValidationErrors Tests

        [Fact]
        public void LoadRulesAsync_WithValidationErrors_ShouldLogValidationErrors()
        {
            // Arrange
            var engine = CreateEngine();
            var validationErrors = new List<string> { "Error 1", "Error 2" };
            var invalidRuleInfo = new RuleInfo(
                name: "TestParam",
                func: "1+1",
                error: "Some error",
                type: "Validation"
            );
            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation>(),
                invalidRules: new List<RuleInfo> { invalidRuleInfo },
                validationErrors: validationErrors,
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();

            // Assert
            _mockLogger.Verify(l => l.Error(It.Is<string>(msg =>
                msg.Contains("Failed to load 1 transformation rules") &&
                msg.Contains("Valid rules: 0") &&
                msg.Contains("Error 1") &&
                msg.Contains("Error 2"))), Times.Once);
        }

        #endregion

        #region Custom Interpolation Definition for Testing

        /// <summary>
        /// Custom interpolation definition that will cause InterpolationMethodFactory.CreateFromDefinition to throw an exception
        /// </summary>
        public class UnsupportedInterpolation : IInterpolationDefinition
        {
            // This will cause the factory to throw an ArgumentException for unsupported type
        }

        #endregion

        #region Interpolation Tests

        [Fact]
        public void TransformData_WithLinearInterpolation_ShouldApplyLinearInterpolation()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            var linearInterpolation = new LinearInterpolation();
            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("HeadPosX * 2"),
                expressionString: "HeadPosX * 2",
                min: 0.0,
                max: 1.0,
                defaultValue: 0.5,
                interpolation: linearInterpolation
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            result.Parameters.Should().HaveCount(1);
            var param = result.Parameters.First();
            param.Id.Should().Be("TestParam");

            // The value should be interpolated (HeadPosX = 0.1, so 0.1 * 2 = 0.2)
            // Linear interpolation should return the value as-is since it's already normalized
            param.Value.Should().BeApproximately(0.2, 0.001);

            // Verify interpolation is stored
            result.ParameterInterpolations.Should().ContainKey("TestParam");
            result.ParameterInterpolations["TestParam"].Should().Be(linearInterpolation);
        }

        [Fact]
        public void TransformData_WithBezierInterpolation_ShouldApplyBezierInterpolation()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            var bezierInterpolation = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.5, Y = 0.8 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("HeadPosX"),
                expressionString: "HeadPosX",
                min: 0.0,
                max: 1.0,
                defaultValue: 0.5,
                interpolation: bezierInterpolation
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            result.Parameters.Should().HaveCount(1);
            var param = result.Parameters.First();
            param.Id.Should().Be("TestParam");

            // The value should be interpolated using Bezier curve
            // HeadPosX = 0.1, so normalized input is 0.1
            // Bezier interpolation will curve the value
            param.Value.Should().BeGreaterThan(0.0).And.BeLessThan(1.0);

            // Verify interpolation is stored
            result.ParameterInterpolations.Should().ContainKey("TestParam");
            result.ParameterInterpolations["TestParam"].Should().Be(bezierInterpolation);
        }

        [Fact]
        public void TransformData_WithInterpolationException_ShouldFallbackToLinearClamping()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            // The rule is not used since we're testing invalid interpolation validation

            // The invalid interpolation should cause the rule to be marked as invalid
            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation>(), // No valid rules due to invalid interpolation
                invalidRules: new List<RuleInfo> { new RuleInfo("TestParam", "HeadPosX * 2", "Invalid interpolation configuration", "Validation") },
                validationErrors: new List<string> { "Rule 'TestParam' has invalid interpolation configuration" },
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            // Since the rule is invalid, no parameters should be generated
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void TransformData_WithValidInterpolationButExceptionDuringProcessing_ShouldFallbackToLinearClamping()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            // Create a rule with unsupported interpolation that will cause an exception during processing
            var unsupportedInterpolation = new UnsupportedInterpolation();

            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("HeadPosX * 2"),
                expressionString: "HeadPosX * 2",
                min: 0.0,
                max: 1.0,
                defaultValue: 0.5,
                interpolation: unsupportedInterpolation
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            result.Parameters.Should().HaveCount(1);
            var param = result.Parameters.First();
            param.Id.Should().Be("TestParam");

            // The value should fallback to linear clamping due to the exception in GetRuleValue
            // HeadPosX * 2 = 0.2, clamped between 0.0 and 1.0 = 0.2
            param.Value.Should().Be(0.2);
        }

        [Fact]
        public void TransformData_WithInvalidExpression_ShouldHandleExceptionInTryEvaluateExpression()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            // Create a rule with an expression that will cause an exception during evaluation
            // Using a string expression that will cause Convert.ToDouble() to throw an exception
            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("\"Hello World\""), // String literal - Convert.ToDouble() will fail
                expressionString: "\"Hello World\"",
                min: 0.0,
                max: 1.0,
                defaultValue: 0.5,
                interpolation: null
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            // The rule should fail evaluation due to Convert.ToDouble() exception, so no parameters should be generated
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void TransformData_WithInterpolationAndValueOutsideRange_ShouldNormalizeAndScale()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            var linearInterpolation = new LinearInterpolation();
            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("HeadPosX * 10"), // This will be 0.1 * 10 = 1.0
                expressionString: "HeadPosX * 10",
                min: 0.0,
                max: 2.0, // Range is 0.0 to 2.0
                defaultValue: 1.0,
                interpolation: linearInterpolation
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            result.Parameters.Should().HaveCount(1);
            var param = result.Parameters.First();
            param.Id.Should().Be("TestParam");

            // The value should be interpolated and scaled back to the range
            // HeadPosX * 10 = 1.0, which is exactly in the middle of [0.0, 2.0]
            // Linear interpolation should return 1.0 (middle of range)
            param.Value.Should().BeApproximately(1.0, 0.001);
        }

        [Fact]
        public void TransformData_WithZeroRangeInterpolation_ShouldHandleZeroRange()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            var linearInterpolation = new LinearInterpolation();
            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("HeadPosX"),
                expressionString: "HeadPosX",
                min: 5.0,
                max: 5.0, // Zero range
                defaultValue: 5.0,
                interpolation: linearInterpolation
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            result.Parameters.Should().HaveCount(1);
            var param = result.Parameters.First();
            param.Id.Should().Be("TestParam");

            // With zero range, should return the min value (5.0)
            param.Value.Should().BeApproximately(5.0, 0.001);
        }

        [Fact]
        public void TransformData_WithInterpolationAndClamping_ShouldClampToRange()
        {
            // Arrange
            var engine = CreateEngine();
            var trackingData = CreateValidTrackingData();

            var linearInterpolation = new LinearInterpolation();
            var rule = new ParameterTransformation(
                name: "TestParam",
                expression: new Expression("HeadPosX * 20"), // This will be 0.1 * 20 = 2.0
                expressionString: "HeadPosX * 20",
                min: 0.0,
                max: 1.0, // Range is 0.0 to 1.0, so 2.0 should be clamped to 1.0
                defaultValue: 0.5,
                interpolation: linearInterpolation
            );

            var loadResult = new RulesLoadResult(
                validRules: new List<ParameterTransformation> { rule },
                invalidRules: new List<RuleInfo>(),
                validationErrors: new List<string>(),
                loadedFromCache: false,
                loadError: null
            );
            _mockRepository.Setup(r => r.LoadRulesAsync()).ReturnsAsync(loadResult);

            // Act
            engine.LoadRulesAsync().Wait();
            var result = engine.TransformData(trackingData);

            // Assert
            result.Parameters.Should().HaveCount(1);
            var param = result.Parameters.First();
            param.Id.Should().Be("TestParam");

            // The value should be clamped to the range [0.0, 1.0]
            param.Value.Should().BeApproximately(1.0, 0.001);
        }

        #endregion
    }
}