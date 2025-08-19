using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Custom JSON converter for BezierInterpolation that supports both flat array and object formats
    /// </summary>
    public class BezierInterpolationConverter : JsonConverter<BezierInterpolation>
    {
        /// <inheritdoc />
        public override BezierInterpolation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Flat array format: [x1, y1, x2, y2, ...]
                return ReadFromFlatArray(ref reader);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Object format: {"controlPoints": [{"x": 0, "y": 0}, ...]}
                return ReadFromObjectFormat(ref reader, options);
            }
            else
            {
                throw new JsonException($"Expected array or object for BezierInterpolation, got {reader.TokenType}");
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, BezierInterpolation value, JsonSerializerOptions options)
        {
            // Write in flat array format for compactness (only middle control points)
            writer.WriteStartArray();

            // Skip the first point (implicit start) and last point (implicit end)
            for (int i = 1; i < value.ControlPoints.Count - 1; i++)
            {
                var point = value.ControlPoints[i];
                writer.WriteNumberValue(point.X);
                writer.WriteNumberValue(point.Y);
            }

            writer.WriteEndArray();
        }

        /// <summary>
        /// Reads Bezier control points from a flat numeric array format.
        /// </summary>
        private BezierInterpolation ReadFromFlatArray(ref Utf8JsonReader reader)
        {
            var controlPoints = new List<Point>();
            var values = new List<double>();

            // Read all numbers from the array
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    values.Add(reader.GetDouble());
                }
                else
                {
                    throw new JsonException($"Expected number in Bezier control points array, got {reader.TokenType}");
                }
            }

            // Validate we have an even number of values (pairs of x,y coordinates)
            if (values.Count % 2 != 0)
            {
                throw new JsonException($"Bezier control points array must have an even number of values (pairs of x,y coordinates), got {values.Count}");
            }

            // Add implicit start point (0, 0)
            controlPoints.Add(new Point { X = 0, Y = 0 });

            // Convert middle control points to Point objects
            for (int i = 0; i < values.Count; i += 2)
            {
                controlPoints.Add(new Point
                {
                    X = values[i],
                    Y = values[i + 1]
                });
            }

            // Add implicit end point (1, 1)
            controlPoints.Add(new Point { X = 1, Y = 1 });

            return new BezierInterpolation { ControlPoints = controlPoints };
        }

        /// <summary>
        /// Reads Bezier control points from an object format containing a 'controlPoints' array.
        /// </summary>
        private BezierInterpolation ReadFromObjectFormat(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            // Use the default object deserialization
            var jsonElement = JsonElement.ParseValue(ref reader);
            var controlPointsArray = jsonElement.TryGetProperty("controlPoints", out var cp) ? cp :
                                   throw new JsonException("Missing 'controlPoints' property");

            var controlPoints = new List<Point>();

            // Only support flat array format: [x1, y1, x2, y2, ...]
            if (controlPointsArray.ValueKind == JsonValueKind.Array)
            {
                var values = new List<double>();
                foreach (var element in controlPointsArray.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        values.Add(element.GetDouble());
                    }
                    else
                    {
                        throw new JsonException($"Expected number in controlPoints array, got {element.ValueKind}");
                    }
                }

                // Validate we have an even number of values (pairs of x,y coordinates)
                if (values.Count % 2 != 0)
                {
                    throw new JsonException($"controlPoints array must have an even number of values (pairs of x,y coordinates), got {values.Count}");
                }

                // Add implicit start point (0, 0)
                controlPoints.Add(new Point { X = 0, Y = 0 });

                // Convert middle control points to Point objects
                for (int i = 0; i < values.Count; i += 2)
                {
                    controlPoints.Add(new Point
                    {
                        X = values[i],
                        Y = values[i + 1]
                    });
                }

                // Add implicit end point (1, 1)
                controlPoints.Add(new Point { X = 1, Y = 1 });
            }
            else
            {
                throw new JsonException("controlPoints must be an array");
            }

            return new BezierInterpolation { ControlPoints = controlPoints };
        }
    }
}