// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.ComponentModel;
using FluentAssertions;
using SharpBridge.Infrastructure.Utilities;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Domain;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Infrastructure.Utilities
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
            var enumWithoutDescription = TestValueWithoutDescription.ValueWithoutDescription;

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

        [Fact]
        public void GetDescription_WithEnumFieldNotFound_ReturnsEnumName()
        {
            // Arrange - Create an enum value that won't have a corresponding field
            // This is a bit tricky, but we can use a value that doesn't exist in the enum
            var enumValue = (ShortcutAction)999; // Assuming 999 is not a valid enum value

            // Act
            var result = AttributeHelper.GetDescription(enumValue);

            // Assert
            result.Should().Be("999"); // Should return the string representation of the enum value
        }

        #endregion

        #region GetPropertyDescription Tests

        [Fact]
        public void GetPropertyDescription_WithDescriptionAttribute_ReturnsDescription()
        {
            // Act
            var result = AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), nameof(GeneralSettingsConfig.EditorCommand));

            // Assert
            result.Should().Be("External Editor Command");
        }

        [Fact]
        public void GetPropertyDescription_WithAllGeneralSettingsConfigProperties_ReturnsExpectedDescriptions()
        {
            // Act & Assert
            AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), nameof(GeneralSettingsConfig.EditorCommand))
                .Should().Be("External Editor Command");

            AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), nameof(GeneralSettingsConfig.Shortcuts))
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
            var result = AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), "NonExistentProperty");

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
                AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), null!));
            exception.ParamName.Should().Be("propertyName");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetPropertyDescription_WithEmptyOrWhitespacePropertyName_ThrowsArgumentException(string propertyName)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), propertyName));
            exception.ParamName.Should().Be("propertyName");
        }

        #endregion

        #region GetPropertyDescription Object Instance Tests

        [Fact]
        public void GetPropertyDescription_WithObjectInstance_ReturnsDescription()
        {
            // Arrange
            var instance = new GeneralSettingsConfig();

            // Act
            var result = AttributeHelper.GetPropertyDescription(instance, nameof(GeneralSettingsConfig.EditorCommand));

            // Assert
            result.Should().Be("External Editor Command");
        }

        [Fact]
        public void GetPropertyDescription_WithObjectInstanceAndAllProperties_ReturnsExpectedDescriptions()
        {
            // Arrange
            var instance = new GeneralSettingsConfig();

            // Act & Assert
            AttributeHelper.GetPropertyDescription(instance, nameof(GeneralSettingsConfig.EditorCommand))
                .Should().Be("External Editor Command");

            AttributeHelper.GetPropertyDescription(instance, nameof(GeneralSettingsConfig.Shortcuts))
                .Should().Be("Keyboard Shortcuts");
        }

        [Fact]
        public void GetPropertyDescription_WithObjectInstanceAndPropertyWithoutDescription_ReturnsPropertyName()
        {
            // Arrange
            var instance = new TestClassWithoutDescription();

            // Act
            var result = AttributeHelper.GetPropertyDescription(instance, "PropertyWithoutDescription");

            // Assert
            result.Should().Be("PropertyWithoutDescription");
        }

        [Fact]
        public void GetPropertyDescription_WithObjectInstanceAndNonExistentProperty_ReturnsPropertyName()
        {
            // Arrange
            var instance = new GeneralSettingsConfig();

            // Act
            var result = AttributeHelper.GetPropertyDescription(instance, "NonExistentProperty");

            // Assert
            result.Should().Be("NonExistentProperty");
        }

        [Fact]
        public void GetPropertyDescription_WithNullInstance_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                AttributeHelper.GetPropertyDescription((object)null!, "SomeProperty"));
            exception.ParamName.Should().Be("instance");
        }

        [Fact]
        public void GetPropertyDescription_WithObjectInstanceAndInheritedProperty_WorksCorrectly()
        {
            // Arrange
            var instance = new DerivedTestClass();

            // Act
            var result = AttributeHelper.GetPropertyDescription(instance, "BaseProperty");

            // Assert
            result.Should().Be("Base Property Description");
        }

        [Fact]
        public void GetPropertyDescription_WithObjectInstanceAndStaticProperty_WorksCorrectly()
        {
            // Arrange - Use a regular class that has a static property
            var instance = new TestClassWithStaticProperty();

            // Act
            var result = AttributeHelper.GetPropertyDescription(instance, "StaticProperty");

            // Assert
            result.Should().Be("Static Property Description");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void GetDescription_WithGenericEnum_WorksCorrectly()
        {
            // Arrange
            var genericEnum = TestGenericValue.ValueWithDescription;

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

        private enum TestValueWithoutDescription
        {
            ValueWithoutDescription
        }

        private enum TestGenericValue
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

            // Constructor for test instantiation
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors",
                                                            Justification = "This is a test class and we need to instantiate it")]
            public TestClassWithStaticProperty() { }
        }

        #endregion
    }
}