using System;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class KeyboardInputHandlerTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly KeyboardInputHandler _handler;
        
        public KeyboardInputHandlerTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _handler = new KeyboardInputHandler(_mockLogger.Object);
        }
        
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new KeyboardInputHandler(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }
        
        [Fact]
        public void RegisterShortcut_WithValidParameters_RegistersShortcut()
        {
            // Arrange
            bool actionExecuted = false;
            void TestAction() => actionExecuted = true;

            // Act
            _handler.RegisterShortcut(ConsoleKey.A, ConsoleModifiers.Alt, TestAction, "Test shortcut");
            var shortcuts = _handler.GetRegisteredShortcuts();
            
            // Assert
            shortcuts.Should().HaveCount(1);
            shortcuts[0].Key.Should().Be(ConsoleKey.A);
            shortcuts[0].Modifiers.Should().Be(ConsoleModifiers.Alt);
            shortcuts[0].Description.Should().Be("Test shortcut");
            
            // Verify the logger was called
            _mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }
        
        [Fact]
        public void RegisterShortcut_WithNullAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _handler.RegisterShortcut(ConsoleKey.A, ConsoleModifiers.Alt, null!, "Test");
            act.Should().Throw<ArgumentNullException>().WithParameterName("action");
        }
        
        [Fact]
        public void RegisterShortcut_WithEmptyDescription_ThrowsArgumentException()
        {
            // Act & Assert
            Action act = () => _handler.RegisterShortcut(ConsoleKey.A, ConsoleModifiers.Alt, () => { }, "");
            act.Should().Throw<ArgumentException>().WithParameterName("description");
        }
        
        [Fact]
        public void GetRegisteredShortcuts_WithMultipleShortcuts_ReturnsAllShortcuts()
        {
            // Arrange
            Action action1 = () => { };
            Action action2 = () => { };
            
            // Act
            _handler.RegisterShortcut(ConsoleKey.A, ConsoleModifiers.Alt, action1, "Test 1");
            _handler.RegisterShortcut(ConsoleKey.B, ConsoleModifiers.Control, action2, "Test 2");
            var shortcuts = _handler.GetRegisteredShortcuts();
            
            // Assert
            shortcuts.Should().HaveCount(2);
            shortcuts.Should().Contain(s => s.Key == ConsoleKey.A && s.Modifiers == ConsoleModifiers.Alt && s.Description == "Test 1");
            shortcuts.Should().Contain(s => s.Key == ConsoleKey.B && s.Modifiers == ConsoleModifiers.Control && s.Description == "Test 2");
        }
        
        [Fact]
        public void RegisterShortcut_WithExistingShortcut_UpdatesShortcut()
        {
            // Arrange
            Action action1 = () => { };
            Action action2 = () => { };
            
            // Act
            _handler.RegisterShortcut(ConsoleKey.A, ConsoleModifiers.Alt, action1, "Test 1");
            _handler.RegisterShortcut(ConsoleKey.A, ConsoleModifiers.Alt, action2, "Test 2");
            var shortcuts = _handler.GetRegisteredShortcuts();
            
            // Assert
            shortcuts.Should().HaveCount(1);
            shortcuts[0].Description.Should().Be("Test 2");
        }
        
        // Note: Testing CheckForKeyboardInput is more complex because it involves Console.KeyAvailable
        // and Console.ReadKey which are hard to mock without more advanced techniques.
        // This would be better tested in integration tests or with a wrapper interface for Console.
    }
} 