using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrackeysBot.API.Exceptions;
using NLog;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Represents an object which can load, manage, and unload plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    ///     Gets a read-only view of the plugins enabled by this manager.
    /// </summary>
    /// <value>A read-only view of <see cref="IPlugin" /> instances which are currently enabled.</value>
    IReadOnlyList<IPlugin> EnabledPlugins { get; }

    /// <summary>
    ///     Gets a read-only view of the plugins loaded by this manager.
    /// </summary>
    /// <value>A read-only view of <see cref="IPlugin" /> instances which are currently loaded.</value>
    IReadOnlyList<IPlugin> LoadedPlugins { get; }

    /// <summary>
    ///     Gets the logger for this plugin manager.
    /// </summary>
    /// <value>The logger.</value>
    ILogger Logger { get; }

    /// <summary>
    ///     Disables a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to disable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin" /> is <see langword="null" />.</exception>
    /// <exception cref="PluginNotLoadedException"><paramref name="plugin" /> refers to a plugin that is not loaded.</exception>
    void DisablePlugin(IPlugin plugin);

    /// <summary>
    ///     Enables a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to enable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin" /> is <see langword="null" />.</exception>
    /// <exception cref="PluginNotLoadedException"><paramref name="plugin" /> refers to a plugin that is not loaded.</exception>
    void EnablePlugin(IPlugin plugin);

    /// <summary>
    ///     Attempts to find a plugin by its type.
    /// </summary>
    /// <typeparam name="T">The plugin type.</typeparam>
    /// <returns>
    ///     The plugin, or <see langword="default" /> if the plugin with the specified type was not found, or is not loaded.
    /// </returns>
    T? GetPlugin<T>() where T : IPlugin;

    /// <summary>
    ///     Attempts to find a plugin by its name.
    /// </summary>
    /// <param name="name">The name of the plugin to find.</param>
    /// <returns>
    ///     The plugin, or <see langword="null" /> if the plugin with the specified name was not found, or is not loaded.
    /// </returns>
    IPlugin? GetPlugin(string name);

    /// <summary>
    ///     Returns a value indicating whether a specified plugin is currently loaded and enabled.
    /// </summary>
    /// <param name="plugin">The plugin whose enabled state to retrieve.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="plugin" /> refers to a plugin that is loaded and enabled; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    bool IsPluginEnabled(IPlugin plugin);

    /// <summary>
    ///     Returns a value indicating whether a specified plugin is currently loaded.
    /// </summary>
    /// <param name="plugin">The plugin whose loaded state to retrieve.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="plugin" /> refers to a loaded plugin; otherwise, <see langword="false" />.
    /// </returns>
    bool IsPluginLoaded(IPlugin plugin);

    /// <summary>
    ///     Loads a plugin with a specified name.
    /// </summary>
    /// <param name="name">The name of the plugin to load, sans the <c>.dll</c> extension.</param>
    /// <returns>The newly loaded plugin.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="name" /> is <see langword="null" />, empty, or consists of only whitespace characters.
    /// </exception>
    /// <exception cref="PluginNotFoundException">No plugin by the name <paramref name="name" /> could be found.</exception>
    /// <exception cref="InvalidPluginException">
    ///     The plugin does not contain an embedded resource named <c>plugin.json</c>.
    /// </exception>
    IPlugin LoadPlugin(string name);

    /// <summary>
    ///     Loads all plugins that this plugin manager can detect.
    /// </summary>
    /// <returns>The read-only view of the loaded <see cref="MonoPlugin" /> instances.</returns>
    IReadOnlyList<IPlugin> LoadPlugins();

    /// <summary>
    ///     Retrieves a plugin by name, case-sensitively. A return value indicates whether the retrieval succeeded.
    /// </summary>
    /// <param name="name">The name of the plugin to retrieve.</param>
    /// <param name="plugin">
    ///     When this method returns, contains the plugin instance if the retrieval succeeded, or <see langword="null" /> if the
    ///     retrieval failed. Retrieval can fail if there is no loaded plugin with the specified name.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if a plugin with the name <paramref name="name" /> was successfully found; otherwise
    ///     <see langword="false" />.
    /// </returns>
    bool TryGetPlugin(string name, [NotNullWhen(true)] out IPlugin? plugin);

    /// <summary>
    ///     Unloads a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to unload.</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin" /> is <see langword="null" />.</exception>
    /// <exception cref="PluginNotLoadedException"><paramref name="plugin" /> refers to a plugin that is not loaded.</exception>
    void UnloadPlugin(IPlugin plugin);
}
