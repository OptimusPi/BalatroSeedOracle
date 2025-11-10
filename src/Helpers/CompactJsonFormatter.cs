using System;
using System.Text;
using System.Text.Json;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Custom JSON formatter that keeps arrays on a single line (horizontal)
    /// instead of vertical formatting, making them much easier to edit.
    /// </summary>
    public static class CompactJsonFormatter
    {
        /// <summary>
        /// Formats JSON with arrays on one line up to a specified width.
        /// </summary>
        /// <param name="json">The JSON string to format</param>
        /// <param name="maxArrayWidth">Maximum line width before arrays wrap (default: 120)</param>
        /// <returns>Formatted JSON with horizontal arrays</returns>
        public static string Format(string json, int maxArrayWidth = 120)
        {
            try
            {
                // Parse the JSON to ensure it's valid
                var jsonDoc = JsonDocument.Parse(json);

                // Format with custom writer
                using var stream = new System.IO.MemoryStream();
                using var writer = new Utf8JsonWriter(
                    stream,
                    new JsonWriterOptions { Indented = true }
                );

                WriteElement(jsonDoc.RootElement, writer);
                writer.Flush();

                var formattedJson = Encoding.UTF8.GetString(stream.ToArray());

                // Post-process to compact short arrays onto single lines
                return CompactArrays(formattedJson, maxArrayWidth);
            }
            catch (Exception)
            {
                // If formatting fails, return original
                return json;
            }
        }

        private static void WriteElement(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        WriteElement(property.Value, writer);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        WriteElement(item, writer);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    writer.WriteStringValue(element.GetString());
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        writer.WriteNumberValue(intValue);
                    else if (element.TryGetInt64(out long longValue))
                        writer.WriteNumberValue(longValue);
                    else if (element.TryGetDouble(out double doubleValue))
                        writer.WriteNumberValue(doubleValue);
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;
            }
        }

        /// <summary>
        /// Post-processes formatted JSON to compact arrays onto single lines
        /// when they fit within the specified width.
        /// </summary>
        private static string CompactArrays(string json, int maxWidth)
        {
            var lines = json.Split('\n');
            var result = new StringBuilder();
            var i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();

                // Check if this line starts an array
                if (trimmed.StartsWith("["))
                {
                    // Try to compact the array
                    var (compactedArray, linesConsumed) = TryCompactArray(lines, i, maxWidth);

                    if (compactedArray != null)
                    {
                        result.AppendLine(compactedArray);
                        i += linesConsumed;
                        continue;
                    }
                }

                result.AppendLine(line);
                i++;
            }

            return result.ToString();
        }

        /// <summary>
        /// Attempts to compact an array onto a single line if it fits within maxWidth.
        /// </summary>
        private static (string? compactedLine, int linesConsumed) TryCompactArray(
            string[] lines,
            int startIndex,
            int maxWidth
        )
        {
            var indent = GetIndentation(lines[startIndex]);
            var arrayLines = new StringBuilder();
            var i = startIndex;
            var depth = 0;
            var hasNestedStructures = false;

            // Collect all lines that belong to this array
            while (i < lines.Length)
            {
                var line = lines[i].TrimStart();

                // Track nesting depth
                if (line.Contains("[") || line.Contains("{"))
                {
                    depth++;
                    if (depth > 1)
                        hasNestedStructures = true;
                }
                if (line.Contains("]") || line.Contains("}"))
                    depth--;

                arrayLines.Append(line);
                i++;

                // Array is complete when we return to depth 0
                if (depth == 0)
                    break;
            }

            var linesConsumed = i - startIndex;

            // Don't compact arrays with nested objects/arrays
            if (hasNestedStructures)
                return (null, 0);

            // Build the compacted line
            var compacted =
                indent
                + arrayLines
                    .ToString()
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("  ", " ")
                    .Replace("[ ", "[")
                    .Replace(" ]", "]")
                    .Replace(" ,", ",");

            // Only use compacted version if it fits within maxWidth
            if (compacted.Length <= maxWidth)
            {
                return (compacted, linesConsumed);
            }

            return (null, 0);
        }

        private static string GetIndentation(string line)
        {
            var indent = 0;
            while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t'))
            {
                indent++;
            }
            return line.Substring(0, indent);
        }
    }
}
