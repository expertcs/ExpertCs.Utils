using System;

namespace ExpertCs.Model;

/// <summary>
/// Интерфейс с айдюхой
/// </summary>
public interface IId
{
    /// <summary>
    /// Айдюха.
    /// </summary>
    object Id { get; }
}

/// <summary>
/// Другой интерфейс с айдюхой.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IId<T>
    where T : IComparable, IEquatable<T>
{
    /// <summary>
    /// Айдюха.
    /// </summary>
    T Id { get; }
}
