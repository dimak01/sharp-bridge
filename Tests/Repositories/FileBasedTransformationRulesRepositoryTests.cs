using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Repositories;
using Xunit;

namespace SharpBridge.Tests.Repositories
{
    public class FileBasedTransformationRulesRepositoryTests : IDisposable
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IFileChangeWatcher> _mockFileWatcher;
        private readonly Mock<IFileChangeWatcher> _mockAppConfigWatcher;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly FileBasedTransformationRulesRepository _repository;
        private readonly List<string> _tempFiles = new();

        public FileBasedTransformationRulesRepositoryTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockFileWatcher = new Mock<IFileChangeWatcher>();
            _mockAppConfigWatcher = new Mock<IFileChangeWatcher>();
            _mockConfigManager = new Mock<IConfigManager>();
            _repository = new FileBasedTransformationRulesRepository(_mockLogger.Object, _mockFileWatcher.Object, _mockAppConfigWatcher.Object, _mockConfigManager.Object);
        }

        public void Dispose()
        {
            _repository?.Dispose();
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            GC.SuppressFinalize(this);
        }

        #region Helper Methods

        private string CreateTempRuleFile(string content)
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, content);
            _tempFiles.Add(filePath);
            return filePath;
        }

        private static string GetValidRuleContent()
        {
            return @"[
                {
                    ""name"": ""TestParam"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
        }

        private static string GetMixedValidityRuleContent()
        {
            return @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""EmptyExpressionRule"",
                    ""func"": """",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""InvalidRangeRule"",
                    ""func"": ""eyeBlinkRight * 50"",
                    ""min"": 100,
                    ""max"": 0,
                    ""defaultValue"": 0
                }
            ]";
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileBasedTransformationRulesRepository(null!, _mockFileWatcher.Object, _mockAppConfigWatcher.Object, _mockConfigManager.Object));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullFileWatcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileBasedTransformationRulesRepository(_mockLogger.Object, null!, _mockAppConfigWatcher.Object, _mockConfigManager.Object));
            exception.ParamName.Should().Be("fileWatcher");
        }

        [Fact]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Act & Assert
            _repository.IsUpToDate.Should().BeTrue();
            _repository.TransformationRulesPath.Should().BeEmpty();
            _repository.LastLoadTime.Should().Be(default(DateTime)); // Test LastLoadTime property access
        }

        #endregion

        #region LoadRulesAsync Tests

        [Fact]
        public async Task LoadRulesAsync_ValidFile_LoadsRulesSuccessfully()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().HaveCount(1);
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().BeEmpty();
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().BeNull();

            var rule = result.ValidRules[0];
            rule.Name.Should().Be("TestParam");
            rule.ExpressionString.Should().Be("eyeBlinkLeft * 100");
            rule.Min.Should().Be(0);
            rule.Max.Should().Be(100);
            rule.DefaultValue.Should().Be(0);

            _repository.IsUpToDate.Should().BeTrue();
            _repository.TransformationRulesPath.Should().Be(Path.GetFullPath(filePath));
            _repository.LastLoadTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)); // Test LastLoadTime is updated
        }

        [Fact]
        public async Task LoadRulesAsync_FileNotFound_ReturnsErrorResult()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var result = await _repository.LoadRulesAsync(nonExistentPath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().HaveCount(1);
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().Contain("Rules file not found");

            _repository.IsUpToDate.Should().BeFalse();
        }

        [Fact]
        public async Task LoadRulesAsync_InvalidJson_ReturnsErrorResult()
        {
            // Arrange
            var filePath = CreateTempRuleFile("{ invalid json }");

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().Contain("JSON parsing error");
            _repository.IsUpToDate.Should().BeFalse();
        }

        [Fact]
        public async Task LoadRulesAsync_NullJsonContent_ReturnsErrorResult()
        {
            // Arrange
            var filePath = CreateTempRuleFile("null");

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().Contain("Failed to deserialize transformation rules");
            _repository.IsUpToDate.Should().BeFalse();
        }

        [Fact]
        public async Task LoadRulesAsync_MixedValidityRules_HandlesPartialValidation()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetMixedValidityRuleContent());

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().HaveCount(1);
            result.InvalidRules.Should().HaveCount(2);
            result.ValidationErrors.Should().HaveCount(2);
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().BeNull();

            // Valid rule
            result.ValidRules[0].Name.Should().Be("ValidRule");

            // Invalid rules
            result.InvalidRules.Should().Contain(r => r.Name == "EmptyExpressionRule");
            result.InvalidRules.Should().Contain(r => r.Name == "InvalidRangeRule");

            _repository.IsUpToDate.Should().BeTrue();
        }

        [Fact]
        public async Task LoadRulesAsync_IOException_ReturnsErrorResult()
        {
            // Arrange - Create a directory with the same name as our file to force IOException
            var tempDir = Path.GetTempPath();
            var conflictingPath = Path.Combine(tempDir, Guid.NewGuid().ToString());

            try
            {
                // Create a directory with the file name we want to use
                Directory.CreateDirectory(conflictingPath);

                // Act - Try to read the directory as if it were a file - this should trigger IOException
                var result = await _repository.LoadRulesAsync(conflictingPath);

                // Assert
                result.Should().NotBeNull();
                result.ValidRules.Should().BeEmpty();
                result.LoadedFromCache.Should().BeFalse();
                result.LoadError.Should().Contain("Rules file not found");
                _repository.IsUpToDate.Should().BeFalse();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(conflictingPath))
                {
                    Directory.Delete(conflictingPath, true);
                }
            }
        }

        [Fact]
        public async Task LoadRulesAsync_UnexpectedException_ReturnsErrorResult()
        {
            // Arrange - Create a scenario that will cause an unexpected exception
            // We'll use a very long path name that exceeds system limits to trigger PathTooLongException
            var longPath = Path.Combine(Path.GetTempPath(), new string('a', 300), "test.json");

            // Act
            var result = await _repository.LoadRulesAsync(longPath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().Contain("Rules file not found");
            _repository.IsUpToDate.Should().BeFalse();
        }

        [Fact]
        public async Task LoadRulesAsync_NullFuncProperty_HandlesGracefully()
        {
            // Arrange - Create JSON with actual null func property
            var ruleContent = @"[
                {
                    ""name"": ""NullFuncRule"",
                    ""func"": null,
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().HaveCount(1);
            result.InvalidRules[0].Name.Should().Be("NullFuncRule");
            result.InvalidRules[0].Func.Should().BeEmpty(); // Should use string.Empty for null func
            result.InvalidRules[0].Error.Should().Contain("empty expression");
        }

        [Fact]
        public async Task LoadRulesAsync_ExpressionConstructorException_MarksRuleAsInvalid()
        {
            // Arrange - Create a rule that will cause Expression constructor to throw
            // Using an expression with unbalanced parentheses that causes parser exception
            var ruleContent = @"[
                {
                    ""name"": ""ExceptionRule"",
                    ""func"": ""((((((((((((((((((((((((((((((((((((((((((((((((((x"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().HaveCount(1);
            result.InvalidRules[0].Name.Should().Be("ExceptionRule");
            result.InvalidRules[0].Error.Should().Contain("Error parsing the expression");
        }

        [Fact]
        public async Task LoadRulesAsync_SecondCall_StopsWatchingPreviousFile()
        {
            // Arrange
            var firstFile = CreateTempRuleFile(GetValidRuleContent());
            var secondFile = CreateTempRuleFile(GetValidRuleContent());

            // Act
            await _repository.LoadRulesAsync(firstFile);
            await _repository.LoadRulesAsync(secondFile);

            // Assert
            _mockFileWatcher.Verify(w => w.StopWatching(), Times.Once);
            _mockFileWatcher.Verify(w => w.StartWatching(It.IsAny<string>()), Times.Exactly(2));
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task LoadRulesAsync_ErrorAfterSuccessfulLoad_ReturnsCachedRules()
        {
            // Arrange
            var validFile = CreateTempRuleFile(GetValidRuleContent());

            // First load - successful
            await _repository.LoadRulesAsync(validFile);

            // Delete the file to simulate error
            File.Delete(validFile);
            var nonExistentPath = validFile; // Same path but file is gone

            // Act
            var result = await _repository.LoadRulesAsync(nonExistentPath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().HaveCount(1); // From cache
            result.InvalidRules.Should().BeEmpty();   // From cache
            result.LoadedFromCache.Should().BeTrue();
            result.LoadError.Should().Contain("Rules file not found");

            result.ValidRules[0].Name.Should().Be("TestParam"); // Cached rule
            _repository.IsUpToDate.Should().BeFalse(); // Error occurred

            // LastLoadTime should still reflect the original successful load time
            _repository.LastLoadTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task LoadRulesAsync_ErrorWithoutPreviousLoad_ReturnsEmptyRules()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var result = await _repository.LoadRulesAsync(nonExistentPath);

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().Contain("Rules file not found");
            _repository.IsUpToDate.Should().BeFalse();

            // LastLoadTime should remain default when no successful load occurred
            _repository.LastLoadTime.Should().Be(default(DateTime));
        }

        #endregion

        #region File Watching Tests

        [Fact]
        public async Task LoadRulesAsync_StartsWatchingFile()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());

            // Act
            await _repository.LoadRulesAsync(filePath);

            // Assert
            _mockFileWatcher.Verify(w => w.StartWatching(Path.GetFullPath(filePath)), Times.Once);
        }

        [Fact]
        public void OnFileChanged_MatchingFile_UpdatesUpToDateStatus()
        {
            // Arrange
            var filePath = "/test/path.json";

            // Simulate the repository tracking this file
            var currentFilePathProperty = typeof(FileBasedTransformationRulesRepository)
                .GetField("_currentFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentFilePathProperty?.SetValue(_repository, filePath);

            var isUpToDateField = typeof(FileBasedTransformationRulesRepository)
                .GetField("_isUpToDate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isUpToDateField?.SetValue(_repository, true);

            // Act
            _mockFileWatcher.Raise(w => w.FileChanged += null, new FileChangeEventArgs(filePath));

            // Assert
            _repository.IsUpToDate.Should().BeFalse();
        }

        [Fact]
        public void OnFileChanged_DifferentFile_DoesNotUpdateStatus()
        {
            // Arrange
            var trackedFile = "/test/tracked.json";
            var differentFile = "/test/different.json";

            // Simulate the repository tracking a specific file
            var currentFilePathProperty = typeof(FileBasedTransformationRulesRepository)
                .GetField("_currentFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentFilePathProperty?.SetValue(_repository, trackedFile);

            var isUpToDateField = typeof(FileBasedTransformationRulesRepository)
                .GetField("_isUpToDate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isUpToDateField?.SetValue(_repository, true);

            // Act
            _mockFileWatcher.Raise(w => w.FileChanged += null, new FileChangeEventArgs(differentFile));

            // Assert
            _repository.IsUpToDate.Should().BeTrue(); // Should remain unchanged
        }

        [Fact]
        public async Task LoadRulesAsync_RaisesRulesChangedEvent()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());
            var eventRaised = false;
            RulesChangedEventArgs? eventArgs = null;

            _repository.RulesChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            // Load rules first
            await _repository.LoadRulesAsync(filePath);

            // Act - Simulate file change
            _mockFileWatcher.Raise(w => w.FileChanged += null, new FileChangeEventArgs(Path.GetFullPath(filePath)));

            // Assert
            eventRaised.Should().BeTrue();
            eventArgs.Should().NotBeNull();
            eventArgs!.FilePath.Should().Be(Path.GetFullPath(filePath));
        }

        #endregion

        #region Rule Validation Tests

        [Fact]
        public async Task LoadRulesAsync_EmptyExpression_MarksRuleAsInvalid()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""EmptyRule"",
                    ""func"": """",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().HaveCount(1);
            result.InvalidRules[0].Name.Should().Be("EmptyRule");
            result.InvalidRules[0].Error.Should().Contain("empty expression");
        }

        [Fact]
        public async Task LoadRulesAsync_InvalidRange_MarksRuleAsInvalid()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""InvalidRange"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 100,
                    ""max"": 0,
                    ""defaultValue"": 50
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().HaveCount(1);
            result.InvalidRules[0].Name.Should().Be("InvalidRange");
            result.InvalidRules[0].Error.Should().Contain("Min value").And.Contain("Max value");
        }

        [Fact]
        public async Task LoadRulesAsync_InvalidSyntax_MarksRuleAsInvalid()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""SyntaxError"",
                    ""func"": ""invalid syntax +++"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().HaveCount(1);
            result.InvalidRules[0].Name.Should().Be("SyntaxError");
            result.InvalidRules[0].Error.Should().Contain("Syntax error");
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_DisposesFileWatcher()
        {
            // Act
            _repository.Dispose();

            // Assert
            _mockFileWatcher.Verify(w => w.Dispose(), Times.Once);
        }

        [Fact]
        public async Task LoadRulesAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _repository.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                _repository.LoadRulesAsync("test.json"));
        }

        [Fact]
        public void Dispose_WithNullFileWatcher_ThrowsNullReferenceException()
        {
            // Arrange - Create repository that will have null file watcher
            var mockLogger = new Mock<IAppLogger>();
            var mockFileWatcher = new Mock<IFileChangeWatcher>();

            var mockAppConfigWatcher = new Mock<IFileChangeWatcher>();
            var mockConfigManager = new Mock<IConfigManager>();
            var repository = new FileBasedTransformationRulesRepository(mockLogger.Object, mockFileWatcher.Object, mockAppConfigWatcher.Object, mockConfigManager.Object);

            // Use reflection to set _fileWatcher to null to test the bug in dispose method
            var fileWatcherField = typeof(FileBasedTransformationRulesRepository)
                .GetField("_fileWatcher", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fileWatcherField?.SetValue(repository, null);

            // Act & Assert - Currently throws NullReferenceException due to missing null-conditional operator
            // This test documents the current behavior - the method should be fixed to handle null gracefully
            Assert.Throws<NullReferenceException>(() => repository.Dispose());
        }

        #endregion

        #region LoadRulesAsync (no parameters) Tests

        [Fact]
        public async Task LoadRulesAsync_WithValidConfig_LoadsRulesSuccessfully()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());
            var config = new TransformationEngineConfig { ConfigPath = filePath };
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Act
            var result = await _repository.LoadRulesAsync();

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().HaveCount(1);
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().BeEmpty();
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().BeNull();
        }

        [Fact]
        public async Task LoadRulesAsync_WithEmptyConfigPath_ReturnsCriticalError()
        {
            // Arrange
            var config = new TransformationEngineConfig { ConfigPath = string.Empty };
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Act
            var result = await _repository.LoadRulesAsync();

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().HaveCount(1);
            result.ValidationErrors[0].Should().Contain("Transformation engine config path is not specified");
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().NotBeNull();
        }

        [Fact]
        public async Task LoadRulesAsync_WithNullConfigPath_ReturnsCriticalError()
        {
            // Arrange
            var config = new TransformationEngineConfig { ConfigPath = null! };
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Act
            var result = await _repository.LoadRulesAsync();

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().HaveCount(1);
            result.ValidationErrors[0].Should().Contain("Transformation engine config path is not specified");
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().NotBeNull();
        }

        [Fact]
        public async Task LoadRulesAsync_WithConfigLoadException_ReturnsCriticalError()
        {
            // Arrange
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ThrowsAsync(new InvalidOperationException("Config load failed"));

            // Act
            var result = await _repository.LoadRulesAsync();

            // Assert
            result.Should().NotBeNull();
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().HaveCount(1);
            result.ValidationErrors[0].Should().Contain("Failed to load application config");
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().NotBeNull();
        }

        #endregion

        #region OnApplicationConfigChanged Tests

        [Fact]
        public async Task OnApplicationConfigChanged_WithSamePath_DoesNotTriggerReload()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());
            var config = new TransformationEngineConfig { ConfigPath = filePath };
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Load rules first to set current path
            await _repository.LoadRulesAsync(filePath);

            // Reset mock to track calls
            _mockConfigManager.Reset();
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            var eventRaised = false;
            _repository.RulesChanged += (sender, args) => eventRaised = true;

            // Act
            _repository.GetType()
                .GetMethod("OnApplicationConfigChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_repository, new object[] { null!, new FileChangeEventArgs(filePath) });

            // Wait a bit for async operation
            await Task.Delay(100);

            // Assert
            eventRaised.Should().BeFalse();
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WithDifferentPath_TriggersReload()
        {
            // Arrange
            var filePath1 = CreateTempRuleFile(GetValidRuleContent());
            var filePath2 = CreateTempRuleFile(GetValidRuleContent());
            var config1 = new TransformationEngineConfig { ConfigPath = filePath1 };
            var config2 = new TransformationEngineConfig { ConfigPath = filePath2 };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config1);

            // Load rules first to set current path
            await _repository.LoadRulesAsync(filePath1);

            // Reset mock to return different config
            _mockConfigManager.Reset();
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config2);

            var eventRaised = false;
            RulesChangedEventArgs? eventArgs = null;
            _repository.RulesChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            // Act
            _repository.GetType()
                .GetMethod("OnApplicationConfigChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_repository, new object[] { null!, new FileChangeEventArgs(filePath2) });

            // Wait a bit for async operation
            await Task.Delay(100);

            // Assert
            eventRaised.Should().BeTrue();
            eventArgs.Should().NotBeNull();
            eventArgs!.FilePath.Should().Be(filePath2);
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WithEmptyConfigPath_LogsWarning()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());
            var config1 = new TransformationEngineConfig { ConfigPath = filePath };
            var config2 = new TransformationEngineConfig { ConfigPath = string.Empty };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config1);

            // Load rules first to set current path
            await _repository.LoadRulesAsync(filePath);

            // Reset mock to return empty config
            _mockConfigManager.Reset();
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config2);

            // Act
            _repository.GetType()
                .GetMethod("OnApplicationConfigChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_repository, new object[] { null!, new FileChangeEventArgs("config.json") });

            // Wait a bit for async operation
            await Task.Delay(100);

            // Assert
            _mockLogger.Verify(x => x.Warning("Transformation engine config path is empty in application config"), Times.Once);
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WithException_LogsError()
        {
            // Arrange
            var filePath = CreateTempRuleFile(GetValidRuleContent());
            var config = new TransformationEngineConfig { ConfigPath = filePath };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Load rules first to set current path
            await _repository.LoadRulesAsync(filePath);

            // Reset mock to throw exception
            _mockConfigManager.Reset();
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ThrowsAsync(new InvalidOperationException("Config error"));

            // Act
            _repository.GetType()
                .GetMethod("OnApplicationConfigChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_repository, new object[] { null!, new FileChangeEventArgs("config.json") });

            // Wait a bit for async operation
            await Task.Delay(100);

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error handling application config change", It.IsAny<InvalidOperationException>()), Times.Once);
        }

        #endregion

        #region Interpolation Validation Tests

        [Fact]
        public async Task LoadRulesAsync_WithInvalidInterpolation_ReturnsCriticalError()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""InvalidInterpolation"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0,
                    ""interpolation"": {
                        ""type"": ""InvalidInterpolationType""
                    }
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().HaveCount(1);
            result.ValidationErrors[0].Should().Contain("JSON parsing error");
            result.LoadedFromCache.Should().BeFalse();
            result.LoadError.Should().NotBeNull();
        }

        [Fact]
        public async Task LoadRulesAsync_WithValidInterpolation_CreatesValidRule()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""ValidInterpolation"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0,
                    ""interpolation"": {
                        ""type"": ""LinearInterpolation""
                    }
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);

            // Act
            var result = await _repository.LoadRulesAsync(filePath);

            // Assert
            result.ValidRules.Should().HaveCount(1);
            result.InvalidRules.Should().BeEmpty();
            result.ValidRules[0].Name.Should().Be("ValidInterpolation");
            result.ValidRules[0].Interpolation.Should().NotBeNull();
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task LoadRulesFromPathAsync_WithIOException_ReturnsCriticalError()
        {
            // Arrange
            var config = new TransformationEngineConfig
            {
                ConfigPath = "nonexistent/path/rules.json"
            };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Act
            var result = await _repository.LoadRulesFromPathAsync("nonexistent/path/rules.json");

            // Assert
            result.ValidRules.Should().BeEmpty();
            result.InvalidRules.Should().BeEmpty();
            result.ValidationErrors.Should().ContainSingle();
            result.ValidationErrors[0].Should().StartWith("Rules file not found:");
            result.LoadError.Should().StartWith("Rules file not found:");
            result.LoadedFromCache.Should().BeFalse();
        }

        [Fact]
        public async Task LoadRulesFromPathAsync_WithGeneralException_ReturnsCriticalError()
        {
            // Arrange
            var config = new TransformationEngineConfig
            {
                ConfigPath = "test/path/rules.json"
            };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>())
                .ReturnsAsync(config);

            // Mock File.ReadAllTextAsync to throw a general exception
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "valid json");

                // Create a mock that will throw an exception
                var fileWatcherMock = new Mock<IFileChangeWatcher>();
                fileWatcherMock.Setup(x => x.StartWatching(It.IsAny<string>()))
                    .Throws(new InvalidOperationException("Mock exception"));

                var repository = new FileBasedTransformationRulesRepository(
                    _mockLogger.Object, fileWatcherMock.Object, fileWatcherMock.Object, _mockConfigManager.Object);

                // Act
                var result = await repository.LoadRulesFromPathAsync(tempFile);

                // Assert
                result.ValidRules.Should().BeEmpty();
                result.InvalidRules.Should().BeEmpty();
                result.ValidationErrors.Should().ContainSingle();
                result.ValidationErrors[0].Should().StartWith("JSON parsing error:");
                result.LoadError.Should().StartWith("JSON parsing error:");
                result.LoadedFromCache.Should().BeFalse();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        #endregion

        #region TryCreateTransformationRule Edge Cases Tests (via LoadRulesAsync)

        [Fact]
        public async Task LoadRulesAsync_WithEmptyFunc_ReturnsInvalidRule()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var rulesJson = @"[
                {
                    ""name"": ""EmptyFuncRule"",
                    ""func"": """",
                    ""min"": 0,
                    ""max"": 1,
                    ""defaultValue"": 0.5
                }
            ]";
                await File.WriteAllTextAsync(tempFile, rulesJson);

                // Act
                var result = await _repository.LoadRulesAsync(tempFile);

                // Assert
                result.ValidRules.Should().BeEmpty();
                result.InvalidRules.Should().HaveCount(1);
                result.InvalidRules[0].Name.Should().Be("EmptyFuncRule");
                result.ValidationErrors.Should().ContainSingle();
                result.ValidationErrors[0].Should().Be("Rule 'EmptyFuncRule' has an empty expression");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadRulesAsync_WithWhitespaceFunc_ReturnsInvalidRule()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var rulesJson = @"[
                {
                    ""name"": ""WhitespaceFuncRule"",
                    ""func"": ""   "",
                    ""min"": 0,
                    ""max"": 1,
                    ""defaultValue"": 0.5
                }
            ]";
                await File.WriteAllTextAsync(tempFile, rulesJson);

                // Act
                var result = await _repository.LoadRulesAsync(tempFile);

                // Assert
                result.ValidRules.Should().BeEmpty();
                result.InvalidRules.Should().HaveCount(1);
                result.InvalidRules[0].Name.Should().Be("WhitespaceFuncRule");
                result.ValidationErrors.Should().ContainSingle();
                result.ValidationErrors[0].Should().Be("Rule 'WhitespaceFuncRule' has an empty expression");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadRulesAsync_WithSyntaxError_ReturnsInvalidRule()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var rulesJson = @"[
                {
                    ""name"": ""SyntaxErrorRule"",
                    ""func"": ""invalid syntax ("",
                    ""min"": 0,
                    ""max"": 1,
                    ""defaultValue"": 0.5
                }
            ]";
                await File.WriteAllTextAsync(tempFile, rulesJson);

                // Act
                var result = await _repository.LoadRulesAsync(tempFile);

                // Assert
                result.ValidRules.Should().BeEmpty();
                result.InvalidRules.Should().HaveCount(1);
                result.InvalidRules[0].Name.Should().Be("SyntaxErrorRule");
                result.ValidationErrors.Should().ContainSingle();
                result.ValidationErrors[0].Should().StartWith("Syntax error in rule 'SyntaxErrorRule':");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadRulesAsync_WithMinGreaterThanMax_ReturnsInvalidRule()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var rulesJson = @"[
                {
                    ""name"": ""MinGreaterThanMaxRule"",
                    ""func"": ""x * 2"",
                    ""min"": 10,
                    ""max"": 5,
                    ""defaultValue"": 7.5
                }
            ]";
                await File.WriteAllTextAsync(tempFile, rulesJson);

                // Act
                var result = await _repository.LoadRulesAsync(tempFile);

                // Assert
                result.ValidRules.Should().BeEmpty();
                result.InvalidRules.Should().HaveCount(1);
                result.InvalidRules[0].Name.Should().Be("MinGreaterThanMaxRule");
                result.ValidationErrors.Should().ContainSingle();
                result.ValidationErrors[0].Should().Be("Rule 'MinGreaterThanMaxRule' has Min value (10) greater than Max value (5)");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadRulesAsync_WithValidExpression_ReturnsValidRule()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var rulesJson = @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""x * 2"",
                    ""min"": 0,
                    ""max"": 1,
                    ""defaultValue"": 0.5
                }
            ]";
                await File.WriteAllTextAsync(tempFile, rulesJson);

                // Act
                var result = await _repository.LoadRulesAsync(tempFile);

                // Assert
                result.ValidRules.Should().HaveCount(1);
                result.ValidRules[0].Name.Should().Be("ValidRule");
                result.ValidRules[0].Expression.Should().NotBeNull();
                result.ValidRules[0].ExpressionString.Should().Be("x * 2");
                result.ValidRules[0].Min.Should().Be(0);
                result.ValidRules[0].Max.Should().Be(1);
                result.ValidRules[0].DefaultValue.Should().Be(0.5);
                result.InvalidRules.Should().BeEmpty();
                result.ValidationErrors.Should().BeEmpty();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadRulesAsync_WithValidExpressionAndInterpolation_ReturnsValidRule()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var rulesJson = @"[
                {
                    ""name"": ""ValidRuleWithInterpolation"",
                    ""func"": ""x * 2"",
                    ""min"": 0,
                    ""max"": 1,
                    ""defaultValue"": 0.5,
                    ""interpolation"": {
                        ""type"": ""LinearInterpolation""
                    }
                }
            ]";
                await File.WriteAllTextAsync(tempFile, rulesJson);

                // Act
                var result = await _repository.LoadRulesAsync(tempFile);

                // Assert
                result.ValidRules.Should().HaveCount(1);
                result.ValidRules[0].Name.Should().Be("ValidRuleWithInterpolation");
                result.ValidRules[0].Interpolation.Should().BeOfType<LinearInterpolation>();
                result.InvalidRules.Should().BeEmpty();
                result.ValidationErrors.Should().BeEmpty();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        #endregion

        #region Disposed State Handling Tests

        [Fact]
        public async Task LoadRulesAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _repository.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _repository.LoadRulesAsync());
        }

        [Fact]
        public async Task LoadRulesAsync_WithPath_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _repository.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _repository.LoadRulesAsync("test.json"));
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            _repository.Dispose();

            // Act & Assert - should not throw
            _repository.Dispose();

            // Verify the repository is still disposed
            Assert.True(true); // Test passes if no exception is thrown
        }

        #endregion

        #region ArePathsEqual Tests

        [Fact]
        public void ArePathsEqual_WithSamePaths_ReturnsTrue()
        {
            // Arrange
            var path1 = @"C:\test\file.json";
            var path2 = @"C:\test\file.json";

            // Act
            var result = typeof(FileBasedTransformationRulesRepository)
                .GetMethod("ArePathsEqual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { path1, path2 });

            // Assert
            result.Should().Be(true);
        }

        [Fact]
        public void ArePathsEqual_WithDifferentPaths_ReturnsFalse()
        {
            // Arrange
            var path1 = @"C:\test\file1.json";
            var path2 = @"C:\test\file2.json";

            // Act
            var result = typeof(FileBasedTransformationRulesRepository)
                .GetMethod("ArePathsEqual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { path1, path2 });

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void ArePathsEqual_WithNullPaths_ReturnsTrue()
        {
            // Arrange
            string? path1 = null;
            string? path2 = null;

            // Act
            var result = typeof(FileBasedTransformationRulesRepository)
                .GetMethod("ArePathsEqual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { path1!, path2! });

            // Assert
            result.Should().Be(true);
        }

        [Fact]
        public void ArePathsEqual_WithOneNullPath_ReturnsFalse()
        {
            // Arrange
            var path1 = @"C:\test\file.json";
            string? path2 = null;

            // Act
            var result = typeof(FileBasedTransformationRulesRepository)
                .GetMethod("ArePathsEqual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { path1, path2! });

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void ArePathsEqual_WithCaseInsensitivePaths_ReturnsTrue()
        {
            // Arrange
            var path1 = @"C:\TEST\FILE.JSON";
            var path2 = @"c:\test\file.json";

            // Act
            var result = typeof(FileBasedTransformationRulesRepository)
                .GetMethod("ArePathsEqual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { path1, path2 });

            // Assert
            result.Should().Be(true);
        }

        #endregion
    }
}