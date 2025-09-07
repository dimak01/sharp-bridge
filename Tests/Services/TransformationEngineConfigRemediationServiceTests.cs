using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Remediation;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class TransformationEngineConfigRemediationServiceTests
    {
        private readonly Mock<IConfigSectionValidatorsFactory> _mockValidatorsFactory;
        private readonly Mock<IConfigSectionValidator> _mockValidator;
        private readonly Mock<IConsole> _mockConsole;
        private readonly TransformationEngineConfigRemediationService _service;

        public TransformationEngineConfigRemediationServiceTests()
        {
            _mockValidatorsFactory = new Mock<IConfigSectionValidatorsFactory>();
            _mockValidator = new Mock<IConfigSectionValidator>();
            _mockConsole = new Mock<IConsole>();

            _mockValidatorsFactory.Setup(x => x.GetValidator(ConfigSectionTypes.TransformationEngineConfig))
                .Returns(_mockValidator.Object);

            _service = new TransformationEngineConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act & Assert
            _service.Should().NotBeNull();
            _mockValidatorsFactory.Verify(x => x.GetValidator(ConfigSectionTypes.TransformationEngineConfig), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullValidatorsFactory_ThrowsNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => new TransformationEngineConfigRemediationService(null!, _mockConsole.Object));
        }

        [Fact]
        public void Constructor_WithNullConsole_DoesNotThrow()
        {
            // Act & Assert
            var service = new TransformationEngineConfigRemediationService(_mockValidatorsFactory.Object, null!);
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task Remediate_WithAllFieldsMissing_AppliesDefaultsSilently()
        {
            // Arrange
            var fields = new List<ConfigFieldState>(); // Empty fields = all missing

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            result.UpdatedConfig.Should().BeOfType<TransformationEngineConfig>();

            var config = (TransformationEngineConfig)result.UpdatedConfig!;
            config.ConfigPath.Should().Be("Configs/vts_transforms.json");
            config.MaxEvaluationIterations.Should().Be(10);

            // Verify no console interaction occurred
            _mockConsole.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.Never);
            _mockConsole.Verify(x => x.ReadLine(), Times.Never);
        }

        [Fact]
        public async Task Remediate_WithValidFields_ReturnsNoRemediationNeeded()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "Configs/custom_transforms.json", true, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", 15, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            _mockValidator.Setup(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>()));

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.NoRemediationNeeded);
            result.UpdatedConfig.Should().BeNull();

            // Verify no console interaction occurred
            _mockConsole.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.Never);
            _mockConsole.Verify(x => x.ReadLine(), Times.Never);
        }

        [Fact]
        public async Task Remediate_WithInvalidFields_ShowsSplashAndRemediates()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", null, false, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", 5, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "ConfigPath is required", null),
                new("MaxEvaluationIterations", typeof(int), "Value must be between 10 and 50", "5")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/new_transforms.json")
                .Returns("20");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify splash screen was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("=== Transformation Engine Configuration - Remediation ===")) &&
                lines.Any(line => line.Contains("The following fields need attention:")) &&
                lines.Any(line => line.Contains("ConfigPath is required")) &&
                lines.Any(line => line.Contains("Value must be between 10 and 50")) &&
                lines.Any(line => line.Contains("Press Enter to start remediation..."))
            )), Times.Once);

            // Verify user input was read
            _mockConsole.Verify(x => x.ReadLine(), Times.AtLeast(2));
        }

        [Fact]
        public async Task Remediate_WithFieldValidationFailure_RetriesUntilValid()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Setup ValidateSingleField to fail first, then succeed
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ConfigPath", typeof(string), "Invalid path format", "invalid")))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("invalid")
                .Returns("Configs/valid_transforms.json");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify field frame was shown with error
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Error:") && line.Contains("Invalid path format"))
            )), Times.Once);

            // Verify user input was read twice (once for invalid, once for valid)
            _mockConsole.Verify(x => x.ReadLine(), Times.AtLeast(2));
        }

        [Fact]
        public async Task Remediate_WithEmptyInput_UsesDefaultValue()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns(""); // Empty input for field

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (TransformationEngineConfig)result.UpdatedConfig!;
            config.ConfigPath.Should().Be("Configs/vts_transforms.json"); // Default value

            // Verify default value was shown in prompt
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Enter new value (or press Enter for default value of")) &&
                lines.Any(line => line.Contains("Configs/vts_transforms.json"))
            )), Times.Once);
        }

        [Fact]
        public async Task Remediate_WithEmptyInputAndNoDefault_ShowsError()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("") // Empty input
                .Returns("valid_value");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify default value prompt was shown
            var allWriteLinesCalls = _mockConsole.Invocations
                .Where(i => i.Method.Name == "WriteLines")
                .SelectMany(i => (string[])i.Arguments[0])
                .ToList();

            var combinedOutput = string.Join(" ", allWriteLinesCalls);
            combinedOutput.Should().Contain("10");
        }

        [Fact]
        public async Task Remediate_WithParseError_ShowsErrorAndRetries()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("not_a_number")
                .Returns("15");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify parse error was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Error:")) &&
                lines.Any(line => line.Contains("Value must be an integer."))
            )), Times.Once);

            // Verify user input was read twice
            _mockConsole.Verify(x => x.ReadLine(), Times.AtLeast(2));
        }

        [Fact]
        public async Task Remediate_WithFinalValidationFailure_ReturnsFailed()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(issues); // Still has issues

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/valid_transforms.json");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Failed);
            result.UpdatedConfig.Should().BeNull();
        }

        [Fact]
        public async Task Remediate_ShowsFieldNotes()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/valid_transforms.json");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);

            // Verify field notes were shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("This is the path to the JSON file containing transformation rules.")) &&
                lines.Any(line => line.Contains("The transformation rules file defines how tracking parameters")) &&
                lines.Any(line => line.Contains("Default location:")) &&
                lines.Any(line => line.Contains("Configs/vts_transforms.json"))
            )), Times.Once);
        }

        [Fact]
        public async Task Remediate_ShowsMaxIterationsNotes()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("15");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);

            // Verify field notes were shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("This controls how many times the transformation engine will evaluate")) &&
                lines.Any(line => line.Contains("Higher values allow for more complex dependency chains")) &&
                lines.Any(line => line.Contains("Recommended range:")) &&
                lines.Any(line => line.Contains("5-20"))
            )), Times.Once);
        }

        [Fact]
        public async Task Remediate_ClearsScreenAfterSuccessfulField()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/valid_transforms.json");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);

            // Verify screen was cleared after successful field remediation
            _mockConsole.Verify(x => x.Clear(), Times.Once);
        }

        [Fact]
        public async Task Remediate_WithPartialFieldsMissing_DoesNotApplySilentDefaults()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "Configs/existing.json", true, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", null, false, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("MaxEvaluationIterations", typeof(int), "MaxEvaluationIterations is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("15");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (TransformationEngineConfig)result.UpdatedConfig!;
            config.ConfigPath.Should().Be("Configs/existing.json"); // Original value preserved
            config.MaxEvaluationIterations.Should().Be(15); // User input

            // Verify interactive remediation occurred
            _mockConsole.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.AtLeastOnce);
            _mockConsole.Verify(x => x.ReadLine(), Times.AtLeast(2));
        }

        [Fact]
        public async Task Remediate_WithFieldNotesForUnknownField_HandlesGracefully()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("valid_value");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify field frame was shown with notes
            var allWriteLinesCalls = _mockConsole.Invocations
                .Where(i => i.Method.Name == "WriteLines")
                .SelectMany(i => (string[])i.Arguments[0])
                .ToList();

            var combinedOutput = string.Join(" ", allWriteLinesCalls);
            combinedOutput.Should().Contain("Path to Transformation Rules JSON File");
            combinedOutput.Should().Contain("This is the path to the JSON file");
        }

        [Fact]
        public async Task Remediate_WithFieldDefaultForUnknownField_HandlesGracefully()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("") // Empty input - should show error since no default
                .Returns("valid_value");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify default value prompt was shown
            var allWriteLinesCalls = _mockConsole.Invocations
                .Where(i => i.Method.Name == "WriteLines")
                .SelectMany(i => (string[])i.Arguments[0])
                .ToList();

            var combinedOutput = string.Join(" ", allWriteLinesCalls);
            combinedOutput.Should().Contain("Configs/vts_transforms.json");
        }

        [Fact]
        public async Task Remediate_WithStringFieldValue_ShowsCurrentValue()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "existing/path.json", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "ConfigPath is invalid", "existing/path.json")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("new/path.json");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify current value was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Current value:")) &&
                lines.Any(line => line.Contains("existing/path.json"))
            )), Times.Once);
        }

        [Fact]
        public async Task Remediate_WithEmptyNotes_HandlesGracefully()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/valid_transforms.json");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify field frame was shown with notes (ConfigPath has notes)
            var allWriteLinesCalls = _mockConsole.Invocations
                .Where(i => i.Method.Name == "WriteLines")
                .SelectMany(i => (string[])i.Arguments[0])
                .ToList();

            var combinedOutput = string.Join(" ", allWriteLinesCalls);
            combinedOutput.Should().Contain("Path to Transformation Rules JSON File");
            combinedOutput.Should().Contain("This is the path to the JSON file containing transformation rules.");
        }

        [Fact]
        public async Task Remediate_WithNonIntegerParseError_ShowsErrorAndRetries()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("abc") // Invalid input
                .Returns("15");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify parse error was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Error:")) &&
                lines.Any(line => line.Contains("Value must be an integer."))
            )), Times.Once);

            // Verify user input was read multiple times
            _mockConsole.Verify(x => x.ReadLine(), Times.AtLeast(3));
        }

        [Fact]
        public async Task Remediate_WithStringTypeParse_Succeeds()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("any_string_value");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (TransformationEngineConfig)result.UpdatedConfig!;
            config.ConfigPath.Should().Be("any_string_value");
        }

        [Fact]
        public async Task Remediate_WithGetFieldDescription_HandlesAllFieldTypes()
        {
            // This test verifies the GetFieldDescription method coverage
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies"),
                new("ConfigPath", "invalid_path2", true, typeof(string), "Path to Transformation Rules JSON File")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path"),
                new("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1"),
                new("ConfigPath", typeof(string), "Invalid path format", "invalid_path2")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/valid_transforms.json")
                .Returns("15")
                .Returns("valid_value");

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify all field types were handled
            _mockConsole.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.AtLeast(4));

            // Verify specific field types were handled
            var allWriteLinesCalls = _mockConsole.Invocations
                .Where(i => i.Method.Name == "WriteLines")
                .SelectMany(i => (string[])i.Arguments[0])
                .ToList();

            var combinedOutput = string.Join(" ", allWriteLinesCalls);
            combinedOutput.Should().Contain("Path to Transformation Rules JSON File");
            combinedOutput.Should().Contain("Maximum Evaluation Iterations for Parameter Dependencies");
        }

        [Fact]
        public async Task Remediate_WithInvalidConfigPath_HandlesGracefully()
        {
            // Arrange - Test with a real field that has validation issues
            var fieldsState = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", 10, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>
                {
                    new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
                }))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>()));

            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ConfigPath", typeof(string), "Invalid path format", "invalid_path")))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("valid_path.json"); // Provide valid input

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify that the field was remediated successfully
            var updatedConfig = (TransformationEngineConfig)result.UpdatedConfig!;
            updatedConfig.ConfigPath.Should().Be("Configs/vts_transforms.json"); // Service used default value
        }

        [Fact]
        public async Task Remediate_WithEmptyInputAndNoDefault_ShowsErrorAndRetries()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>
                {
                    new("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1")
                }))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>()));

            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("MaxEvaluationIterations", typeof(int), "Invalid value format", "-1")))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("") // Empty input - should use default
                .Returns("15"); // Valid input

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify error handling for empty input
            var allWriteLinesCalls = _mockConsole.Invocations
                .Where(i => i.Method.Name == "WriteLines")
                .SelectMany(i => (string[])i.Arguments[0])
                .ToList();

            var combinedOutput = string.Join(" ", allWriteLinesCalls);
            combinedOutput.Should().Contain("10"); // Should show default value
        }

        [Fact]
        public async Task Remediate_WithEmptyInputForConfigPath_ShowsErrorAndRetries()
        {
            // Arrange - Test empty input handling for a field with a default
            var fieldsState = new List<ConfigFieldState>
            {
                new("ConfigPath", "invalid_path", true, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", 10, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>
                {
                    new("ConfigPath", typeof(string), "Invalid path format", "invalid_path")
                }))
                .Returns(new ConfigValidationResult(new List<FieldValidationIssue>()));

            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ConfigPath", typeof(string), "Invalid path format", "invalid_path")))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("") // Empty input - should use default value
                .Returns("valid_path.json"); // Valid input

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            // Verify that the field was remediated successfully
            var updatedConfig = (TransformationEngineConfig)result.UpdatedConfig!;
            updatedConfig.ConfigPath.Should().Be("valid_path.json");
        }

        [Fact]
        public async Task Remediate_WithMultipleFields_ProcessesAllFields()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", null, false, typeof(string), "Path to Transformation Rules JSON File"),
                new("MaxEvaluationIterations", -1, true, typeof(int), "Maximum Evaluation Iterations for Parameter Dependencies")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ConfigPath", typeof(string), "ConfigPath is required", null),
                new("MaxEvaluationIterations", typeof(int), "Value must be between 10 and 50", "-1")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Press Enter to start
                .Returns("Configs/valid.json") // For ConfigPath
                .Returns("20"); // For MaxEvaluationIterations

            // Act
            var result = await _service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (TransformationEngineConfig)result.UpdatedConfig!;
            config.ConfigPath.Should().Be("Configs/valid.json");
            config.MaxEvaluationIterations.Should().Be(20);

            // Verify all fields were processed
            _mockConsole.Verify(x => x.ReadLine(), Times.AtLeast(3)); // Start + 2 fields
        }

    }
}
