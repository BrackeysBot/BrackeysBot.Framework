using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API;
using Microsoft.Extensions.Hosting;
using NLog;

namespace BrackeysBot;

/// <summary>
///     Represents a bot application instance.
/// </summary>
internal sealed partial class BrackeysBotApp : BackgroundService, IBot
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
    public string Version { get; }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.Info($"Starting Brackeys Bot version {Version}");

        LoadPlugins();
        Logger.Info($"Loaded {_loadedPlugins.Count} plugins");

        EnablePlugins();
        Logger.Info($"Enabled {_loadedPlugins.Count(p => p.Value)} plugins");

        return Task.CompletedTask;
    }
}
