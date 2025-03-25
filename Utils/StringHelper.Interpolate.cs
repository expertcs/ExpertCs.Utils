using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// var s = "p1={IntField,10:n2}, p2={StrPop}, p3={IntField}, p4={(DateProp.Year+5):n2}, p5={DateProp:g}, 10={7+3}, p7={GetType().Name}, p8={DArray[1]:p2}";
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
        this object contextObject,
        string interpolatedExpression,
        IFormatProvider? formatProvider = null)
    {
        if (contextObject == null)
            contextObject = Interpolator.NullObject;
        var i = new Interpolator(interpolatedExpression);
        var ret = i.Interpolate(formatProvider, contextObject);
        return ret;
    }

    private class Interpolator
    {
        internal Interpolator(string template)
        {
            _interplateTemplate = template;
        }

        internal static readonly object NullObject = new();

        private const string PATTERN = @"\{(.*?)(\(.*?\))*(}|\:|,)";

        private static readonly Regex _regex = new(PATTERN);

        private readonly string _interplateTemplate;

        private readonly Dictionary<string, int> _para = new();

        internal string Interpolate(IFormatProvider? formatProvider, object obj)
        {
            var format = _regex.Replace(_interplateTemplate, MatchEval);
            return string.Format(
                formatProvider,
                format,
                 _para.Select(p => EvalExpression(obj, p.Key, p.Value)).ToArray());
        }


        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> _delegates = new();

        private string MatchEval(Match m)
        {
            var gr = m.Groups.Cast<Group>().Select(g => g.Value.Trim()).ToArray();
            var ex = $"{gr[1]}{gr[2]}";
            if (!_para.ContainsKey(ex))
                _para[ex] = _para.Count;
            var z = m.Value.Trim();
            return $"{z.First()}{_para[ex]}{z.Last()}";
        }

        private static object? EvalExpression(object obj, string expression, int index)
        {
            var tx = obj.GetType();
            var dcol = _delegates.GetOrAdd(tx, t => new ConcurrentDictionary<string, Delegate>());
            try
            {
                var _func = dcol.GetOrAdd(
                    expression,
                    e => System.Linq.Dynamic.DynamicExpression.ParseLambda(tx, default, e)
                    .Compile());
                return _func.DynamicInvoke(obj);
            }
            catch (Exception ex)
            {
                var message = $"Error parameter={index} value='{expression}'";
                throw new FormatException(message, ex);
            }
        }
    }
}
