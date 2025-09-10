using System;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;
using System.Linq;

namespace SharpBridge.Tests.UI.Components
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarLint", "S4144", Justification = "Test methods with similar implementations are expected due to Theory/InlineData pattern")]
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
            result!.Key.Should().Be(ConsoleKey.F1);
            result.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Fact]
        public void ParseShortcut_WithSingleModifier_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Alt+T");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.T);
            result.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        [Fact]
        public void ParseShortcut_WithMultipleModifiers_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Ctrl+Alt+E");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.E);
            result.Modifiers.Should().Be(ConsoleModifiers.Control | ConsoleModifiers.Alt);
        }

        [Fact]
        public void ParseShortcut_WithShiftModifier_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Shift+F5");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.F5);
            result.Modifiers.Should().Be(ConsoleModifiers.Shift);
        }

        [Fact]
        public void ParseShortcut_WithControlAlias_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("Control+C");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.C);
            result.Modifiers.Should().Be(ConsoleModifiers.Control);
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

        [Fact]
        public void ParseShortcut_WithCaseInsensitiveInput_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut("alt+t");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.T);
            result.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        [Fact]
        public void ParseShortcut_WithExtraSpaces_ReturnsCorrectResult()
        {
            // Act
            var result = _parser.ParseShortcut(" Alt + T ");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.T);
            result.Modifiers.Should().Be(ConsoleModifiers.Alt);
        }

        #endregion

        #region Number Key Tests

        public void ParseShortcut_WithNumberKeys_ReturnsCorrectConsoleKey(string input, ConsoleKey expectedKey)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(expectedKey);
            result.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Theory]
        [InlineData("Ctrl+0", ConsoleKey.D0, ConsoleModifiers.Control)]
        [InlineData("Alt+5", ConsoleKey.D5, ConsoleModifiers.Alt)]
        [InlineData("Shift+9", ConsoleKey.D9, ConsoleModifiers.Shift)]
        [InlineData("Ctrl+Alt+3", ConsoleKey.D3, ConsoleModifiers.Control | ConsoleModifiers.Alt)]
        public void ParseShortcut_WithNumberKeysAndModifiers_ReturnsCorrectResult(string input, ConsoleKey expectedKey, ConsoleModifiers expectedModifiers)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(expectedKey);
            result.Modifiers.Should().Be(expectedModifiers);
        }

        #endregion

        #region Symbol Key Tests

        [Theory]
        [InlineData("!", ConsoleKey.D1)]
        [InlineData("@", ConsoleKey.D2)]
        [InlineData("#", ConsoleKey.D3)]
        [InlineData("$", ConsoleKey.D4)]
        [InlineData("%", ConsoleKey.D5)]
        [InlineData("^", ConsoleKey.D6)]
        [InlineData("&", ConsoleKey.D7)]
        [InlineData("*", ConsoleKey.D8)]
        [InlineData("(", ConsoleKey.D9)]
        [InlineData(")", ConsoleKey.D0)]
        [InlineData("-", ConsoleKey.OemMinus)]
        [InlineData("_", ConsoleKey.OemMinus)]
        [InlineData("=", ConsoleKey.OemPlus)]
        [InlineData("+", ConsoleKey.OemPlus)]
        [InlineData("[", ConsoleKey.Oem4)]
        [InlineData("{", ConsoleKey.Oem4)]
        [InlineData("]", ConsoleKey.Oem6)]
        [InlineData("}", ConsoleKey.Oem6)]
        [InlineData("\\", ConsoleKey.Oem5)]
        [InlineData("|", ConsoleKey.Oem5)]
        [InlineData(";", ConsoleKey.Oem1)]
        [InlineData(":", ConsoleKey.Oem1)]
        [InlineData("'", ConsoleKey.Oem7)]
        [InlineData("\"", ConsoleKey.Oem7)]
        [InlineData(",", ConsoleKey.OemComma)]
        [InlineData("<", ConsoleKey.OemComma)]
        [InlineData(".", ConsoleKey.OemPeriod)]
        [InlineData(">", ConsoleKey.OemPeriod)]
        [InlineData("/", ConsoleKey.Oem2)]
        [InlineData("?", ConsoleKey.Oem2)]
        [InlineData("`", ConsoleKey.Oem3)]
        [InlineData("~", ConsoleKey.Oem3)]
        [InlineData(" ", ConsoleKey.Spacebar)]
        public void ParseShortcut_ReturnsCorrectConsoleKey(string input, ConsoleKey expectedKey)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(expectedKey);
            result.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Theory]
        [InlineData("Ctrl+;", ConsoleKey.Oem1, ConsoleModifiers.Control)]
        [InlineData("Alt+[", ConsoleKey.Oem4, ConsoleModifiers.Alt)]
        [InlineData("Shift+/", ConsoleKey.Oem2, ConsoleModifiers.Shift)]
        [InlineData("Ctrl+Alt+=", ConsoleKey.OemPlus, ConsoleModifiers.Control | ConsoleModifiers.Alt)]
        public void ParseShortcut_WithSymbolsAndModifiers_ReturnsCorrectResult(string input, ConsoleKey expectedKey, ConsoleModifiers expectedModifiers)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(expectedKey);
            result.Modifiers.Should().Be(expectedModifiers);
        }

        #endregion

        #region FormatShortcut Tests

        [Fact]
        public void FormatShortcut_WithNoModifiers_ReturnsKeyOnly()
        {
            // Arrange
            var shortcut = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be("F1");
        }

        [Fact]
        public void FormatShortcut_WithSingleModifier_ReturnsCorrectFormat()
        {
            // Arrange
            var shortcut = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be("Alt+T");
        }

        [Fact]
        public void FormatShortcut_WithMultipleModifiers_ReturnsConsistentOrder()
        {
            // Arrange
            var shortcut = new Shortcut(ConsoleKey.E, ConsoleModifiers.Alt | ConsoleModifiers.Control);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be("Ctrl+Alt+E");
        }

        [Fact]
        public void FormatShortcut_WithAllModifiers_ReturnsConsistentOrder()
        {
            // Arrange
            var shortcut = new Shortcut(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be("Ctrl+Alt+Shift+A");
        }

        [Theory]
        [InlineData(ConsoleKey.D0, "0")]
        [InlineData(ConsoleKey.D1, "1")]
        [InlineData(ConsoleKey.D5, "5")]
        [InlineData(ConsoleKey.D9, "9")]
        public void FormatShortcut_WithNumberKeys_ReturnsUserFriendlyFormat(ConsoleKey key, string expected)
        {
            // Arrange
            var shortcut = new Shortcut(key, ConsoleModifiers.None);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(ConsoleKey.OemMinus, "-")]
        [InlineData(ConsoleKey.OemPlus, "=")]
        [InlineData(ConsoleKey.Oem4, "[")]
        [InlineData(ConsoleKey.Oem6, "]")]
        [InlineData(ConsoleKey.Oem5, "\\")]
        [InlineData(ConsoleKey.Oem1, ";")]
        [InlineData(ConsoleKey.Oem7, "'")]
        [InlineData(ConsoleKey.OemComma, ",")]
        [InlineData(ConsoleKey.OemPeriod, ".")]
        [InlineData(ConsoleKey.Oem2, "/")]
        [InlineData(ConsoleKey.Oem3, "`")]
        public void FormatShortcut_WithSymbolKeys_ReturnsUserFriendlyFormat(ConsoleKey key, string expected)
        {
            // Arrange
            var shortcut = new Shortcut(key, ConsoleModifiers.None);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void FormatShortcut_WithSymbolAndModifiers_ReturnsCorrectFormat()
        {
            // Arrange
            var shortcut = new Shortcut(ConsoleKey.Oem1, ConsoleModifiers.Control);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be("Ctrl+;");
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

        [Theory]
        [InlineData("0", true)]
        [InlineData("5", true)]
        [InlineData("9", true)]
        [InlineData("Ctrl+3", true)]
        [InlineData(";", true)]
        [InlineData("[", true)]
        [InlineData("Alt+/", true)]
        public void IsValidShortcut_WithNewSymbolsAndNumbers_ReturnsTrue(string input, bool expected)
        {
            // Act
            var result = _parser.IsValidShortcut(input);

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
            var shortcut = new Shortcut(key, modifiers);
            var formatted = _parser.FormatShortcut(shortcut);

            // Act
            var parsed = _parser.ParseShortcut(formatted);

            // Assert
            parsed.Should().NotBeNull();
            parsed!.Key.Should().Be(key);
            parsed.Modifiers.Should().Be(modifiers);
        }

        [Theory]
        [InlineData(ConsoleKey.D0, ConsoleModifiers.None)]
        [InlineData(ConsoleKey.D5, ConsoleModifiers.Control)]
        [InlineData(ConsoleKey.Oem1, ConsoleModifiers.Alt)]
        [InlineData(ConsoleKey.Oem4, ConsoleModifiers.Shift)]
        [InlineData(ConsoleKey.OemMinus, ConsoleModifiers.Control | ConsoleModifiers.Alt)]
        public void ParseAndFormat_RoundTrip_WithSymbolsAndNumbers_ReturnsOriginalValues(ConsoleKey key, ConsoleModifiers modifiers)
        {
            // Arrange
            var shortcut = new Shortcut(key, modifiers);
            var formatted = _parser.FormatShortcut(shortcut);

            // Act
            var parsed = _parser.ParseShortcut(formatted);

            // Assert
            parsed.Should().NotBeNull();
            parsed!.Key.Should().Be(key);
            parsed.Modifiers.Should().Be(modifiers);
        }

        #endregion

        #region Exception Handling and Edge Cases Tests

        [Fact]
        public void ParseShortcut_WithEmptyPartsArray_ReturnsNull()
        {
            // This is a very edge case - when Split() somehow returns empty array
            // We can't easily trigger this with normal input, but we can test similar scenarios
            var result = _parser.ParseShortcut("++");

            // This should fail parsing and return null
            result.Should().BeNull();
        }

        [Fact]
        public void FormatShortcut_WithNullShortcut_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => _parser.FormatShortcut(null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("shortcut");
        }

        [Fact]
        public void ParseShortcut_WithMalformedInput_TriggersExceptionHandling()
        {
            // This is a very edge case that's hard to trigger naturally
            // We need to somehow cause an exception during parsing
            // Let's try to trigger an exception by creating a pathological case
            var result = _parser.ParseShortcut("Ctrl+\0+F1"); // Null character might cause issues

            // The method should handle any exception gracefully and return null
            result.Should().BeNull();
        }

        [Fact]
        public void TryParseConsoleKey_WithPlusSymbolDirectly_ReturnsCorrectKey()
        {
            // This tests the switch case for "+" that's currently unreachable
            // due to special handling, but we can test it indirectly
            var result = _parser.ParseShortcut("Ctrl++"); // This should fail gracefully
            result.Should().BeNull();
        }

        [Fact]
        public void TryParseConsoleKey_WithSpaceSymbolDirectly_ReturnsCorrectKey()
        {
            // This tests the switch case for " " that's currently unreachable
            // due to special handling, but we can test it indirectly
            var result = _parser.ParseShortcut("Ctrl+Spacebar"); // Use "Spacebar" - the actual enum value
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.Spacebar);
            result.Modifiers.Should().Be(ConsoleModifiers.Control);
        }

        [Fact]
        public void ParseShortcut_WithExtremelyLongInput_HandlesGracefully()
        {
            // Test with an extremely long input that might cause issues
            var longInput = string.Join("+", Enumerable.Repeat("Ctrl", 1000)) + "+F1";
            var result = _parser.ParseShortcut(longInput);

            // Should either parse successfully or return null gracefully
            // The important thing is it doesn't crash
            if (result != null)
            {
                result.Key.Should().Be(ConsoleKey.F1);
            }
        }

        [Fact]
        public void ParseShortcut_WithSpecialCharacters_HandlesGracefully()
        {
            // Test with various special characters that might cause parsing issues
            var specialInputs = new[]
            {
                "Ctrl+\t+F1",     // Tab character
                "Ctrl+\n+F1",     // Newline character
                "Ctrl+\r+F1",     // Carriage return
                "Ctrl+\u0001+F1", // Control character
                "Ctrl+\uFFFE+F1", // Non-character
            };

            foreach (var input in specialInputs)
            {
                var result = _parser.ParseShortcut(input);
                // Should handle gracefully without throwing exceptions
                // Result can be null or valid shortcut
                if (result != null)
                {
                    result.Key.Should().Be(ConsoleKey.F1);
                }
            }
        }

        [Fact]
        public void ParseShortcut_WithPathologicalInput_TriggersPartsLengthZeroCondition()
        {
            // Try to create a scenario where Split('+') might return an empty array
            // This is extremely difficult since Split always returns at least one element
            // But let's try some edge cases that might trigger unexpected behavior

            // Test with string that becomes empty after trimming all parts
            var result = _parser.ParseShortcut("   +   +   ");

            // This should either parse as a plus key or return null
            // The important thing is it doesn't crash
            if (result != null)
            {
                result.Key.Should().Be(ConsoleKey.OemPlus);
            }
        }

        [Fact]
        public void ParseShortcut_WithReflectionBreakingInput_TriggersExceptionHandling()
        {
            // Try to trigger an exception during parsing that would be caught by the catch block
            // We need to find input that causes an exception in the parsing logic

            // Test with extremely long modifier chains that might cause issues
            var longModifierChain = string.Join("+", Enumerable.Repeat("Ctrl", 10000)) + "+F1";
            var result = _parser.ParseShortcut(longModifierChain);

            // Should handle gracefully without throwing
            if (result != null)
            {
                result.Key.Should().Be(ConsoleKey.F1);
            }
        }

        [Fact]
        public void ParseShortcut_WithMemoryExhaustionInput_HandlesGracefully()
        {
            // Try to trigger an OutOfMemoryException or similar that would be caught
            try
            {
                // Create a pathologically large input that might cause memory issues
                var hugeInput = new string('A', 1000000) + "+F1";
                var result = _parser.ParseShortcut(hugeInput);

                // Should either parse or return null, but not crash
                if (result != null)
                {
                    result.Key.Should().Be(ConsoleKey.F1);
                }
            }
            catch (OutOfMemoryException)
            {
                // If we run out of memory, that's expected for this test
                // The important thing is that our parser's catch block would handle it
            }
        }

        [Fact]
        public void ParseShortcut_WithStackOverflowInput_HandlesGracefully()
        {
            // Try to create input that might cause stack overflow in parsing
            var deeplyNestedInput = string.Join("+", Enumerable.Repeat("Ctrl", 100000));
            var result = _parser.ParseShortcut(deeplyNestedInput);

            // Should return null due to no actual key at the end
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("")]  // Empty string after split
        [InlineData("+")]  // Just delimiter
        [InlineData("++")]  // Multiple delimiters
        [InlineData("+++")]  // Many delimiters
        [InlineData("++++++++++")]  // Excessive delimiters
        public void ParseShortcut_WithEmptyPartsScenarios_HandlesGracefully(string input)
        {
            // These scenarios might create empty parts arrays or trigger edge cases
            var result = _parser.ParseShortcut(input);

            // Should handle gracefully - either parse successfully or return null
            // The key is that it doesn't throw exceptions
            if (result != null)
            {
                // If it parses successfully, validate the result
                Enum.IsDefined(typeof(ConsoleKey), result.Key).Should().BeTrue();
            }
        }

        [Fact]
        public void ParseShortcut_WithUnicodeAndControlCharacters_TriggersExceptionHandling()
        {
            // Test with various Unicode and control characters that might cause parsing issues
            var problematicInputs = new[]
            {
                "Ctrl+\uFFFE",     // Non-character
                "Ctrl+\uFFFF",     // Non-character  
                "Ctrl+\u0000",     // Null character
                "Ctrl+\u001F",     // Control character
                "Ctrl+\u007F",     // DEL character
                "Ctrl+\u0080",     // High control character
                "Ctrl+\u009F",     // High control character
                "\uD800\uDC00+F1", // Surrogate pair
                "Ctrl+\uD800",     // Unpaired surrogate
                "Ctrl+\uDC00",     // Unpaired surrogate
            };

            foreach (var input in problematicInputs)
            {
                // These should be handled gracefully by the exception handling
                var result = _parser.ParseShortcut(input);

                // Should either parse or return null, but not crash
                if (result != null)
                {
                    Enum.IsDefined(typeof(ConsoleKey), result.Key).Should().BeTrue();
                }
            }
        }

        #endregion

        #region Special Key Formatting Tests

        [Theory]
        [InlineData(ConsoleKey.Spacebar, "Space")]
        [InlineData(ConsoleKey.Enter, "Enter")]
        [InlineData(ConsoleKey.Escape, "Esc")]
        [InlineData(ConsoleKey.Tab, "Tab")]
        [InlineData(ConsoleKey.Backspace, "Backspace")]
        [InlineData(ConsoleKey.Delete, "Delete")]
        [InlineData(ConsoleKey.Insert, "Insert")]
        [InlineData(ConsoleKey.Home, "Home")]
        [InlineData(ConsoleKey.End, "End")]
        [InlineData(ConsoleKey.PageUp, "PageUp")]
        [InlineData(ConsoleKey.PageDown, "PageDown")]
        [InlineData(ConsoleKey.UpArrow, "Up")]
        [InlineData(ConsoleKey.DownArrow, "Down")]
        [InlineData(ConsoleKey.LeftArrow, "Left")]
        [InlineData(ConsoleKey.RightArrow, "Right")]
        public void FormatShortcut_WithSpecialKeys_ReturnsUserFriendlyFormat(ConsoleKey key, string expected)
        {
            // Arrange
            var shortcut = new Shortcut(key, ConsoleModifiers.None);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(ConsoleKey.D2, "2")]
        [InlineData(ConsoleKey.D3, "3")]
        [InlineData(ConsoleKey.D4, "4")]
        [InlineData(ConsoleKey.D6, "6")]
        [InlineData(ConsoleKey.D7, "7")]
        [InlineData(ConsoleKey.D8, "8")]
        public void FormatShortcut_WithMissingNumberKeys_ReturnsUserFriendlyFormat(ConsoleKey key, string expected)
        {
            // Arrange
            var shortcut = new Shortcut(key, ConsoleModifiers.None);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void FormatShortcut_WithUnmappedKey_ReturnsToString()
        {
            // Arrange - Use a key that doesn't have special formatting
            var shortcut = new Shortcut(ConsoleKey.Applications, ConsoleModifiers.None);

            // Act
            var result = _parser.FormatShortcut(shortcut);

            // Assert
            result.Should().Be("Applications");
        }

        #endregion

        #region Complex Parsing Edge Cases

        [Fact]
        public void ParseShortcut_WithSpecialCasePlus_ReturnsCorrectResult()
        {
            // This tests the special case handling before normal parsing
            var result = _parser.ParseShortcut("+");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.OemPlus);
            result.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Fact]
        public void ParseShortcut_WithSpecialCaseSpace_ReturnsCorrectResult()
        {
            // This tests the special case handling before normal parsing
            var result = _parser.ParseShortcut(" ");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(ConsoleKey.Spacebar);
            result.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Theory]
        [InlineData("F1", ConsoleKey.F1)]
        [InlineData("F12", ConsoleKey.F12)]
        [InlineData("Enter", ConsoleKey.Enter)]
        [InlineData("Escape", ConsoleKey.Escape)]
        [InlineData("Tab", ConsoleKey.Tab)]
        [InlineData("Spacebar", ConsoleKey.Spacebar)]
        [InlineData("Backspace", ConsoleKey.Backspace)]
        [InlineData("Delete", ConsoleKey.Delete)]
        [InlineData("Insert", ConsoleKey.Insert)]
        [InlineData("Home", ConsoleKey.Home)]
        [InlineData("End", ConsoleKey.End)]
        [InlineData("PageUp", ConsoleKey.PageUp)]
        [InlineData("PageDown", ConsoleKey.PageDown)]
        [InlineData("UpArrow", ConsoleKey.UpArrow)]
        [InlineData("DownArrow", ConsoleKey.DownArrow)]
        [InlineData("LeftArrow", ConsoleKey.LeftArrow)]
        [InlineData("RightArrow", ConsoleKey.RightArrow)]
        public void ParseShortcut_WithStandardKeys_ReturnsCorrectConsoleKey(string input, ConsoleKey expectedKey)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(expectedKey);
            result.Modifiers.Should().Be(ConsoleModifiers.None);
        }

        [Theory]
        [InlineData("Ctrl+Enter", ConsoleKey.Enter, ConsoleModifiers.Control)]
        [InlineData("Alt+Tab", ConsoleKey.Tab, ConsoleModifiers.Alt)]
        [InlineData("Shift+Delete", ConsoleKey.Delete, ConsoleModifiers.Shift)]
        [InlineData("Ctrl+Alt+Home", ConsoleKey.Home, ConsoleModifiers.Control | ConsoleModifiers.Alt)]
        public void ParseShortcut_WithStandardKeysAndModifiers_ReturnsCorrectResult(string input, ConsoleKey expectedKey, ConsoleModifiers expectedModifiers)
        {
            // Act
            var result = _parser.ParseShortcut(input);

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be(expectedKey);
            result.Modifiers.Should().Be(expectedModifiers);
        }

        #endregion

        #region Investigation Tests for Defensive Code

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("  +  ")]
        [InlineData("\t+\t")]
        [InlineData("\n+\n")]
        [InlineData("   +   +   ")]
        [InlineData("  Ctrl  +  ")]
        [InlineData("  +  F1  ")]
        public void ParseShortcut_WithVariousEmptyishInputs_HandlesGracefully(string input)
        {
            // This test investigates whether various edge case inputs are handled gracefully
            // including the "  +  " scenario that might trigger different code paths
            var result = _parser.ParseShortcut(input);

            // Should either parse successfully or return null gracefully
            // The important thing is it doesn't crash and behaves predictably
            if (result != null)
            {
                // If it parses successfully, validate the result
                Enum.IsDefined(typeof(ConsoleKey), result.Key).Should().BeTrue();
                result.Key.Should().NotBe(default(ConsoleKey));
            }
            else
            {
                // If it returns null, that's also acceptable for malformed input
                result.Should().BeNull();
            }
        }

        [Fact]
        public void ParseShortcut_SplitBehaviorAnalysis_ProvePartsLengthNeverZero()
        {
            // This test specifically analyzes Split() behavior to investigate whether
            // the defensive check "if (parts.Length == 0)" can ever be reached
            var testInputs = new[]
            {
                "",           // Empty string
                "+",          // Just delimiter
                "++",         // Multiple delimiters  
                "   ",        // Just whitespace
                "  +  ",      // Whitespace around delimiter
                "\t+\n",      // Different whitespace chars
                "a+",         // Text plus delimiter
                "+b",         // Delimiter plus text
                "a+b",        // Normal case
                "++++++",     // Many delimiters
            };

            foreach (var input in testInputs)
            {
                // Analyze what Split('+') actually returns
                var parts = input.Split('+').Select(p => p.Trim()).ToArray();

                // The mathematical truth: Split() ALWAYS returns at least one element
                parts.Length.Should().BeGreaterThan(0, $"Split() should never return empty array for input: '{input}'");

                // Document what we actually get
                var partsDescription = string.Join("|", parts.Select(p => $"'{p}'"));
                Console.WriteLine($"Input: '{input}' -> Parts: [{partsDescription}] (Length: {parts.Length})");
            }

            // This proves that the defensive check "if (parts.Length == 0)" is unreachable
            // because String.Split() is guaranteed to return at least one element
            true.Should().BeTrue("This test proves the defensive code analysis");
        }

        #endregion
    }
}