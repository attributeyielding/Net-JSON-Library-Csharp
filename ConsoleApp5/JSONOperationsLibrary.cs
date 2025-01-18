
using System.Reflection;
using System.Text;

namespace JSONOperationsLibrary
{
    public static class JSONOperations
    {
        // Cache for property info to improve reflection performance
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string Serialize(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");
            }

            var jsonBuilder = new StringBuilder();
            SerializeValue(obj, jsonBuilder);
            return jsonBuilder.ToString();
        }

        /// <summary>
        /// Deserializes a JSON string to an object or array.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A Dictionary<string, object> for JSON objects or a List<object> for JSON arrays.</returns>
        public static object? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(nameof(json), "JSON string cannot be null or empty.");
            }

            int index = 0;
            SkipWhitespace(json, ref index);

            // Determine if the root element is an object or an array
            char firstChar = json[index];
            if (firstChar == '{')
            {
                return DeserializeDictionary(json, ref index);
            }
            else if (firstChar == '[')
            {
                return DeserializeList(json, ref index);
            }
            else
            {
                throw new FormatException($"Unexpected root element '{firstChar}' at position {index}. Expected '{{' or '['.");
            }
        }

        /// <summary>
        /// Deserializes a JSON string to a specific type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An instance of the specified type.</returns>
        public static T Deserialize<T>(string json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(nameof(json), "JSON string cannot be null or empty.");
            }

            var dict = Deserialize(json) as Dictionary<string, object>;
            if (dict == null)
            {
                throw new FormatException("JSON root element must be an object for custom type deserialization.");
            }

            return MapDictionaryToType<T>(dict);
        }

        /// <summary>
        /// Serializes an object to a JSON string and writes it to a stream.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="stream">The stream to write to.</param>
        public static void SerializeToStream(object? obj, Stream stream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");
            }

            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.Write(Serialize(obj));
            }
        }

        /// <summary>
        /// Deserializes a JSON string from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A Dictionary<string, object> for JSON objects or a List<object> for JSON arrays.</returns>
        public static object? DeserializeFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                return Deserialize(reader.ReadToEnd());
            }
        }

        /// <summary>
        /// Validates a JSON string against a schema.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="schema">The schema to validate against.</param>
        /// <returns>True if the JSON string is valid; otherwise, false.</returns>
        public static bool ValidateSchema(string json, Dictionary<string, Type> schema)
        {
            var dict = Deserialize(json) as Dictionary<string, object>;
            if (dict == null)
            {
                throw new FormatException("JSON root element must be an object for schema validation.");
            }

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

        // Serialization Methods
        private static void SerializeValue(object? value, StringBuilder jsonBuilder)
        {
            if (value == null)
            {
                jsonBuilder.Append("null");
            }
            else if (value is string str)
            {
                SerializeString(str, jsonBuilder);
            }
            else if (value is bool b)
            {
                jsonBuilder.Append(b ? "true" : "false");
            }
            else if (value is int || value is long || value is double || value is float || value is decimal)
            {
                jsonBuilder.Append(value);
            }
            else if (value is IDictionary<string, object> dict)
            {
                SerializeDictionary(dict, jsonBuilder);
            }
            else if (value is IEnumerable<object> list)
            {
                SerializeList(list, jsonBuilder);
            }
            else if (value.GetType().IsClass)
            {
                SerializeCustomType(value, jsonBuilder);
            }
            else
            {
                throw new NotSupportedException($"Type {value.GetType()} is not supported for serialization.");
            }
        }

        private static void SerializeString(string str, StringBuilder jsonBuilder)
        {
            jsonBuilder.Append('"');
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        jsonBuilder.Append("\\\"");
                        break;
                    case '\\':
                        jsonBuilder.Append("\\\\");
                        break;
                    case '\b':
                        jsonBuilder.Append("\\b");
                        break;
                    case '\f':
                        jsonBuilder.Append("\\f");
                        break;
                    case '\n':
                        jsonBuilder.Append("\\n");
                        break;
                    case '\r':
                        jsonBuilder.Append("\\r");
                        break;
                    case '\t':
                        jsonBuilder.Append("\\t");
                        break;
                    default:
                        jsonBuilder.Append(c);
                        break;
                }
            }
            jsonBuilder.Append('"');
        }

        private static void SerializeDictionary(IDictionary<string, object> dict, StringBuilder jsonBuilder)
        {
            jsonBuilder.Append('{');
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first)
                {
                    jsonBuilder.Append(',');
                }
                jsonBuilder.Append('"').Append(kvp.Key).Append('"').Append(':');
                SerializeValue(kvp.Value, jsonBuilder);
                first = false;
            }
            jsonBuilder.Append('}');
        }

        private static void SerializeList(IEnumerable<object> list, StringBuilder jsonBuilder)
        {
            jsonBuilder.Append('[');
            bool first = true;
            foreach (var item in list)
            {
                if (!first)
                {
                    jsonBuilder.Append(',');
                }
                SerializeValue(item, jsonBuilder);
                first = false;
            }
            jsonBuilder.Append(']');
        }

        private static void SerializeCustomType(object obj, StringBuilder jsonBuilder)
        {
            var properties = GetProperties(obj.GetType());
            jsonBuilder.Append('{');
            bool first = true;
            foreach (var property in properties)
            {
                if (!first)
                {
                    jsonBuilder.Append(',');
                }
                jsonBuilder.Append('"').Append(property.Name).Append('"').Append(':');
                SerializeValue(property.GetValue(obj), jsonBuilder);
                first = false;
            }
            jsonBuilder.Append('}');
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            if (!PropertyCache.ContainsKey(type))
            {
                PropertyCache[type] = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            }
            return PropertyCache[type];
        }

        // Deserialization Methods
        private static T MapDictionaryToType<T>(Dictionary<string, object> dict) where T : new()
        {
            var obj = new T();
            var properties = GetProperties(typeof(T));

            foreach (var property in properties)
            {
                if (dict.ContainsKey(property.Name))
                {
                    property.SetValue(obj, Convert.ChangeType(dict[property.Name], property.PropertyType));
                }
            }

            return obj;
        }

        private static Dictionary<string, object> DeserializeDictionary(string json, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // Skip '{'

            while (json[index] != '}')
            {
                SkipWhitespace(json, ref index);
                string key = DeserializeString(json, ref index);
                SkipWhitespace(json, ref index);
                if (json[index] != ':')
                {
                    throw new FormatException($"Expected ':' at position {index}.");
                }
                index++; // Skip ':'
                object? value = DeserializeValue(json, ref index);
                if (value != null)
                {
                    dict[key] = value;
                }

                SkipWhitespace(json, ref index);
                if (json[index] == ',')
                {
                    index++; // Skip ','
                }
            }

            index++; // Skip '}'
            return dict;
        }

        private static List<object> DeserializeList(string json, ref int index)
        {
            var list = new List<object>();
            index++; // Skip '['

            while (json[index] != ']')
            {
                SkipWhitespace(json, ref index);
                object? value = DeserializeValue(json, ref index);
                if (value != null)
                {
                    list.Add(value);
                }

                SkipWhitespace(json, ref index);
                if (json[index] == ',')
                {
                    index++; // Skip ','
                }
            }

            index++; // Skip ']'
            return list;
        }

        private static object? DeserializeValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);

            char currentChar = json[index];
            if (currentChar == '{')
            {
                return DeserializeDictionary(json, ref index);
            }
            else if (currentChar == '[')
            {
                return DeserializeList(json, ref index);
            }
            else if (currentChar == '"')
            {
                return DeserializeString(json, ref index);
            }
            else if (char.IsDigit(currentChar) || currentChar == '-')
            {
                return DeserializeNumber(json, ref index);
            }
            else if (currentChar == 't' || currentChar == 'f')
            {
                return DeserializeBool(json, ref index);
            }
            else if (currentChar == 'n')
            {
                return DeserializeNull(json, ref index);
            }
            else
            {
                throw new FormatException($"Unexpected character '{currentChar}' at position {index}.");
            }
        }

        private static string DeserializeString(string json, ref int index)
        {
            index++; // Skip '"'
            var result = new StringBuilder();

            while (json[index] != '"')
            {
                if (json[index] == '\\')
                {
                    index++; // Skip '\'
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
                            // Handle Unicode escape sequences (e.g., \uXXXX)
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
                    result.Append(json[index]);
                }
                index++;
            }

            index++; // Skip '"'
            return result.ToString();
        }

        private static object DeserializeNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-'))
            {
                index++;
            }
            string numberStr = json.Substring(start, index - start);
            if (numberStr.Contains("."))
            {
                return double.Parse(numberStr);
            }
            else
            {
                return int.Parse(numberStr);
            }
        }

        private static bool DeserializeBool(string json, ref int index)
        {
            if (json.Substring(index, 4) == "true")
            {
                index += 4;
                return true;
            }
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

        private static object? DeserializeNull(string json, ref int index)
        {
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

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }
}