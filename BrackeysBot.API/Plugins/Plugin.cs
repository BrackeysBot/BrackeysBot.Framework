using System;
using System.IO;
using System.Threading.Tasks;
using BrackeysBot.API.Configuration;
using DisCatSharp;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Represents a bot plugin.
/// </summary>
public abstract class Plugin : IDisposable, IConfigurationHolder
{
    /// <summary>
    ///     Gets the data directory for this plugin.
    /// </summary>
    /// <value>The data directory.</value>
    public DirectoryInfo DataDirectory { get; internal set; } = null!;

    /// <summary>
    ///     Gets the logger for this plugin.
    /// </summary>
    /// <value>The plugin's logger.</value>
    public ILogger Logger { get; internal set; } = null!;

    /// <summary>
    ///     Gets the information about this plugin.
    /// </summary>
    /// <value>A <see cref="BrackeysBot.API.Plugins.PluginInfo" /> object containing</value>
    public PluginInfo PluginInfo { get; internal set; } = null!;

    /// <summary>
    ///     Gets the manager which owns this plugin.
    /// </summary>
    /// <value>The plugin manager.</value>
    public IPluginManager PluginManager { get; internal set; } = null!;

    /// <summary>
    ///     Gets the underlying <see cref="DisCatSharp.DiscordClient" /> instance.
    /// </summary>
    /// <value>The underlying <see cref="DisCatSharp.DiscordClient" />.</value>
    protected internal DiscordClient? DiscordClient { get; internal set; }

    /// <summary>
    ///     Gets the service provider for this plugin.
    /// </summary>
    /// <value>The service provider.</value>
    public IServiceProvider ServiceProvider { get; internal set; } = null!;

    /// <inheritdoc />
    public IConfiguration Configuration { get; internal set; } = null!;

    /// <inheritdoc />
    public virtual void Dispose()
    {
    }

    /// <summary>
    ///     Allows configuration of the plugin's <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="services">The service collection.</param>
    protected internal virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    ///     Called when this plugin is disabled.
    /// </summary>
    protected internal virtual Task OnDisable()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called when this plugin is enabled.
    /// </summary>
    protected internal virtual Task OnEnable()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called when this plugin is loaded.
    /// </summary>
    /// <remarks>This method is always called upon a plugin's load, even if the plugin is not yet enabled.</remarks>
    /// <remarks>Event subscriptions for <see cref="DiscordClient" /> should be placed here.</remarks>
    protected internal virtual Task OnLoad()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called when this plugin is unloaded.
    /// </summary>
    /// <remarks>This method is always called upon a plugin's unload, even if the plugin is not currently enabled.</remarks>
    protected internal virtual Task OnUnload()
    {
        return Task.CompletedTask;
    }
}
