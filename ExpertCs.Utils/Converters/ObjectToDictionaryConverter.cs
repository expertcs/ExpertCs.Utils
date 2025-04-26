using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpertCs.Utils.Converters;

/// <summary>
/// Статический класс для преобразования объектов в словарь строк
/// </summary>
public class ObjectToDictionaryConverter
{
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="options"></param>
    public ObjectToDictionaryConverter(JsonSerializerOptions? options = null)
        => _serializerOptions = options ?? new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

    /// <summary>
    /// Преобразует объект в словарь строк в формате "путь:значение"
    /// </summary>
    /// <typeparam name="T">Тип преобразуемого объекта</typeparam>
    /// <param name="obj">Объект для преобразования</param>
    /// <returns>Словарь, где ключ - путь к свойству, значение - строковое представление значения</returns>
    public Dictionary<string, string> Convert<T>(T? obj)
    {
        if (obj == null)
            return new();

        var dictionary = new Dictionary<string, string>();
        using var jsonDoc = ConvertToJson(obj);
        ProcessElement(jsonDoc.RootElement, dictionary, "");
        return dictionary;
    }

    /// <summary>
    /// Преобразует объект в <see href="JsonDocument"/>
    /// </summary>
    /// <typeparam name="T">Тип преобразуемого объекта</typeparam>
    /// <param name="obj">Объект для преобразования</param>
    /// <returns></returns>
    public JsonDocument ConvertToJson<T>(T? obj)
        => JsonSerializer.SerializeToDocument(obj, _serializerOptions);

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
                            : $"{currentPath}:{property.Name}";
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
                    dictionary.Add(currentPath, element.GetString()!);
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
            throw new SerializationException($"Ошибка обработки пути '{currentPath}'", ex);
        }
    }
}