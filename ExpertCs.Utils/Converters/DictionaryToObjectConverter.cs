using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ExpertCs.Utils.Converters;

/// <summary>
/// Статический класс для преобразования словаря строк в объект
/// </summary>
public class DictionaryToObjectConverter
{
    private readonly JsonSerializerOptions _deserializerOptions;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="options"></param>
    public DictionaryToObjectConverter(JsonSerializerOptions? options = null)
         => _deserializerOptions = options ?? new()
         {
             PropertyNameCaseInsensitive = true,
             NumberHandling = JsonNumberHandling.AllowReadingFromString,
             Converters = { new JsonStringEnumConverter() }
         };

    /// <summary>
    /// Преобразует словарь строк в объект указанного типа
    /// </summary>
    /// <typeparam name="T">Тип целевого объекта</typeparam>
    /// <param name="dictionary">Словарь для преобразования</param>
    /// <returns>Новый экземпляр объекта типа T</returns>
    /// <exception cref="ArgumentNullException">Если передан null словарь</exception>
    /// <exception cref="InvalidOperationException">При ошибке десериализации</exception>
    public T? Convert<T>(Dictionary<string, string>? dictionary)
    {
        if (dictionary == null)
            return default;

        try
        {
            var jsonObject = BuildJsonObject(dictionary);
            return TryConvertObject<T>(jsonObject, out var result)
                ? result
                : jsonObject.Deserialize<T>(_deserializerOptions);
        }
        catch (JsonException ex)
        {
            throw new SerializationException("Ошибка десериализации JSON объекта", ex);
        }
    }

    private bool TryConvertObject<T>(JsonObject jsonObject, out T? result)
    {
        if (typeof(T) == typeof(JsonObject))
        {
            result = (T)(object)jsonObject;
            return true;
        }
        result = default;
        var types = new[] { typeof(object), typeof(ExpandoObject) };
        if (types.Contains(typeof(T)))
        {
            var ret = jsonObject.Deserialize<ExpandoObject>(_deserializerOptions)!;
            UpdateExpandoObject(ret);
            result = (T)(object)ret!;
            return true;
        }

        return false;
    }

    private void UpdateExpandoObject(ExpandoObject? obj)
    {
        if (obj == null)
            return;
        IDictionary<string, object?> retDict = obj;
        foreach (var kv in retDict.ToArray())
        {
            if (kv.Value is JsonElement element)
            {
                retDict[kv.Key] = GetValue(element);
            }
        }
    }

    private object? GetValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var value = element.Deserialize<ExpandoObject>(_deserializerOptions);
                UpdateExpandoObject(value);
                return value;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var arrayElement in element.EnumerateArray())
                {
                    var item = GetValue(arrayElement);
                    list.Add(item);
                }
                var ret = list.ToArray();
                var types = list.Select(x => x?.GetType()).Distinct().ToArray();
                if (types.Length == 1)
                {
                    var arr = Array.CreateInstance(types[0]!, ret.Length);
                    Array.Copy(ret, arr, ret.Length);
                    return arr;
                }
                return ret;

            case JsonValueKind.String:
                if (element.TryGetDateTimeOffset(out var dateTimeOffset))
                    return dateTimeOffset;
                if (element.TryGetDateTime(out var dateTime))
                    return dateTime;
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                else if (element.TryGetInt64(out var longValue))
                    return longValue;
                else if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;
                else if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                return element.GetRawText();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.GetRawText();
        }
    }

    /// <summary>
    /// Строит JsonObject из словаря строк
    /// </summary>
    /// <param name="dictionary">Исходный словарь</param>
    /// <returns>JsonObject с восстановленной структурой</returns>
    private static JsonObject BuildJsonObject(Dictionary<string, string> dictionary)
    {
        var root = new JsonObject();

        foreach (var kvp in dictionary)
        {
            try
            {
                var pathParts = kvp.Key.Split(':');

                var valueNode = ParseValue(kvp.Value);
                SetValueAtPath(root, pathParts, valueNode);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обработки ключа '{kvp.Key}'", ex);
            }
        }

        return root;
    }

    /// <summary>
    /// Устанавливает значение по указанному пути в JsonObject
    /// </summary>
    /// <param name="parent">Родительский объект</param>
    /// <param name="pathParts">Части пути</param>
    /// <param name="valueNode">Значение для установки</param>
    /// <param name="currentIndex">Текущий индекс в пути</param>
    /// <exception cref="InvalidOperationException">При конфликте пути</exception>
    private static void SetValueAtPath(JsonObject parent, string[] pathParts, JsonNode? valueNode, int currentIndex = 0)
    {
        var currentPart = pathParts[currentIndex];
        var isArrayPart = TryParseArrayNotation(currentPart, out var arrayName, out var arrayIndex);

        if (currentIndex == pathParts.Length - 1)
        {
            // Установка значения для конечной части пути
            if (isArrayPart)
            {
                var array = GetOrCreateArray(parent, arrayName!);
                EnsureArraySize(array, arrayIndex!.Value);
                array[arrayIndex.Value] = valueNode;
            }
            else
            {
                parent[currentPart] = valueNode;
            }
        }
        else
        {
            // Навигация по пути
            if (isArrayPart)
            {
                var array = GetOrCreateArray(parent, arrayName!);
                EnsureArraySize(array, arrayIndex!.Value);

                if (array[arrayIndex.Value] is not JsonObject nextObject)
                {
                    array[arrayIndex.Value] = new JsonObject();
                    nextObject = (JsonObject)array[arrayIndex.Value]!;
                }

                SetValueAtPath(nextObject, pathParts, valueNode, currentIndex + 1);
            }
            else
            {
                if (!parent.TryGetPropertyValue(currentPart, out var nextNode))
                {
                    nextNode = new JsonObject();
                    parent[currentPart] = nextNode;
                }

                if (nextNode is JsonObject nextObject)
                {
                    SetValueAtPath(nextObject, pathParts, valueNode, currentIndex + 1);
                }
                else
                {
                    throw new InvalidOperationException($"Конфликт пути на '{currentPart}'");
                }
            }
        }
    }

    /// <summary>
    /// Пытается разобрать нотацию массива из части пути
    /// </summary>
    /// <param name="part">Часть пути</param>
    /// <param name="name">Имя массива (выходной параметр)</param>
    /// <param name="index">Индекс в массиве (выходной параметр)</param>
    /// <returns>True, если часть пути является нотацией массива</returns>
    private static bool TryParseArrayNotation(string part, out string? name, out int? index)
    {
        name = null;
        index = null;

        var openBracket = part.IndexOf('[');
        if (openBracket < 0 || !part.EndsWith("]"))
            return false;

        name = part[..openBracket];
        var indexStr = part[(openBracket + 1)..^1];

        if (int.TryParse(indexStr, out var parsedIndex))
        {
            index = parsedIndex;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Получает или создает JsonArray в указанном объекте
    /// </summary>
    /// <param name="parent">Родительский объект</param>
    /// <param name="arrayName">Имя массива</param>
    /// <returns>Существующий или новый JsonArray</returns>
    /// <exception cref="InvalidOperationException">Если свойство не является массивом</exception>
    private static JsonArray GetOrCreateArray(JsonObject parent, string arrayName)
    {
        if (!parent.TryGetPropertyValue(arrayName, out var arrayNode))
        {
            arrayNode = new JsonArray();
            parent[arrayName] = arrayNode;
        }

        return arrayNode as JsonArray ?? throw new InvalidOperationException($"Свойство '{arrayName}' не является массивом");
    }

    /// <summary>
    /// Гарантирует, что массив имеет достаточный размер
    /// </summary>
    /// <param name="array">Массив</param>
    /// <param name="requiredIndex">Требуемый индекс</param>
    private static void EnsureArraySize(JsonArray array, int requiredIndex)
    {
        while (array.Count <= requiredIndex)
        {
            array.Add(null!);
        }
    }

    /// <summary>
    /// Преобразует строковое значение в JsonNode
    /// </summary>
    /// <param name="value">Строковое значение</param>
    /// <returns>JsonNode соответствующего типа</returns>
    private static JsonNode? ParseValue(string value)
    {
        if (value == "null")
            return JsonValue.Create<object?>(null);

        if (value == "true")
            return JsonValue.Create(true);

        if (value == "false")
            return JsonValue.Create(false);

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return JsonValue.Create(intValue);

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            return JsonValue.Create(longValue);

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
            return JsonValue.Create(decimalValue);

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            return JsonValue.Create(doubleValue);

        return JsonValue.Create(value);
    }
}