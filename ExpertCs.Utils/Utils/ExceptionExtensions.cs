using System;

namespace ExpertCs.Utils;

/// <summary>
/// Extensions for working with exceptions.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <typeparam name="T">Тип результата</typeparam>
    /// <typeparam name="TException">Тип игнориуемого исключения</typeparam>
    /// <param name="func">Вызываемая функция</param>
    /// <param name="exceptionResultFunc">Функция вызыватся в случае возникновения исключения</param>
    /// <returns></returns>
    public static T? InvokeIgnoreException<T, TException>(this Func<T?> func, Func<TException, T?>? exceptionResultFunc = null)
        where TException : Exception
    {
        try
        {
            return func();
        }
        catch (TException ex)
        {
            if (exceptionResultFunc != null)
                return exceptionResultFunc(ex);
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
    public static T? InvokeIgnoreException<T>(this Func<T?> func, Func<Exception, T?>? exceptionResultFunc = null)
        => func.InvokeIgnoreException<T, Exception>(exceptionResultFunc);

    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <typeparam name="TException">Тип игнориуемого исключения</typeparam>
    /// <param name="action">Вызываемая функция</param>
    public static void InvokeIgnoreException<TException>(this Action action)
        where TException : Exception
        => _ = new Func<object?>(() =>
        {
            action();
            return null;
        }).InvokeIgnoreException<object, TException>();

    /// <summary>
    /// Игнорирует исключение возникающее в вызываемой функции
    /// </summary>
    /// <param name="action">Вызываемая функция</param>
    public static void InvokeIgnoreException(this Action action)
        => action.InvokeIgnoreException<Exception>();
}
