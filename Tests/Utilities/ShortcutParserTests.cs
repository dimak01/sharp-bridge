using System;
using FluentAssertions;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ShortcutParserTests
    {
        private readonly ShortcutParser _parser;

        public ShortcutParserTests()
        {
            _parser = new ShortcutParser();
        }

        #region ParseShortcut Tests

        [Fact]
        public void ParseShortcut_WithValidSingleKey_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("F1");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.F1);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Fact]
        public void ParseShortcut_WithSingleModifier_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Alt+T");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.T);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        [Fact]
        public void ParseShortcut_WithMultipleModifiers_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Ctrl+Alt+E");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.E);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.Control | ConsoleModifiers.Alt);
        }

        [Fact]
        public void ParseShortcut_WithShiftModifier_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Shift+F5");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.F5);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.Shift);
        }

        [Fact]
        public void ParseShortcut_WithControlAlias_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Control+C");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.C);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.Control);
        }

        [Theory]
        [InlineData("None")]
        [InlineData("Disabled")]
        [InlineData("none")]
        [InlineData("disabled")]
        [InlineData("NONE")]
        [InlineData("DISABLED")]
        public void ParseShortcut_WithDisableKeywords_ReturnsNull(string input)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseShortcut_WithNullOrWhitespace_ReturnsNull(string? input)
        {
            // Act
            var result = _parser.ParseShortcut(input!);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("InvalidKey")]
        [InlineData("Alt+InvalidKey")]
        [InlineData("Ctrl+")]
        [InlineData("+T")]
        [InlineData("Alt++T")]
        public void ParseShortcut_WithInvalidFormat_ReturnsNull(string input)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("Ctrl")]
        [InlineData("Alt")]
        [InlineData("Shift")]
        public void ParseShortcut_WithModifierAsKey_ReturnsNull(string input)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseShortcut_WithCaseInsensitiveInput_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("alt+t");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.T);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        [Fact]
        public void ParseShortcut_WithExtraSpaces_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut(" Alt + T ");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Key.Should().Be(ConsoleKey.T);
            result.Value.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        #endregion

        #region FormatShortcut Tests

        [Fact]
        public void FormatShortcut_WithNoModifiers_ReturnsKeyOnly()
        {
            // Act
            var result = _parser.FormatShortcut(ConsoleKey.F1, ConsoleModifiers.None);

            // Assert
            result.Should().Be("F1");
        }

        [Fact]
        public void FormatShortcut_WithSingleModifier_ReturnsCorrectFormat()
        {
            // Act
            var result = _parser.FormatShortcut(ConsoleKey.T, ConsoleModifiers.Alt);

            // Assert
            result.Should().Be("Alt+T");
        }

        [Fact]
        public void FormatShortcut_WithMultipleModifiers_ReturnsConsistentOrder()
        {
            // Act
            var result = _parser.FormatShortcut(ConsoleKey.E, ConsoleModifiers.Alt | ConsoleModifiers.Control);

            // Assert
            result.Should().Be("Ctrl+Alt+E");
        }

        [Fact]
        public void FormatShortcut_WithAllModifiers_ReturnsConsistentOrder()
        {
            // Act
            var result = _parser.FormatShortcut(ConsoleKey.A,
                ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);

            // Assert
            result.Should().Be("Ctrl+Alt+Shift+A");
        }

        #endregion

        #region IsValidShortcut Tests

        [Theory]
        [InlineData("F1", true)]
        [InlineData("Alt+T", true)]
        [InlineData("Ctrl+Alt+E", true)]
        [InlineData("None", false)]
        [InlineData("Disabled", false)]
        [InlineData("InvalidKey", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidShortcut_WithVariousInputs_ReturnsExpectedResult(string? input, bool expected)
        {
            // Act
            var result = _parser.IsValidShortcut(input!);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Round-Trip Tests

        [Theory]
        [InlineData(ConsoleKey.F1, ConsoleModifiers.None)]
        [InlineData(ConsoleKey.T, ConsoleModifiers.Alt)]
        [InlineData(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt)]
        [InlineData(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift)]
        public void ParseAndFormat_RoundTrip_ReturnsOriginalValues(ConsoleKey key, ConsoleModifiers modifiers)
        {
            // Arrange
            var formatted = _parser.FormatShortcut(key, modifiers);

            // Act
            var parsed = _parser.ParseShortcut(formatted);

            // Assert
            parsed.Should().NotBeNull();
            parsed!.Value.Key.Should().Be(key);
            parsed.Value.Modifiers.Should().Be(modifiers);
        }

        #endregion
    }
}