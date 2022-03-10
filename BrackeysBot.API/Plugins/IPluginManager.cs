using BrackeysBot.API.Exceptions;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Represents an object which can load, manage, and unload plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    ///     Gets a read-only view of the plugins enabled by this bot.
    /// </summary>
    /// <value>A read-only view of <see cref="Plugin" /> instances.</value>
    IReadOnlyList<Plugin> EnabledPlugins { get; }

    /// <summary>
    ///     Gets a read-only view of the plugins loaded by this bot.
    /// </summary>
    /// <value>A read-only view of <see cref="Plugin" /> instances.</value>
    IReadOnlyList<Plugin> LoadedPlugins { get; }

    /// <summary>
    ///     Disables a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to disable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin" /> is <see langword="null" />.</exception>
    /// <exception cref="PluginNotLoadedException"><paramref name="plugin" /> refers to a plugin that is not loaded.</exception>
    void DisablePlugin(Plugin plugin);

    /// <summary>
    ///     Enables a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to enable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin" /> is <see langword="null" />.</exception>
    /// <exception cref="PluginNotLoadedException"><paramref name="plugin" /> refers to a plugin that is not loaded.</exception>
    void EnablePlugin(Plugin plugin);

    /// <summary>
    ///     Attempts to find a plugin by its type.
    /// </summary>
    /// <typeparam name="T">The plugin type.</typeparam>
    /// <returns>
    ///     The plugin, or <see langword="null" /> if the plugin with the specified type was not found, or is not loaded.
    /// </returns>
    T? GetPlugin<T>() where T : Plugin;

    /// <summary>
    ///     Attempts to find a plugin by its name.
    /// </summary>
    /// <param name="name">The name of the plugin to find.</param>
    /// <returns>
    ///     The plugin, or <see langword="null" /> if the plugin with the specified name was not found, or is not loaded.
    /// </returns>
    Plugin? GetPlugin(string name);

    /// <summary>
    ///     Loads a plugin with a specified name.
    /// </summary>
    /// <param name="name">The name of the plugin to load, sans the <c>.dll</c> extension.</param>
    /// <returns>The newly loaded <see cref="Plugin" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="name" /> is <see langword="null" />, empty, or consists of only whitespace characters.
    /// </exception>
    /// <exception cref="PluginNotFoundException">No plugin by the name <paramref name="name" /> could be found.</exception>
    /// <exception cref="InvalidPluginException">
    ///     The plugin does not contain an embedded resource named <c>plugin.json</c>.
    /// </exception>
    Plugin LoadPlugin(string name);

    /// <summary>
    ///     Unloads a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to unload.</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin" /> is <see langword="null" />.</exception>
    /// <exception cref="PluginNotLoadedException"><paramref name="plugin" /> refers to a plugin that is not loaded.</exception>
    void UnloadPlugin(Plugin plugin);
}
