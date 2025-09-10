using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class InitializationContentProviderTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IExternalEditorService> _mockExternalEditorService;
        private readonly Mock<IConsole> _mockConsole;
        private readonly Mock<ConsoleRenderContext> _mockContext;

        public InitializationContentProviderTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockExternalEditorService = new Mock<IExternalEditorService>();
            _mockConsole = new Mock<IConsole>();
            _mockContext = new Mock<ConsoleRenderContext>();
        }

        private InitializationContentProvider CreateProvider()
        {
            return new InitializationContentProvider(_mockLogger.Object, _mockExternalEditorService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var provider = CreateProvider();

            // Assert
            provider.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new InitializationContentProvider(null!, _mockExternalEditorService.Object));
        }

        [Fact]
        public void Constructor_WithNullExternalEditorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new InitializationContentProvider(_mockLogger.Object, null!));
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Mode_ReturnsInitialization()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var mode = provider.Mode;

            // Assert
            mode.Should().Be(ConsoleMode.Initialization);
        }

        [Fact]
        public void DisplayName_ReturnsInitialization()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var displayName = provider.DisplayName;

            // Assert
            displayName.Should().Be("Initialization");
        }

        [Fact]
        public void ToggleAction_ReturnsShowSystemHelp()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var toggleAction = provider.ToggleAction;

            // Assert
            toggleAction.Should().Be(ShortcutAction.ShowSystemHelp);
        }

        [Fact]
        public void PreferredUpdateInterval_Returns100Milliseconds()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var interval = provider.PreferredUpdateInterval;

            // Assert
            interval.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        #endregion

        #region SetProgress Tests

        [Fact]
        public void SetProgress_WithValidProgress_SetsProgress()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();

            // Act
            provider.SetProgress(progress);

            // Assert
            // We can't directly verify the private field, but we can test it indirectly through GetContent
            var content = provider.GetContent(_mockContext.Object);
            content.Should().NotBeEmpty();
        }

        [Fact]
        public void SetProgress_WithNullProgress_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => provider.SetProgress(null!));
        }

        #endregion

        #region Enter/Exit Tests

        [Fact]
        public void Enter_DoesNothing()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            // These are no-op methods, so we just verify they don't throw
            var exception = Record.Exception(() => provider.Enter(_mockConsole.Object));
            exception.Should().BeNull();
        }

        [Fact]
        public void Exit_DoesNothing()
        {
            // Arrange
            var provider = CreateProvider();

            // Act & Assert
            // These are no-op methods, so we just verify they don't throw
            var exception = Record.Exception(() => provider.Exit(_mockConsole.Object));
            exception.Should().BeNull();
        }

        #endregion

        #region GetContent Tests

        [Fact]
        public void GetContent_WithValidContext_ReturnsRenderedDisplay()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            content.Should().NotBeEmpty();
            content[0].Should().Be("Initializing Sharp Bridge...");
            content[1].Should().StartWith("Elapsed:");
        }

        [Fact]
        public void GetContent_WhenRenderingThrows_LogsErrorAndReturnsErrorMessage()
        {
            // Arrange
            var provider = new TestableInitializationContentProvider(_mockLogger.Object, _mockExternalEditorService.Object);
            provider.SetProgress(new InitializationProgress());

            // Act & Assert
            // The testable class throws an exception, but the base class should catch it
            var content = provider.GetContent(_mockContext.Object);

            content.Should().HaveCount(1);
            content[0].Should().Be("Error displaying initialization progress");
            _mockLogger.Verify(x => x.ErrorWithException("Error rendering initialization display", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region TryOpenInExternalEditorAsync Tests

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WhenSuccessful_ReturnsTrue()
        {
            // Arrange
            var provider = CreateProvider();
            _mockExternalEditorService.Setup(x => x.TryOpenApplicationConfigAsync()).ReturnsAsync(true);

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeTrue();
            _mockExternalEditorService.Verify(x => x.TryOpenApplicationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WhenServiceThrows_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var provider = CreateProvider();
            var exception = new Exception("Test exception");
            _mockExternalEditorService.Setup(x => x.TryOpenApplicationConfigAsync()).ThrowsAsync(exception);

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.ErrorWithException("Error opening application config in external editor", exception), Times.Once);
        }

        #endregion

        #region RenderInitializationDisplay Tests

        [Fact]
        public void RenderInitializationDisplay_WithEmptyProgress_ReturnsBasicDisplay()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            // Clear all steps to test empty progress
            progress.Steps.Clear();
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            content.Should().HaveCount(3); // Header, elapsed time, empty line
            content[0].Should().Be("Initializing Sharp Bridge...");
            content[1].Should().StartWith("Elapsed:");
            content[2].Should().BeEmpty();
        }

        [Fact]
        public void RenderInitializationDisplay_WithAllSteps_ReturnsCompleteDisplay()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();

            // Add all steps with different statuses
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo
            {
                Status = StepStatus.Completed,
                StartTime = DateTime.UtcNow.AddSeconds(-1.5),
                EndTime = DateTime.UtcNow
            };
            progress.Steps[InitializationStep.TransformationEngine] = new StepInfo { Status = StepStatus.InProgress };
            progress.Steps[InitializationStep.FileWatchers] = new StepInfo { Status = StepStatus.Pending };
            progress.Steps[InitializationStep.PCClient] = new StepInfo
            {
                Status = StepStatus.Failed,
                StartTime = DateTime.UtcNow.AddSeconds(-2.3),
                EndTime = DateTime.UtcNow
            };
            progress.Steps[InitializationStep.PhoneClient] = new StepInfo { Status = StepStatus.Pending };
            progress.Steps[InitializationStep.ParameterSync] = new StepInfo { Status = StepStatus.Pending };
            progress.Steps[InitializationStep.FinalSetup] = new StepInfo { Status = StepStatus.Pending };

            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            content.Should().HaveCount(10); // Header + elapsed + empty + 7 steps
            content[0].Should().Be("Initializing Sharp Bridge...");
            content[1].Should().StartWith("Elapsed:");
            content[2].Should().BeEmpty();

            // Check that all steps are present with correct descriptions
            var contentText = string.Join("\n", content);
            contentText.Should().Contain("Console Setup");
            contentText.Should().Contain("Loading Transformation Rules");
            contentText.Should().Contain("Setting up File Watchers");
            contentText.Should().Contain("PC Client");
            contentText.Should().Contain("Phone Client");
            contentText.Should().Contain("Parameter Sync");
            contentText.Should().Contain("Final Setup");
        }

        #endregion

        #region RenderStepLine Tests (Tested indirectly through GetContent)

        [Fact]
        public void RenderStepLine_WithCompletedStep_ReturnsFormattedLine()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            var stepInfo = new StepInfo
            {
                Status = StepStatus.Completed,
                StartTime = DateTime.UtcNow.AddSeconds(-1.5),
                EndTime = DateTime.UtcNow
            };
            progress.Steps[InitializationStep.ConsoleSetup] = stepInfo;
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("Console Setup");
            stepLine.Should().Contain("(1.5s)");
            stepLine.Should().Contain("OK");
        }

        [Fact]
        public void RenderStepLine_WithInProgressStep_ReturnsFormattedLine()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.TransformationEngine] = new StepInfo { Status = StepStatus.InProgress };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("Loading Transformation Rules");
            stepLine.Should().Contain("(In Progress)");
            stepLine.Should().Contain("RUN");
        }

        [Fact]
        public void RenderStepLine_WithPendingStep_ReturnsFormattedLine()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.FileWatchers] = new StepInfo { Status = StepStatus.Pending };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("Setting up File Watchers");
            stepLine.Should().Contain("(Pending)");
            stepLine.Should().Contain("PEND");
        }

        [Fact]
        public void RenderStepLine_WithFailedStep_ReturnsFormattedLine()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            var stepInfo = new StepInfo
            {
                Status = StepStatus.Failed,
                StartTime = DateTime.UtcNow.AddSeconds(-2.3),
                EndTime = DateTime.UtcNow
            };
            progress.Steps[InitializationStep.PCClient] = stepInfo;
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("PC Client");
            stepLine.Should().Contain("(2.3s)");
            stepLine.Should().Contain("FAIL");
        }

        [Fact]
        public void RenderStepLine_WithUnknownStep_ReturnsFormattedLine()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = (StepStatus)999 }; // Unknown status
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("Console Setup");
            stepLine.Should().Contain("UNK");
        }

        [Fact]
        public void RenderStepLine_WithDuration_ShowsDuration()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            var stepInfo = new StepInfo
            {
                Status = StepStatus.Completed,
                StartTime = DateTime.UtcNow.AddSeconds(-3.7),
                EndTime = DateTime.UtcNow
            };
            progress.Steps[InitializationStep.ConsoleSetup] = stepInfo;
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("(3.7s)");
        }

        [Fact]
        public void RenderStepLine_WithoutDuration_ShowsStatusDescription()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = StepStatus.Pending };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("(Pending)");
        }

        #endregion

        #region GetStatusIndicator Tests (Tested indirectly through GetContent)

        [Fact]
        public void GetStatusIndicator_Completed_ReturnsColoredOK()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = StepStatus.Completed };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("[");
            stepLine.Should().Contain("]");
            stepLine.Should().Contain("OK");
        }

        [Fact]
        public void GetStatusIndicator_InProgress_ReturnsColoredRUN()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = StepStatus.InProgress };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("[");
            stepLine.Should().Contain("]");
            stepLine.Should().Contain("RUN");
        }

        [Fact]
        public void GetStatusIndicator_Pending_ReturnsColoredPEND()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = StepStatus.Pending };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("[");
            stepLine.Should().Contain("]");
            stepLine.Should().Contain("PEND");
        }

        [Fact]
        public void GetStatusIndicator_Failed_ReturnsColoredFAIL()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = StepStatus.Failed };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("[");
            stepLine.Should().Contain("]");
            stepLine.Should().Contain("FAIL");
        }

        [Fact]
        public void GetStatusIndicator_Unknown_ReturnsColoredUNK()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            progress.Steps.Clear(); // Clear default steps
            progress.Steps[InitializationStep.ConsoleSetup] = new StepInfo { Status = (StepStatus)999 };
            provider.SetProgress(progress);

            // Act
            var content = provider.GetContent(_mockContext.Object);

            // Assert
            var stepLine = content[3]; // After header, elapsed, empty line
            stepLine.Should().Contain("[");
            stepLine.Should().Contain("]");
            stepLine.Should().Contain("UNK");
        }

        #endregion

        #region FormatElapsedTime Tests (Tested indirectly through GetContent)

        [Fact]
        public void FormatElapsedTime_WithVariousTimes_FormatsCorrectly()
        {
            // Arrange
            var provider = CreateProvider();
            var progress = new InitializationProgress();
            provider.SetProgress(progress);

            // Act & Assert - Test different elapsed times
            var content1 = provider.GetContent(_mockContext.Object);
            content1[1].Should().StartWith("Elapsed: " + ConsoleColors.ColorizeBasicType("00:00.0"));

            // Note: We can't easily test different elapsed times without modifying the progress object
            // The elapsed time is set when the progress object is created, not when GetContent is called
        }

        #endregion

    }

    /// <summary>
    /// Testable version of InitializationContentProvider that allows testing error scenarios
    /// </summary>
    public class TestableInitializationContentProvider : InitializationContentProvider
    {
        public TestableInitializationContentProvider(IAppLogger logger, IExternalEditorService externalEditorService)
            : base(logger, externalEditorService)
        {
        }

        protected override string[] RenderInitializationDisplay()
        {
            // Override to throw an exception for testing error handling
            throw new Exception("Test exception for error handling");
        }
    }

}
