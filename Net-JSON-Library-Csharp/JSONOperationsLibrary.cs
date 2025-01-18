using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JSONOperationsLibrary
{
    /// <summary>
    /// Provides static methods for serializing and deserializing objects to and from JSON format.
    /// </summary>
    public static class JSONOperations
    {
        /// <summary>
        /// Cache for storing property information of types to improve performance during serialization.
        /// </summary>
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// A set to track objects that have already been serialized to detect and prevent circular references.
        /// </summary>
        private static readonly HashSet<object> SerializedObjects = new HashSet<object>();

        /// <summary>
        /// Serializes an object to its JSON string representation.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representing the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the object is null.</exception>
        public static string Serialize(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");
            }

            // Clear the set of serialized objects to avoid interference from previous serializations.
            SerializedObjects.Clear();

            // Use a StringBuilder to efficiently build the JSON string.
            var jsonBuilder = new StringBuilder();

            // Serialize the object and append the result to the StringBuilder.
            SerializeValue(obj, jsonBuilder);

            // Return the constructed JSON string.
            return jsonBuilder.ToString();
        }

        /// <summary>
        /// Deserializes a JSON string into an object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the JSON string is null or empty.</exception>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed.</exception>
        public static object? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(nameof(json), "JSON string cannot be null or empty.");
            }

            // Initialize an index to track the current position in the JSON string.
            int index = 0;

            // Skip any leading whitespace in the JSON string.
            SkipWhitespace(json, ref index);

            // Determine the type of the root element (object or array).
            char firstChar = json[index];
            if (firstChar == '{')
            {
                // Deserialize the JSON string as a dictionary (object).
                return DeserializeDictionary(json, ref index);
            }
            else if (firstChar == '[')
            {
                // Deserialize the JSON string as a list (array).
                return DeserializeList(json, ref index);
            }
            else
            {
                // Throw an exception if the root element is neither an object nor an array.
                throw new FormatException($"Unexpected root element '{firstChar}' at position {index}. Expected '{{' or '['.");
            }
        }

        /// <summary>
        /// Deserializes a JSON string into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize into.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An object of type T.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the JSON string is null or empty.</exception>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed or does not match the expected schema.</exception>
        public static T Deserialize<T>(string json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(nameof(json), "JSON string cannot be null or empty.");
            }

            // Deserialize the JSON string into a dictionary.
            var dict = Deserialize(json) as Dictionary<string, object>;
            if (dict == null)
            {
                throw new FormatException("JSON root element must be an object for custom type deserialization.");
            }

            // Map the dictionary to an object of type T.
            return MapDictionaryToType<T>(dict);
        }

        /// <summary>
        /// Serializes an object to a JSON string and writes it to a stream.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="stream">The stream to write the JSON string to.</param>
        /// <exception cref="ArgumentNullException">Thrown if the object or stream is null.</exception>
        public static void SerializeToStream(object? obj, Stream stream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");
            }

            // Use a StreamWriter to write the JSON string to the stream.
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.Write(Serialize(obj));
            }
        }

        /// <summary>
        /// Deserializes a JSON string from a stream into an object.
        /// </summary>
        /// <param name="stream">The stream to read the JSON string from.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the stream is null.</exception>
        public static object? DeserializeFromStream(Stream stream)
        {
            // Use a StreamReader to read the JSON string from the stream.
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                return Deserialize(reader.ReadToEnd());
            }
        }

        /// <summary>
        /// Validates a JSON string against a schema.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="schema">A dictionary representing the expected schema (keys and their types).</param>
        /// <returns>True if the JSON string matches the schema; otherwise, throws an exception.</returns>
        /// <exception cref="FormatException">Thrown if the JSON string does not match the schema.</exception>
        public static bool ValidateSchema(string json, Dictionary<string, Type> schema)
        {
            // Deserialize the JSON string into a dictionary.
            var dict = Deserialize(json) as Dictionary<string, object>;
            if (dict == null)
            {
                throw new FormatException("JSON root element must be an object for schema validation.");
            }

            // Validate each key and its type in the schema.
            foreach (var key in schema.Keys)
            {
                if (!dict.ContainsKey(key))
                {
                    throw new FormatException($"Missing required key '{key}' in JSON.");
                }

                if (dict[key] != null && dict[key].GetType() != schema[key])
                {
                    throw new FormatException($"Type mismatch for key '{key}'. Expected {schema[key]}, got {dict[key]?.GetType()}.");
                }
            }

            return true;
        }

        /// <summary>
        /// Serializes a value to its JSON representation and appends it to the StringBuilder.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="jsonBuilder">The StringBuilder to append the JSON representation to.</param>
        /// <exception cref="NotSupportedException">Thrown if the value type is not supported for serialization.</exception>
        private static void SerializeValue(object? value, StringBuilder jsonBuilder)
        {
            if (value == null)
            {
                // Append "null" for null values.
                jsonBuilder.Append("null");
            }
            else if (value is string str)
            {
                // Serialize strings with proper escaping.
                SerializeString(str, jsonBuilder);
            }
            else if (value is bool b)
            {
                // Append "true" or "false" for boolean values.
                jsonBuilder.Append(b ? "true" : "false");
            }
            else if (value is int || value is long || value is double || value is float || value is decimal)
            {
                // Append numeric values directly.
                jsonBuilder.Append(value);
            }
            else if (value is DateTime dateTime)
            {
                // Serialize DateTime values in ISO 8601 format.
                jsonBuilder.Append($"\"{dateTime:o}\"");
            }
            else if (value is Enum enumValue)
            {
                // Serialize enum values as strings.
                jsonBuilder.Append($"\"{enumValue}\"");
            }
            else if (value is IDictionary<string, object> dict)
            {
                // Serialize dictionaries as JSON objects.
                SerializeDictionary(dict, jsonBuilder);
            }
            else if (value is IEnumerable<object> list)
            {
                // Serialize lists as JSON arrays.
                SerializeList(list, jsonBuilder);
            }
            else if (value.GetType().IsClass)
            {
                // Check for circular references in custom class objects.
                if (SerializedObjects.Contains(value))
                {
                    throw new InvalidOperationException("Circular reference detected.");
                }
                SerializedObjects.Add(value);

                // Serialize custom class objects by serializing their properties.
                SerializeCustomType(value, jsonBuilder);
            }
            else
            {
                // Throw an exception for unsupported types.
                throw new NotSupportedException($"Type {value.GetType()} is not supported for serialization.");
            }
        }

        /// <summary>
        /// Serializes a string with proper escaping for JSON.
        /// </summary>
        /// <param name="str">The string to serialize.</param>
        /// <param name="jsonBuilder">The StringBuilder to append the serialized string to.</param>
        private static void SerializeString(string str, StringBuilder jsonBuilder)
        {
            // Append the opening quote.
            jsonBuilder.Append('"');

            // Iterate through each character in the string.
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        // Escape double quotes.
                        jsonBuilder.Append("\\\"");
                        break;
                    case '\\':
                        // Escape backslashes.
                        jsonBuilder.Append("\\\\");
                        break;
                    case '\b':
                        // Escape backspace.
                        jsonBuilder.Append("\\b");
                        break;
                    case '\f':
                        // Escape form feed.
                        jsonBuilder.Append("\\f");
                        break;
                    case '\n':
                        // Escape newline.
                        jsonBuilder.Append("\\n");
                        break;
                    case '\r':
                        // Escape carriage return.
                        jsonBuilder.Append("\\r");
                        break;
                    case '\t':
                        // Escape tab.
                        jsonBuilder.Append("\\t");
                        break;
                    default:
                        // Append the character as-is.
                        jsonBuilder.Append(c);
                        break;
                }
            }

            // Append the closing quote.
            jsonBuilder.Append('"');
        }

        /// <summary>
        /// Serializes a dictionary to its JSON representation.
        /// </summary>
        /// <param name="dict">The dictionary to serialize.</param>
        /// <param name="jsonBuilder">The StringBuilder to append the serialized dictionary to.</param>
        private static void SerializeDictionary(IDictionary<string, object> dict, StringBuilder jsonBuilder)
        {
            // Append the opening brace.
            jsonBuilder.Append('{');

            // Track whether the current key-value pair is the first in the dictionary.
            bool first = true;

            // Iterate through each key-value pair in the dictionary.
            foreach (var kvp in dict)
            {
                if (!first)
                {
                    // Append a comma between key-value pairs.
                    jsonBuilder.Append(',');
                }

                // Serialize the key.
                jsonBuilder.Append('"').Append(kvp.Key).Append('"').Append(':');

                // Serialize the value.
                SerializeValue(kvp.Value, jsonBuilder);

                // Mark that the first key-value pair has been processed.
                first = false;
            }

            // Append the closing brace.
            jsonBuilder.Append('}');
        }

        /// <summary>
        /// Serializes a list to its JSON representation.
        /// </summary>
        /// <param name="list">The list to serialize.</param>
        /// <param name="jsonBuilder">The StringBuilder to append the serialized list to.</param>
        private static void SerializeList(IEnumerable<object> list, StringBuilder jsonBuilder)
        {
            // Append the opening bracket.
            jsonBuilder.Append('[');

            // Track whether the current item is the first in the list.
            bool first = true;

            // Iterate through each item in the list.
            foreach (var item in list)
            {
                if (!first)
                {
                    // Append a comma between items.
                    jsonBuilder.Append(',');
                }

                // Serialize the item.
                SerializeValue(item, jsonBuilder);

                // Mark that the first item has been processed.
                first = false;
            }

            // Append the closing bracket.
            jsonBuilder.Append(']');
        }

        /// <summary>
        /// Serializes a custom class object by serializing its properties.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="jsonBuilder">The StringBuilder to append the serialized object to.</param>
        private static void SerializeCustomType(object obj, StringBuilder jsonBuilder)
        {
            // Get the properties of the object's type from the cache.
            var properties = GetProperties(obj.GetType());

            // Append the opening brace.
            jsonBuilder.Append('{');

            // Track whether the current property is the first in the object.
            bool first = true;

            // Iterate through each property of the object.
            foreach (var property in properties)
            {
                if (!first)
                {
                    // Append a comma between properties.
                    jsonBuilder.Append(',');
                }

                // Serialize the property name.
                jsonBuilder.Append('"').Append(property.Name).Append('"').Append(':');

                // Serialize the property value.
                SerializeValue(property.GetValue(obj), jsonBuilder);

                // Mark that the first property has been processed.
                first = false;
            }

            // Append the closing brace.
            jsonBuilder.Append('}');
        }

        /// <summary>
        /// Retrieves the properties of a type from the cache or uses reflection to get them.
        /// </summary>
        /// <param name="type">The type to get properties for.</param>
        /// <returns>An array of PropertyInfo objects representing the properties of the type.</returns>
        private static PropertyInfo[] GetProperties(Type type)
        {
            if (!PropertyCache.ContainsKey(type))
            {
                // Use reflection to get the properties of the type and cache them.
                PropertyCache[type] = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            }
            return PropertyCache[type];
        }

        /// <summary>
        /// Maps a dictionary to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="dict">The dictionary containing the property values.</param>
        /// <returns>An object of type T with properties set from the dictionary.</returns>
        private static T MapDictionaryToType<T>(Dictionary<string, object> dict) where T : new()
        {
            // Create a new instance of the specified type.
            var obj = new T();

            // Get the properties of the type.
            var properties = GetProperties(typeof(T));

            // Iterate through each property and set its value from the dictionary.
            foreach (var property in properties)
            {
                if (dict.ContainsKey(property.Name))
                {
                    property.SetValue(obj, Convert.ChangeType(dict[property.Name], property.PropertyType));
                }
            }

            return obj;
        }

        /// <summary>
        /// Deserializes a JSON object (dictionary) from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>A dictionary representing the JSON object.</returns>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed.</exception>
        private static Dictionary<string, object> DeserializeDictionary(string json, ref int index)
        {
            // Create a new dictionary to store the key-value pairs.
            var dict = new Dictionary<string, object>();

            // Skip the opening brace.
            index++;

            // Iterate through the JSON string until the closing brace is encountered.
            while (json[index] != '}')
            {
                // Skip any whitespace.
                SkipWhitespace(json, ref index);

                // Deserialize the key.
                string key = DeserializeString(json, ref index);

                // Skip any whitespace.
                SkipWhitespace(json, ref index);

                // Ensure the next character is a colon.
                if (json[index] != ':')
                {
                    throw new FormatException($"Expected ':' at position {index}.");
                }

                // Skip the colon.
                index++;

                // Deserialize the value.
                object? value = DeserializeValue(json, ref index);

                // Add the key-value pair to the dictionary if the value is not null.
                if (value != null)
                {
                    dict[key] = value;
                }

                // Skip any whitespace.
                SkipWhitespace(json, ref index);

                // If the next character is a comma, skip it.
                if (json[index] == ',')
                {
                    index++;
                }
            }

            // Skip the closing brace.
            index++;

            return dict;
        }

        /// <summary>
        /// Deserializes a JSON array (list) from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>A list representing the JSON array.</returns>
        private static List<object> DeserializeList(string json, ref int index)
        {
            // Create a new list to store the array elements.
            var list = new List<object>();

            // Skip the opening bracket.
            index++;

            // Iterate through the JSON string until the closing bracket is encountered.
            while (json[index] != ']')
            {
                // Skip any whitespace.
                SkipWhitespace(json, ref index);

                // Deserialize the value.
                object? value = DeserializeValue(json, ref index);

                // Add the value to the list if it is not null.
                if (value != null)
                {
                    list.Add(value);
                }

                // Skip any whitespace.
                SkipWhitespace(json, ref index);

                // If the next character is a comma, skip it.
                if (json[index] == ',')
                {
                    index++;
                }
            }

            // Skip the closing bracket.
            index++;

            return list;
        }

        /// <summary>
        /// Deserializes a JSON value from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed.</exception>
        private static object? DeserializeValue(string json, ref int index)
        {
            // Skip any whitespace.
            SkipWhitespace(json, ref index);

            // Determine the type of the value based on the current character.
            char currentChar = json[index];
            if (currentChar == '{')
            {
                // Deserialize the value as a dictionary.
                return DeserializeDictionary(json, ref index);
            }
            else if (currentChar == '[')
            {
                // Deserialize the value as a list.
                return DeserializeList(json, ref index);
            }
            else if (currentChar == '"')
            {
                // Deserialize the value as a string.
                return DeserializeString(json, ref index);
            }
            else if (char.IsDigit(currentChar) || currentChar == '-')
            {
                // Deserialize the value as a number.
                return DeserializeNumber(json, ref index);
            }
            else if (currentChar == 't' || currentChar == 'f')
            {
                // Deserialize the value as a boolean.
                return DeserializeBool(json, ref index);
            }
            else if (currentChar == 'n')
            {
                // Deserialize the value as null.
                return DeserializeNull(json, ref index);
            }
            else
            {
                // Throw an exception for unexpected characters.
                throw new FormatException($"Unexpected character '{currentChar}' at position {index}.");
            }
        }

        /// <summary>
        /// Deserializes a JSON string from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>The deserialized string.</returns>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed.</exception>
        private static string DeserializeString(string json, ref int index)
        {
            // Skip the opening quote.
            index++;

            // Use a StringBuilder to efficiently build the string.
            var result = new StringBuilder();

            // Iterate through the JSON string until the closing quote is encountered.
            while (json[index] != '"')
            {
                if (json[index] == '\\')
                {
                    // Handle escape sequences.
                    index++; // Skip the backslash.
                    switch (json[index])
                    {
                        case '"':
                            result.Append('"');
                            break;
                        case '\\':
                            result.Append('\\');
                            break;
                        case '/':
                            result.Append('/');
                            break;
                        case 'b':
                            result.Append('\b');
                            break;
                        case 'f':
                            result.Append('\f');
                            break;
                        case 'n':
                            result.Append('\n');
                            break;
                        case 'r':
                            result.Append('\r');
                            break;
                        case 't':
                            result.Append('\t');
                            break;
                        case 'u':
                            // Handle Unicode escape sequences (e.g., \uXXXX).
                            string hex = json.Substring(index + 1, 4);
                            result.Append((char)Convert.ToInt32(hex, 16));
                            index += 4;
                            break;
                        default:
                            throw new FormatException($"Invalid escape sequence '\\{json[index]}' at position {index}.");
                    }
                }
                else
                {
                    // Append the character as-is.
                    result.Append(json[index]);
                }
                index++;
            }

            // Skip the closing quote.
            index++;

            return result.ToString();
        }

        /// <summary>
        /// Deserializes a JSON number from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>The deserialized number as a double or int.</returns>
        private static object DeserializeNumber(string json, ref int index)
        {
            // Track the starting position of the number.
            int start = index;

            // Iterate through the JSON string until the end of the number is reached.
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-'))
            {
                index++;
            }

            // Extract the number substring.
            string numberStr = json.Substring(start, index - start);

            // Determine if the number is a floating-point or integer.
            if (numberStr.Contains("."))
            {
                return double.Parse(numberStr);
            }
            else
            {
                return int.Parse(numberStr);
            }
        }

        /// <summary>
        /// Deserializes a JSON boolean from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>The deserialized boolean value.</returns>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed.</exception>
        private static bool DeserializeBool(string json, ref int index)
        {
            // Check for the "true" keyword.
            if (json.Substring(index, 4) == "true")
            {
                index += 4;
                return true;
            }
            // Check for the "false" keyword.
            else if (json.Substring(index, 5) == "false")
            {
                index += 5;
                return false;
            }
            else
            {
                throw new FormatException($"Unexpected boolean value at position {index}.");
            }
        }

        /// <summary>
        /// Deserializes a JSON null value from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="index">The current position in the JSON string.</param>
        /// <returns>null.</returns>
        /// <exception cref="FormatException">Thrown if the JSON string is malformed.</exception>
        private static object? DeserializeNull(string json, ref int index)
        {
            // Check for the "null" keyword.
            if (json.Substring(index, 4) == "null")
            {
                index += 4;
                return null;
            }
            else
            {
                throw new FormatException($"Unexpected null value at position {index}.");
            }
        }

        /// <summary>
        /// Skips any whitespace characters in the JSON string.
        /// </summary>
        /// <param name="json">The JSON string to process.</param>
        /// <param name="index">The current position in the JSON string.</param>
        private static void SkipWhitespace(string json, ref int index)
        {
            // Increment the index while the current character is whitespace.
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }
}