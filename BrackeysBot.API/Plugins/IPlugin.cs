using System;
using System.IO;
using BrackeysBot.API.Configuration;
using NLog;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Represents a bot plugin.
/// </summary>
public interface IPlugin : IDisposable, IConfigurationHolder
{
    /// <summary>
    ///     Gets the data directory for this plugin.
    /// </summary>
    /// <value>The data directory.</value>
    DirectoryInfo DataDirectory { get; }

    /// <summary>
    ///     Gets the date and time at which this plugin was last enabled.
    /// </summary>
    /// <value>
    ///     A <see cref="DateTimeOffset" /> representing the date and time at which this plugin was enabled, or
    ///     <see langword="null" /> if this plugin is not currently enabled.
    /// </value>
    DateTimeOffset? EnableTime { get; }

    /// <summary>
    ///     Gets the logger for this plugin.
    /// </summary>
    /// <value>The plugin's logger.</value>
    ILogger Logger { get; }

    /// <summary>
    ///     Gets the information about this plugin.
    /// </summary>
    /// <value>A <see cref="BrackeysBot.API.Plugins.PluginInfo" /> object containing</value>
    PluginInfo PluginInfo { get; }

    /// <summary>
    ///     Gets the manager which owns this plugin.
    /// </summary>
    /// <value>The plugin manager.</value>
    IPluginManager PluginManager { get; }

    /// <summary>
    ///     Gets the service provider for this plugin.
    /// </summary>
    /// <value>The service provider.</value>
    public IServiceProvider ServiceProvider { get; }
}
