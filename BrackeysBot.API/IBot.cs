using BrackeysBot.API.Plugins;
using NLog;

namespace BrackeysBot.API;

/// <summary>
///     Represents a bot application instance.
/// </summary>
public interface IBot
{
    /// <summary>
    ///     Gets the logger for this bot.
    /// </summary>
    /// <value>The logger.</value>
    ILogger Logger { get; }

    /// <summary>
    ///     Gets the plugin manager.
    /// </summary>
    /// <value>The plugin manager.</value>
    IPluginManager PluginManager { get; }

    /// <summary>
    ///     Gets the bot version.
    /// </summary>
    /// <value>The bot version.</value>
    static string Version { get; } = string.Empty;
}
