using System;
using BrackeysBot.API.Plugins;

namespace BrackeysBot.API.Exceptions;

/// <summary>
///     The exception that is thrown when attempting to retrieve a plugin with <see cref="IPluginManager.GetPlugin{T}" />,
///     but the type parameter does not match the type of the retrieved plugin.
/// </summary>
public sealed class PluginTypeMismatchException : Exception
{
    internal PluginTypeMismatchException(Type expectedType, Type actualType)
    {
        ExpectedType = expectedType;
        ActualType = actualType;
    }

    /// <summary>
    ///     Gets the actual type.
    /// </summary>
    /// <value>The actual type.</value>
    public Type ActualType { get; }

    /// <summary>
    ///     Gets the expected type.
    /// </summary>
    /// <value>The expected type.</value>
    public Type ExpectedType { get; }
}
