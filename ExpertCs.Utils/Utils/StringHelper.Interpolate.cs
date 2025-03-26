using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;

namespace ExpertCs.Utils;

/// <summary>
/// Extensions for working with strings.
/// </summary>
public static partial class StringHelper
{

    /// <summary>
    /// Интерполирует входную строку используя выражения вычесленные для заданного объекта.
    /// </summary>
    /// <example>
    /// var s = "p1={IntField,10:n2}, p2={StrProp}, p3={IntField}, p4={(DateProp.Year+5):n2}, p5={DateProp:g}, 10={7+3}, p7={GetType().Name}, p8={DArray[1]:p2}";
    ///
    /// var c = new
    /// {
    ///     DateProp = DateTime.Now,
    ///     IntField = 5,
    ///     StrProp = "-test text-",
    ///     DArray = new decimal[] { 2m, 4.3m, 7m }
    /// };
    /// 
    /// var ret1 = ExpertCs.StringUtils.StringHelper.FormatWithExpressions(c, s);
    ///
    /// var ret2 = c.FormatWithExpressions(s, System.Globalization.CultureInfo.GetCultureInfo("en"));
    /// </example>
    /// <param name="contextObject">Объект.</param>
    /// <param name="interpolatedExpression">Выражение для форматирования.</param>
    /// <param name="formatProvider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <returns></returns>
    public static string FormatWithExpressions(
        this object? contextObject,
        string interpolatedExpression,
        IFormatProvider? formatProvider = null)
    {
        if (contextObject == null)
            contextObject = EmptyObject;
        var cache = _caches.GetOrAdd(contextObject.GetType(), _ => new());
        return cache.Interpolate(contextObject, interpolatedExpression, formatProvider);
    }

    private static readonly object EmptyObject = new();

    private static readonly ConcurrentDictionary<Type, TypeTemplateCache> _caches = new();

    [DebuggerDisplay("{Format}")]
    private class TemplateCache()
    {
        public required string Format { get; init; }

        public required Parameter[] Parameters { get; init; }
    }

    [DebuggerDisplay("{Expression}")]
    private class Parameter
    {
        public required string Expression { get; init; }

        private Delegate? _delegate;

        public override int GetHashCode() => Expression.GetHashCode();

        public override bool Equals(object? obj) => obj is Parameter p && p.Expression.Equals(Expression);

        public object? EvalExpression(object obj)
        {
            var type = obj.GetType();
            try
            {
                _delegate ??= DynamicExpression.ParseLambda(type, default, Expression).Compile(true);
                return _delegate.DynamicInvoke(obj);
            }
            catch (Exception ex)
            {
                var message = $"Error expression='{Expression}'";
                throw new FormatException(message, ex);
            }
        }
    }

    private class TypeTemplateCache
    {
        private readonly ConcurrentDictionary<string, TemplateCache> _tempates = new();
        private readonly ConcurrentDictionary<string, Parameter> _parameters = new();

        private const string PATTERN = @"\{(.*?)(\(.*?\))*(}|\:|,)";
        private static readonly Regex _regex = new(PATTERN);

        internal string Interpolate(object obj, string tempate, IFormatProvider? formatProvider)
        {
            var cache = GetTemplate(tempate);
            return string.Format(
                formatProvider,
                cache.Format,
                cache.Parameters
                    .Select(p => p.EvalExpression(obj))
                    .ToArray());
        }

        private TemplateCache GetTemplate(string cache)
            => _tempates.GetOrAdd(cache, t =>
            {
                var para = new ConcurrentDictionary<Parameter, int>();
                var format = _regex.Replace(t, m => MatchEval(m, para));
                return new TemplateCache
                {
                    Format = format,
                    Parameters = para.OrderBy(p => p.Value).Select(p => p.Key).ToArray()
                };
            });

        private string MatchEval(Match m, ConcurrentDictionary<Parameter, int> para)
        {
            var gr = m.Groups.Cast<Group>().Select(g => g.Value.Trim()).ToArray();
            var expression = $"{gr[1]}{gr[2]}";
            var p = _parameters.GetOrAdd(expression, _ => new Parameter { Expression = expression });
            var index = para.GetOrAdd(p, _ => para.Count);
            var z = m.Value.Trim();
            return $"{z.First()}{index}{z.Last()}";
        }
    }
}
