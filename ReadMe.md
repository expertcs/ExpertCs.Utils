# ExpertCs.Utils

Helpful utilities and classes
The library will be extended
Use at your own risk

## StringHelper.FormatWithExpressions - interpolates an input string using expressions calculated for a given object.
```C#
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
```

Полезный метод