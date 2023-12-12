#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToys.Helpers
{
    internal static class JsonTableHelper
    {
        /// <summary>
        /// Detects whether the given string is a valid JSON or not.
        /// </summary>
        internal static bool IsValid(string? input)
        {
            ConvertResult result = ConvertFromJson(input, ',');
            return result.Error == null;
        }

        internal static ConvertResult ConvertFromJson(string? text, char separator)
        {
            JObject[]? array = ParseJsonArray(text);
            if (array == null)
            {
                return new(new(), "", LanguageManager.Instance.JsonTable.JsonError);
            }

            JObject[] flattened = array.Select(o => FlattenJsonObject(o)).ToArray();

            var properties = flattened
                .SelectMany(o => o.Properties())
                .Select(p => p.Name)
                .Distinct()
                .ToList();

            if (properties.Count == 0)
            {
                return new(new(), "", LanguageManager.Instance.JsonTable.JsonError);
            }

            var table = new DataTable();
            table.Columns.AddRange(properties.Select(p => new DataColumn(p)).ToArray());

            var clipboard = new StringBuilder();
            clipboard.AppendLine(string.Join(separator, properties));

            foreach (JObject obj in flattened)
            {
                string?[] values = properties
                    .Select(p => obj[p]?.ToString()) // JObject indexer conveniently returns null for unknown properties
                    .ToArray();

                table.Rows.Add(values);
                clipboard.AppendLine(string.Join(separator, values));
            }

            return new(table, clipboard.ToString(), null);
        }

        internal class ConvertResult
        {
            public ConvertResult(DataTable data, string text, string? error)
            {
                Data = data;
                Text = text;
                Error = error;
            }

            public DataTable Data { get; }
            public string Text { get; }
            public string? Error { get; }
        }

        /// <summary>
        /// Parse the text to an array of JObject, or null if the text does not represent a JSON array of objects.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static JObject[]? ParseJsonArray(string? text)
        {
            try
            {
                // Coalesce to empty string to prevent ArgumentNullException (returns null instead).
                var array = JsonConvert.DeserializeObject(text ?? "") as JArray;
                return array?.Cast<JObject>().ToArray();
            }
            catch (JsonException)
            {
                return null;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        internal static JObject FlattenJsonObject(JObject json)
        {
            var flattened = new JObject();

            foreach (KeyValuePair<string, JToken?> kv in json)
            {
                if (kv.Value is JObject jobj)
                {
                    // Flatten objects by prefixing their property names with the parent property name, underscore separated.
                    foreach (KeyValuePair<string, JToken?> kv2 in FlattenJsonObject(jobj))
                    {
                        flattened.Add($"{kv.Key}_{kv2.Key}", kv2.Value);
                    }
                }
                else if (kv.Value is JValue)
                {
                    flattened[kv.Key] = kv.Value;
                }
                // else strip out any array values
            }

            return flattened;
        }
    }
}
