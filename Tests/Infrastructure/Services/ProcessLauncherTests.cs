using SharpBridge.Utilities;
using SharpBridge.Interfaces;
using Xunit;
using System;
using System.Runtime.InteropServices;

namespace SharpBridge.Tests.Infrastructure.Services
{
    public class ProcessLauncherTests : IDisposable
    {
        private readonly ProcessLauncher _processLauncher;

        public ProcessLauncherTests()
        {
            // Use invisible processes for tests (no windows, no shell execute)
            _processLauncher = new ProcessLauncher(useShellExecute: false, createNoWindow: true);
        }

        public void Dispose()
        {
            _processLauncher?.Dispose();
        }

        #region Successful Process Launch Tests

        [Fact]
        public void TryStartProcess_WithValidExecutable_ReturnsTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = GetSafeArguments();

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
            Assert.NotNull(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithEmptyArguments_ReturnsTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = "";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
            Assert.NotNull(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithNullArguments_ReturnsTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = null!;

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
            Assert.NotNull(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithComplexArguments_ReturnsTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = GetComplexSafeArguments();

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
            Assert.NotNull(_processLauncher.LastStartedProcess);
        }

        #endregion

        #region Failed Process Launch Tests

        [Fact]
        public void TryStartProcess_WithNonExistentExecutable_ReturnsFalse()
        {
            // Arrange
            string executable = "nonexistent_executable_that_does_not_exist.exe";
            string arguments = "";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.False(result);
            Assert.Null(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithEmptyExecutable_ReturnsFalse()
        {
            // Arrange
            string executable = "";
            string arguments = "";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.False(result);
            Assert.Null(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithNullExecutable_ReturnsFalse()
        {
            // Arrange
            string executable = null!;
            string arguments = "";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.False(result);
            Assert.Null(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithInvalidExecutablePath_ReturnsFalse()
        {
            // Arrange
            string executable = "/this/path/does/not/exist/executable";
            string arguments = "";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.False(result);
            Assert.Null(_processLauncher.LastStartedProcess);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void ProcessLauncher_ImplementsIProcessLauncher()
        {
            // Arrange
            var launcher = new ProcessLauncher();

            // Act & Assert
            Assert.IsAssignableFrom<IProcessLauncher>(launcher);
        }

        [Fact]
        public void TryStartProcess_ImplementsInterfaceCorrectly()
        {
            // Arrange
            IProcessLauncher launcher = new ProcessLauncher(useShellExecute: false, createNoWindow: true);
            string executable = GetSafeExecutable();
            string arguments = GetSafeArguments();

            // Act
            bool result = launcher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void TryStartProcess_WithVeryLongArguments_ReturnsTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = new string('a', 1000); // Very long argument

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
            Assert.NotNull(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithSpecialCharactersInArguments_ReturnsTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = "test \"quoted string\" & | < > ^ %";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.True(result);
            Assert.NotNull(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_WithWhitespaceInExecutable_ReturnsFalse()
        {
            // Arrange
            string executable = "   nonexistent_executable_with_whitespace   ";
            string arguments = "";

            // Act
            bool result = _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.False(result);
            Assert.Null(_processLauncher.LastStartedProcess);
        }

        #endregion

        #region Multiple Calls Tests

        [Fact]
        public void TryStartProcess_MultipleSuccessfulCalls_AllReturnTrue()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = GetSafeArguments();

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                bool result = _processLauncher.TryStartProcess(executable, arguments);
                Assert.True(result);
                Assert.NotNull(_processLauncher.LastStartedProcess);
            }
        }

        [Fact]
        public void TryStartProcess_MixedSuccessAndFailureCalls_ReturnExpectedResults()
        {
            // Arrange
            string validExecutable = GetSafeExecutable();
            string invalidExecutable = "nonexistent.exe";

            // Act & Assert
            bool successResult = _processLauncher.TryStartProcess(validExecutable, "");
            bool failureResult = _processLauncher.TryStartProcess(invalidExecutable, "");

            Assert.True(successResult);
            Assert.False(failureResult);
        }

        #endregion

        #region Process Tracking Tests

        [Fact]
        public void SpawnedProcesses_TracksAllStartedProcesses()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = GetSafeArguments();

            // Act
            _processLauncher.TryStartProcess(executable, arguments);
            _processLauncher.TryStartProcess(executable, arguments);

            // Assert
            Assert.Equal(2, _processLauncher.SpawnedProcesses.Count);
        }

        [Fact]
        public void LastStartedProcess_ReturnsMostRecentProcess()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = GetSafeArguments();

            // Act
            _processLauncher.TryStartProcess(executable, arguments);
            var firstProcess = _processLauncher.LastStartedProcess;
            _processLauncher.TryStartProcess(executable, arguments);
            var secondProcess = _processLauncher.LastStartedProcess;

            // Assert
            Assert.NotNull(firstProcess);
            Assert.NotNull(secondProcess);
            Assert.NotEqual(firstProcess, secondProcess);
        }

        #endregion

        #region Cleanup Tests

        [Fact]
        public void Dispose_KillsAllSpawnedProcesses()
        {
            // Arrange
            string executable = GetSafeExecutable();
            string arguments = GetSafeArguments();
            _processLauncher.TryStartProcess(executable, arguments);
            _processLauncher.TryStartProcess(executable, arguments);

            // Act
            _processLauncher.Dispose();

            // Assert
            Assert.Equal(0, _processLauncher.SpawnedProcesses.Count);
            Assert.Null(_processLauncher.LastStartedProcess);
        }

        [Fact]
        public void TryStartProcess_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _processLauncher.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
                _processLauncher.TryStartProcess("echo", "test"));
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public void ProcessLauncher_WithDefaultConstructor_UsesDefaultSettings()
        {
            // Arrange & Act
            var launcher = new ProcessLauncher();

            // Assert
            Assert.True(launcher.TryStartProcess(GetSafeExecutable(), GetSafeArguments()));
        }

        [Fact]
        public void ProcessLauncher_WithCustomSettings_UsesProvidedSettings()
        {
            // Arrange
            var launcher = new ProcessLauncher(useShellExecute: false, createNoWindow: true);

            // Act
            bool result = launcher.TryStartProcess(GetSafeExecutable(), GetSafeArguments());

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Helper Methods

        private static string GetSafeExecutable()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "cmd.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "echo";
            }
            else
            {
                // Fallback for other platforms
                return "echo";
            }
        }

        private static string GetSafeArguments()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "/c echo test";
            }
            else
            {
                return "test";
            }
        }

        private static string GetComplexSafeArguments()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "/c echo \"Hello World\" && echo \"Second Line\"";
            }
            else
            {
                return "\"Hello World\"";
            }
        }

        #endregion
    }
}