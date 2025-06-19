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
        private readonly FileBasedTransformationRulesRepository _repository;
        private readonly List<string> _tempFiles = new();

        public FileBasedTransformationRulesRepositoryTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockFileWatcher = new Mock<IFileChangeWatcher>();
            _repository = new FileBasedTransformationRulesRepository(_mockLogger.Object, _mockFileWatcher.Object);
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
        }

        #region Helper Methods

        private string CreateTempRuleFile(string content)
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, content);
            _tempFiles.Add(filePath);
            return filePath;
        }

        private string GetValidRuleContent()
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

        private string GetMixedValidityRuleContent()
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
                new FileBasedTransformationRulesRepository(null!, _mockFileWatcher.Object));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullFileWatcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new FileBasedTransformationRulesRepository(_mockLogger.Object, null!));
            exception.ParamName.Should().Be("fileWatcher");
        }

        [Fact]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Act & Assert
            _repository.IsUpToDate.Should().BeTrue();
            _repository.CurrentFilePath.Should().BeEmpty();
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
            _repository.CurrentFilePath.Should().Be(Path.GetFullPath(filePath));
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

        #endregion
    }
} 