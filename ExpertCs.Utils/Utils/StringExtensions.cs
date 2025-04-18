using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;

namespace ExpertCs.Utils;

/// <summary>
/// Extensions for working with strings.
/// </summary>
public static partial class StringExtensions
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
    /// var ret1 = ExpertCs.StringExtensions.GetInterpolatedString(c, s);
    ///
    /// var ret2 = c.GetInterpolatedString(s, System.Globalization.CultureInfo.GetCultureInfo("en"));
    /// </example>
    /// <param name="contextObject">Объект.</param>
    /// <param name="template">Выражение для форматирования.</param>
    /// <param name="formatProvider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <returns></returns>
    public static string GetInterpolatedString(
        this object? contextObject,
        string template,
        IFormatProvider? formatProvider = null)
    {
        contextObject ??= EmptyObject;
        var cache = _caches.GetOrAdd(contextObject.GetType(), _ => new());
        return cache.Interpolate(contextObject, template, formatProvider);
    }

    private static readonly object EmptyObject = new();

    private static readonly ConcurrentDictionary<Type, TypeTemplateCache> _caches = new();

    private record TemplateCache(string Format, Parameter[] Parameters);

    [DebuggerDisplay("{Expression}")]
    private class Parameter(string expression)
    {
        public string Expression { get; } = expression;

        private Delegate? _delegate;

        public override int GetHashCode() => Expression.GetHashCode();

        public override bool Equals(object? obj) => obj is Parameter p && p.Expression.Equals(Expression);

        public object? EvalExpression(object obj)
        {
            var type = obj.GetType();
            try
            {
                _delegate ??= DynamicExpression.ParseLambda(type, null, Expression).Compile(true);
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

        internal string Interpolate(object obj, string template, IFormatProvider? formatProvider)
        {
            var cache = GetTemplate(template);
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
                var para = new Dictionary<Parameter, int>();
                var format = _regex.Replace(t, m => MatchEval(m, para));
                return new TemplateCache(
                    format,
                    para.OrderBy(p => p.Value).Select(p => p.Key).ToArray());
            });

        private string MatchEval(Match m, Dictionary<Parameter, int> para)
        {
            var gr = m.Groups.Cast<Group>().Select(g => g.Value.Trim()).ToArray();
            var expression = $"{gr[1]}{gr[2]}";
            var p = _parameters.GetOrAdd(expression, e => new Parameter(e));
            if (!para.TryGetValue(p, out var index))
                index = para[p] = para.Count;
            var z = m.Value.Trim();
            return $"{z.First()}{index}{z.Last()}";
        }
    }
}
