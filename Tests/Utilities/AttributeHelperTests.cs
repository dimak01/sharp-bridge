using System;
using System.ComponentModel;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class AttributeHelperTests
    {
        #region GetDescription Enum Tests

        [Fact]
        public void GetDescription_WithDescriptionAttribute_ReturnsDescription()
        {
            // Act
            var result = AttributeHelper.GetDescription(ShortcutAction.CycleTransformationEngineVerbosity);

            // Assert
            result.Should().Be("Cycle Transformation Engine Verbosity");
        }

        [Fact]
        public void GetDescription_WithAllShortcutActions_ReturnsExpectedDescriptions()
        {
            // Act & Assert
            AttributeHelper.GetDescription(ShortcutAction.CycleTransformationEngineVerbosity)
                .Should().Be("Cycle Transformation Engine Verbosity");

            AttributeHelper.GetDescription(ShortcutAction.CyclePCClientVerbosity)
                .Should().Be("Cycle PC Client Verbosity");

            AttributeHelper.GetDescription(ShortcutAction.CyclePhoneClientVerbosity)
                .Should().Be("Cycle Phone Client Verbosity");

            AttributeHelper.GetDescription(ShortcutAction.ReloadTransformationConfig)
                .Should().Be("Reload Transformation Config");

            AttributeHelper.GetDescription(ShortcutAction.OpenConfigInEditor)
                .Should().Be("Open Config in External Editor");

            AttributeHelper.GetDescription(ShortcutAction.ShowSystemHelp)
                .Should().Be("Show System Help");
        }

        [Fact]
        public void GetDescription_WithEnumWithoutDescription_ReturnsEnumName()
        {
            // Arrange
            var enumWithoutDescription = TestEnumWithoutDescription.ValueWithoutDescription;

            // Act
            var result = AttributeHelper.GetDescription(enumWithoutDescription);

            // Assert
            result.Should().Be("ValueWithoutDescription");
        }

        [Fact]
        public void GetDescription_WithNullEnum_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                AttributeHelper.GetDescription((Enum)null!));
            exception.ParamName.Should().Be("enumValue");
        }

        #endregion

        #region GetPropertyDescription Tests

        [Fact]
        public void GetPropertyDescription_WithDescriptionAttribute_ReturnsDescription()
        {
            // Act
            var result = AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), nameof(ApplicationConfig.EditorCommand));

            // Assert
            result.Should().Be("External Editor Command");
        }

        [Fact]
        public void GetPropertyDescription_WithAllApplicationConfigProperties_ReturnsExpectedDescriptions()
        {
            // Act & Assert
            AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), nameof(ApplicationConfig.EditorCommand))
                .Should().Be("External Editor Command");

            AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), nameof(ApplicationConfig.Shortcuts))
                .Should().Be("Keyboard Shortcuts");
        }

        [Fact]
        public void GetPropertyDescription_WithPropertyWithoutDescription_ReturnsPropertyName()
        {
            // Act
            var result = AttributeHelper.GetPropertyDescription(typeof(TestClassWithoutDescription), "PropertyWithoutDescription");

            // Assert
            result.Should().Be("PropertyWithoutDescription");
        }

        [Fact]
        public void GetPropertyDescription_WithNonExistentProperty_ReturnsPropertyName()
        {
            // Act
            var result = AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), "NonExistentProperty");

            // Assert
            result.Should().Be("NonExistentProperty");
        }

        [Fact]
        public void GetPropertyDescription_WithNullType_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                AttributeHelper.GetPropertyDescription(null!, "SomeProperty"));
            exception.ParamName.Should().Be("type");
        }

        [Fact]
        public void GetPropertyDescription_WithNullPropertyName_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), null!));
            exception.ParamName.Should().Be("propertyName");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetPropertyDescription_WithEmptyOrWhitespacePropertyName_ThrowsArgumentException(string propertyName)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), propertyName));
            exception.ParamName.Should().Be("propertyName");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GetDescription_WithGenericEnum_WorksCorrectly()
        {
            // Arrange
            var genericEnum = TestGenericEnum.ValueWithDescription;

            // Act
            var result = AttributeHelper.GetDescription(genericEnum);

            // Assert
            result.Should().Be("Generic Value with Description");
        }

        [Fact]
        public void GetPropertyDescription_WithInheritedProperty_WorksCorrectly()
        {
            // Act
            var result = AttributeHelper.GetPropertyDescription(typeof(DerivedTestClass), "BaseProperty");

            // Assert
            result.Should().Be("Base Property Description");
        }

        [Fact]
        public void GetPropertyDescription_WithStaticProperty_WorksCorrectly()
        {
            // Act
            var result = AttributeHelper.GetPropertyDescription(typeof(TestClassWithStaticProperty), "StaticProperty");

            // Assert
            result.Should().Be("Static Property Description");
        }

        #endregion

        #region Test Helper Classes and Enums

        private enum TestEnumWithoutDescription
        {
            ValueWithoutDescription
        }

        private enum TestGenericEnum
        {
            [Description("Generic Value with Description")]
            ValueWithDescription,
            ValueWithoutDescription
        }

        private class TestClassWithoutDescription
        {
            public string PropertyWithoutDescription { get; set; } = string.Empty;
        }

        private class BaseTestClass
        {
            [Description("Base Property Description")]
            public virtual string BaseProperty { get; set; } = string.Empty;
        }

        private class DerivedTestClass : BaseTestClass
        {
            public override string BaseProperty { get; set; } = string.Empty;
        }

        private class TestClassWithStaticProperty
        {
            [Description("Static Property Description")]
            public static string StaticProperty { get; set; } = string.Empty;
        }

        #endregion
    }
}