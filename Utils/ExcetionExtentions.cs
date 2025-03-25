using System;

namespace ExpertCs.Utils;

/// <summary>
/// Extensions for working with excetions.
/// </summary>
public static class ExcetionExtentions
{
    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <typeparam name="T">Тип результата</typeparam>
    /// <typeparam name="TExcection">Тип игнориуемого исключения</typeparam>
    /// <param name="func">Вызываемая функция</param>
    /// <param name="exceptionResultFunc">Функция вызыватся в случае возникновения исключения</param>
    /// <returns></returns>
    public static T? RunIgnoreException<T, TExcection>(this Func<T> func, Func<T>? exceptionResultFunc = default)
        where TExcection : Exception
    {
        try
        {
            return func();
        }
        catch (TExcection)
        {
            if (exceptionResultFunc != null)
                return exceptionResultFunc();
            return default;
        }
    }

    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <typeparam name="T">Тип результата</typeparam>
    /// <param name="func">Вызываемая функция</param>
    /// <param name="exceptionResultFunc">Функция вызыватся в случае возникновения исключения</param>
    /// <returns></returns>
    public static T? RunIgnoreException<T>(this Func<T> func, Func<T>? exceptionResultFunc = default)
        => func.RunIgnoreException<T, Exception>(exceptionResultFunc);

    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <typeparam name="TExcection">Тип игнориуемого исключения</typeparam>
    /// <param name="action">Вызываемая функция</param>
    public static void RunIgnoreException<TExcection>(this Action action)
        where TExcection : Exception
    {
        var func = () =>
        {
            action();
            return default(object)!;
        };
        func.RunIgnoreException<object, TExcection>();
    }

    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <param name="action">Вызываемая функция</param>
    public static void RunIgnoreException(this Action action) => action.RunIgnoreException<Exception>();
}
