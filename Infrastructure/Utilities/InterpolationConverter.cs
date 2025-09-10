using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpBridge.Models;
using SharpBridge.Models.Domain;

namespace SharpBridge.Infrastructure.Utilities
{
    /// <summary>
    /// JSON converter for IInterpolationDefinition that handles type serialization/deserialization
    /// </summary>
    public class InterpolationConverter : JsonConverter<IInterpolationDefinition>
    {
        private static readonly Type[] _availableTypes;

        /// <summary>
        /// Initializes the list of available interpolation types in the models assembly.
        /// </summary>
        static InterpolationConverter()
        {
            // Get all types in SharpBridge.Models namespace that implement IInterpolationDefinition
            var modelsAssembly = typeof(IInterpolationDefinition).Assembly;
            _availableTypes = modelsAssembly.GetTypes()
                .Where(t => t.Namespace == "SharpBridge.Models" &&
                           typeof(IInterpolationDefinition).IsAssignableFrom(t) &&
                           !t.IsInterface &&
                           !t.IsAbstract)
                .ToArray();
        }

        /// <inheritdoc />
        public override IInterpolationDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Special handling for BezierInterpolation - check if it's a flat array first
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // This is a flat array format for BezierInterpolation
                var bezierConverter = new BezierInterpolationConverter();
                return bezierConverter.Read(ref reader, typeof(BezierInterpolation), options);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object or array");
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeProperty))
            {
                throw new JsonException("Missing 'type' property in interpolation definition");
            }

            var typeName = typeProperty.GetString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException("Type property cannot be null or empty");
            }

            // Find the type by name
            var targetType = _availableTypes.FirstOrDefault(t => t.Name == typeName);
            if (targetType == null)
            {
                var availableTypes = string.Join(", ", _availableTypes.Select(t => t.Name));
                throw new JsonException($"Unknown interpolation type '{typeName}'. Available types: {availableTypes}");
            }

            // Special handling for BezierInterpolation in object format
            if (targetType == typeof(BezierInterpolation))
            {
                // Use the BezierInterpolationConverter to handle the object format
                var bezierConverter = new BezierInterpolationConverter();
                var jsonBytes = System.Text.Encoding.UTF8.GetBytes(root.GetRawText());
                var newReader = new Utf8JsonReader(jsonBytes);
                newReader.Read(); // Advance to the first token
                return bezierConverter.Read(ref newReader, typeof(BezierInterpolation), options);
            }

            // Deserialize to the specific type
            var jsonString = root.GetRawText();
            return JsonSerializer.Deserialize(jsonString, targetType, options) as IInterpolationDefinition;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, IInterpolationDefinition value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // Let the specific converter handle the serialization
            var specificConverter = options.GetConverter(value.GetType());
            if (specificConverter != null && specificConverter.GetType() != typeof(InterpolationConverter))
            {
                // Use the specific converter (like BezierInterpolationConverter)
                if (specificConverter is JsonConverter<IInterpolationDefinition> typedConverter)
                {
                    typedConverter.Write(writer, value, options);
                    return;
                }
            }

            // Serialize the object normally, then add the Type property
            var jsonString = JsonSerializer.Serialize(value, value.GetType(), options);
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            writer.WriteStartObject();

            // Add the Type property first (using lowercase to match README examples)
            writer.WriteString("type", value.GetType().Name);

            // Copy all other properties
            foreach (var property in root.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}