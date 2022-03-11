using System;

namespace BrackeysBot.API.Exceptions;

/// <summary>
///     The exception that is thrown when a plugin could not be loaded because it contains a circular dependency.
/// </summary>
public sealed class CircularPluginDependencyException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CircularPluginDependencyException" /> class.
    /// </summary>
    /// <param name="pluginName">The name of the plugin.</param>
    internal CircularPluginDependencyException(string pluginName)
        : base($"The plugin {pluginName} could not be loaded because it contains a circular dependency.")
    {
        PluginName = pluginName;
    }

    /// <summary>
    ///     Gets the name of the plugin which contains a circular dependency.
    /// </summary>
    /// <value>The plugin name.</value>
    public string PluginName { get; }
}
