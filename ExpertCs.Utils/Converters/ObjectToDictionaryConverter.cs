using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ExpertCs.Utils.Converters;

/// <summary>
/// Статический класс для преобразования объектов в словарь строк
/// </summary>
public class ObjectToDictionaryConverter
{
    private readonly JsonSerializerOptions _serializerOptions;

    public ObjectToDictionaryConverter(JsonSerializerOptions? options)
    {
        _serializerOptions = options
                             ?? new()
                             {
                                 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                 Converters = { new JsonStringEnumConverter() }
                             };
    }

    /// <summary>
    /// Преобразует объект в словарь строк в формате "путь:значение"
    /// </summary>
    /// <typeparam name="T">Тип преобразуемого объекта</typeparam>
    /// <param name="obj">Объект для преобразования</param>
    /// <returns>Словарь, где ключ - путь к свойству, значение - строковое представление значения</returns>
    /// <exception cref="ArgumentNullException">Если передан null объект</exception>
    public Dictionary<string, string> Convert<T>(T? obj)
    {
        if (obj == null)
            return new();

        var dictionary = new Dictionary<string, string>();
        using var jsonDoc = JsonSerializer.SerializeToDocument(obj, _serializerOptions);
        ProcessElement(jsonDoc.RootElement, dictionary, "");
        return dictionary;
    }

    /// <summary>
    /// Рекурсивно обрабатывает элемент JSON и добавляет его в словарь
    /// </summary>
    /// <param name="element">Элемент JSON для обработки</param>
    /// <param name="dictionary">Словарь для сохранения результатов</param>
    /// <param name="currentPath">Текущий путь к элементу</param>
    /// <exception cref="NotSupportedException">При неподдерживаемом типе JSON-значения</exception>
    private static void ProcessElement(JsonElement element, Dictionary<string, string> dictionary, string currentPath)
    {
        try
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var newPath = string.IsNullOrEmpty(currentPath) 
                            ? property.Name 
                            : $"{currentPath}:{EscapeKeyPart(property.Name)}";
                        ProcessElement(property.Value, dictionary, newPath);
                    }
                    break;

                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var arrayElement in element.EnumerateArray())
                    {
                        var arrayPath = $"{currentPath}[{index}]";
                        ProcessElement(arrayElement, dictionary, arrayPath);
                        index++;
                    }
                    break;

                case JsonValueKind.String:
                    dictionary.Add(currentPath, EscapeString(element.GetString()!));
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        dictionary.Add(currentPath, intValue.ToString());
                    else if (element.TryGetInt64(out var longValue))
                        dictionary.Add(currentPath, longValue.ToString());
                    else if (element.TryGetDouble(out var doubleValue))
                        dictionary.Add(currentPath, doubleValue.ToString(CultureInfo.InvariantCulture));
                    else
                        dictionary.Add(currentPath, element.GetRawText());
                    break;

                case JsonValueKind.True:
                    dictionary.Add(currentPath, "true");
                    break;

                case JsonValueKind.False:
                    dictionary.Add(currentPath, "false");
                    break;

                case JsonValueKind.Null:
                    dictionary.Add(currentPath, "null");
                    break;

                default:
                    throw new NotSupportedException($"Неподдерживаемый тип JSON-значения: {element.ValueKind}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка обработки пути '{currentPath}'", ex);
        }
    }

    /// <summary>
    /// Экранирует специальные символы в строковых значениях
    /// </summary>
    /// <param name="value">Исходная строка</param>
    /// <returns>Экранированная строка</returns>
    private static string EscapeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '\"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                ':' => "\\:",
                '[' => "\\[",
                ']' => "\\]",
                _ => c
            });
        }
        return sb.ToString();
    }

    /// <summary>
    /// Экранирует специальные символы в частях ключей
    /// </summary>
    /// <param name="keyPart">Часть ключа</param>
    /// <returns>Экранированная часть ключа</returns>
    private static string EscapeKeyPart(string keyPart)
    {
        if (string.IsNullOrEmpty(keyPart))
            return keyPart;

        var sb = new StringBuilder(keyPart.Length);
        foreach (var c in keyPart)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                ':' => "\\:",
                '[' => "\\[",
                ']' => "\\]",
                _ => c
            });
        }
        return sb.ToString();
    }
}

/// <summary>
/// Статический класс для преобразования словаря строк в объект
/// </summary>
public class DictionaryToObjectConverter
{
    private readonly JsonSerializerOptions _deserializerOptions;

    public DictionaryToObjectConverter(JsonSerializerOptions? options)
    {
        _deserializerOptions = options
            ?? new()
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Converters = { new JsonStringEnumConverter() }
            };
    }

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
        if(dictionary == null)
            return default;
        
        if(!dictionary.Any())
            return default;

        try
        {
            var jsonObject = BuildJsonObject(dictionary);
            return jsonObject.Deserialize<T>(_deserializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Ошибка десериализации JSON объекта", ex);
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
                var pathParts = kvp.Key.Split(':')
                    .Select(UnescapeKeyPart)
                    .ToArray();

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

        int openBracket = part.IndexOf('[');
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

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            return JsonValue.Create(intValue);

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
            return JsonValue.Create(longValue);

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
            return JsonValue.Create(decimalValue);

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            return JsonValue.Create(doubleValue);

        return JsonValue.Create(UnescapeString(value));
    }

    /// <summary>
    /// Удаляет экранирование из строки
    /// </summary>
    /// <param name="value">Экранированная строка</param>
    /// <returns>Неэкранированная строка</returns>
    private static string UnescapeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var sb = new StringBuilder(value.Length);
        var escape = false;

        foreach (var c in value)
        {
            if (escape)
            {
                sb.Append(c switch
                {
                    '\\' => '\\',
                    '"' => '\"',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    ':' => ':',
                    '[' => '[',
                    ']' => ']',
                    _ => c
                });
                escape = false;
            }
            else if (c == '\\')
            {
                escape = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Удаляет экранирование из части ключа
    /// </summary>
    /// <param name="keyPart">Экранированная часть ключа</param>
    /// <returns>Неэкранированная часть ключа</returns>
    private static string UnescapeKeyPart(string keyPart)
    {
        if (string.IsNullOrEmpty(keyPart))
            return keyPart;

        var sb = new StringBuilder(keyPart.Length);
        var escape = false;

        foreach (var c in keyPart)
        {
            if (escape)
            {
                sb.Append(c);
                escape = false;
            }
            else if (c == '\\')
            {
                escape = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}