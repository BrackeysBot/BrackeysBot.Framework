using System;
using BrackeysBot.API.Plugins;

namespace BrackeysBot.API.Exceptions;

/// <summary>
///     The exception that is thrown when an attempt was made to operate on a plugin that is not currently loaded.
/// </summary>
public sealed class PluginNotLoadedException : Exception
{
    internal PluginNotLoadedException(IPlugin plugin)
        : base($"{plugin} is not loaded.")
    {
        Plugin = plugin;
    }

    /// <summary>
    ///     Gets the plugin.
    /// </summary>
    /// <value>The plugin.</value>
    public IPlugin Plugin { get; }
}
