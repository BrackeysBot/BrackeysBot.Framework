using System;
using System.Collections.Generic;
using BrackeysBot.API.Plugins;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace BrackeysBot.Logging;

/// <summary>
///     Represents a plugin logger factory which uses an NLog provider.
/// </summary>
internal sealed class PluginLoggerFactory : ILoggerFactory
{
    private static readonly Dictionary<string, ILogger> Loggers = new(StringComparer.Ordinal);
    private readonly IPlugin _plugin;
    private readonly NLogLoggerProvider _provider = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginLoggerFactory" /> class.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    public PluginLoggerFactory(IPlugin plugin)
    {
        _plugin = plugin;
    }


    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
        // ignored
    }

    /// <inheritdoc />
    // ReSharper disable once RedundantAssignment
    public ILogger CreateLogger(string categoryName)
    {
        // categoryName will be in the form DisCatSharp.BaseDiscordClient - which we don't want.
        // so explicitly use the plugin's name for clarity!
        categoryName = $"{_plugin.PluginInfo.Name}.DiscordClient";

        lock (Loggers)
        {
            if (!Loggers.TryGetValue(categoryName, out ILogger? logger))
            {
                logger = _provider.CreateLogger(categoryName);
                Loggers[categoryName] = logger;
            }

            return logger;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _provider.Dispose();
    }
}
