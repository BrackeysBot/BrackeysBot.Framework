using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API;
using BrackeysBot.API.Plugins;
using BrackeysBot.Plugins;
using Microsoft.Extensions.Hosting;
using NLog;

namespace BrackeysBot;

/// <summary>
///     Represents a bot application instance.
/// </summary>
internal sealed class BrackeysBotApp : BackgroundService, IBot
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrackeysBotApp" /> class.
    /// </summary>
    public BrackeysBotApp()
    {
        var assembly = Assembly.GetAssembly(typeof(BrackeysBotApp))!;
        Version = assembly.GetName().Version!.ToString(3);
    }

    /// <inheritdoc />
    public ILogger Logger { get; } = LogManager.GetLogger("BrackeysBot");

    /// <inheritdoc />
    public IPluginManager PluginManager { get; } = new SimplePluginManager();

    /// <inheritdoc />
    public string Version { get; }

    /// <summary>
    ///     Disables all currently-loaded and currently-enabled plugins.
    /// </summary>
    public void DisablePlugins()
    {
        foreach (Plugin plugin in PluginManager.EnabledPlugins)
            PluginManager.DisablePlugin(plugin);
    }

    /// <summary>
    ///     Enables all currently-loaded plugins.
    /// </summary>
    public void EnablePlugins()
    {
        foreach (Plugin plugin in PluginManager.LoadedPlugins)
            PluginManager.EnablePlugin(plugin);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.Info($"Starting Brackeys Bot version {Version}");

        PluginManager.LoadPlugins();
        Logger.Info($"Loaded {PluginManager.LoadedPlugins.Count} plugins");

        EnablePlugins();
        Logger.Info($"Enabled {PluginManager.EnabledPlugins.Count} plugins");

        return Task.CompletedTask;
    }
}
