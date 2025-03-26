using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExpertCs.Utils;

/// <summary>
/// Extensions for working with reflection.
/// </summary>
public static class ReflectionExtensions
{
    private static object? GetRuntimePropertyValue(this object? obj, IEnumerable<string> properties)
    {
        if (obj == default)
            return default;
        var first = properties.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
            return obj;

        var prop = obj.GetType().GetRuntimeProperty(first);
        var value = prop?.GetValue(obj, null);

        return value.GetRuntimePropertyValue(properties.Skip(1));
    }

    /// <summary>
    /// Возвращает значение цепчки свойств.
    /// Использует рефлексию <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty(Type, string)"/>
    /// </summary>
    /// <param name="obj">Объект</param>
    /// <param name="property">Свойства</param>
    /// <returns></returns>
    public static object? GetRuntimePropertyValue(this object? obj, string property)
        => obj.GetRuntimePropertyValue(property.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
