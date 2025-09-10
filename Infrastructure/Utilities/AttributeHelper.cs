using System;
using System.ComponentModel;
using System.Reflection;

namespace SharpBridge.Infrastructure.Utilities
{
    /// <summary>
    /// Utility class for extracting attribute information from types and members
    /// </summary>
    public static class AttributeHelper
    {
        /// <summary>
        /// Gets the Description attribute value from an enum value, or returns the enum name if no attribute is found
        /// </summary>
        /// <param name="enumValue">The enum value to get description for</param>
        /// <returns>Description from attribute or enum name as fallback</returns>
        public static string GetDescription(Enum enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field == null)
                return enumValue.ToString();

            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? enumValue.ToString();
        }

        /// <summary>
        /// Gets the Description attribute value from a property, or returns the property name if no attribute is found
        /// </summary>
        /// <param name="type">The type containing the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>Description from attribute or property name as fallback</returns>
        public static string GetPropertyDescription(Type type, string propertyName)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));

            var property = type.GetProperty(propertyName);
            if (property == null)
                return propertyName;

            var attribute = property.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? propertyName;
        }

        /// <summary>
        /// Gets the Description attribute value from a property using reflection on an instance
        /// </summary>
        /// <param name="instance">The instance containing the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>Description from attribute or property name as fallback</returns>
        public static string GetPropertyDescription(object instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return GetPropertyDescription(instance.GetType(), propertyName);
        }
    }
}