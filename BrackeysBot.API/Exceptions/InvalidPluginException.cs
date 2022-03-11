using System;

namespace BrackeysBot.API.Exceptions;

/// <summary>
///     The exception that is thrown when an attempt to load a plugin failed because the plugin was invalid in some way.
/// </summary>
public sealed class InvalidPluginException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InvalidPluginException" /> class.
    /// </summary>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="message">The exception message.</param>
    internal InvalidPluginException(string pluginName, string message)
        : base(message)
    {
        PluginName = pluginName;
    }

    /// <summary>
    ///     Gets the name of the invalid plugin.
    /// </summary>
    /// <value>The plugin name.</value>
    public string PluginName { get; }
}
