using BrackeysBot.API.Plugins;
using NLog;

namespace BrackeysBot.API;

/// <summary>
///     Represents a bot application instance.
/// </summary>
public interface IBot : IPluginManager
{
    /// <summary>
    ///     Gets the logger for this bot.
    /// </summary>
    /// <value>The logger.</value>
    ILogger Logger { get; }

    /// <summary>
    ///     Gets the plugin directory.
    /// </summary>
    /// <value>The plugin directory.</value>
    DirectoryInfo PluginDirectory { get; }

    /// <summary>
    ///     Gets the bot version.
    /// </summary>
    /// <value>The bot version.</value>
    string Version { get; }

    /// <summary>
    ///     Loads all plugins from <see cref="PluginDirectory" />.
    /// </summary>
    void LoadPlugins();
}
