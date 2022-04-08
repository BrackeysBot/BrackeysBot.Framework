using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
    private readonly List<Assembly> _libraries = new();
    private readonly List<IntPtr> _nativeLibraries = new();

    static BrackeysBotApp()
    {
        var apiAssembly = Assembly.GetAssembly(typeof(IBot))!;
        var assembly = Assembly.GetAssembly(typeof(BrackeysBotApp))!;

        string? apiVersion = apiAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        apiVersion ??= apiAssembly.GetName().Version?.ToString();
        version ??= assembly.GetName().Version?.ToString();

        ApiVersion = apiVersion ?? "0.0.0";
        Version = version ?? "0.0.0";
    }

    /// <summary>
    ///     Gets the version of the API in use.
    /// </summary>
    /// <value>The API version.</value>
    public static string ApiVersion { get; }

    /// <summary>
    ///     Gets the <c>libraries/managed</c> directory for this bot.
    /// </summary>
    /// <value>The <c>libraries/managed</c> directory.</value>
    public DirectoryInfo ManagedLibrariesDirectory { get; } = new(Path.Join("libraries", "managed"));

    /// <summary>
    ///     Gets the <c>libraries/native</c> directory for this bot.
    /// </summary>
    /// <value>The <c>libraries/native</c> directory.</value>
    public DirectoryInfo NativeLibrariesDirectory { get; } = new(Path.Join("libraries", "native"));

    /// <inheritdoc />
    public ILogger Logger { get; } = LogManager.GetLogger("BrackeysBot");

    /// <inheritdoc />
    public IPluginManager PluginManager { get; } = new SimplePluginManager();

    /// <inheritdoc />
    public static string Version { get; }

    /// <summary>
    ///     Disables all currently-loaded and currently-enabled plugins.
    /// </summary>
    public void DisablePlugins()
    {
        foreach (IPlugin plugin in PluginManager.EnabledPlugins)
            PluginManager.DisablePlugin(plugin);
    }

    /// <summary>
    ///     Enables all currently-loaded plugins.
    /// </summary>
    public void EnablePlugins()
    {
        foreach (IPlugin plugin in PluginManager.LoadedPlugins)
            PluginManager.EnablePlugin(plugin);
    }

    /// <summary>
    ///     Loads third party dependencies as found <see cref="ManagedLibrariesDirectory" />.
    /// </summary>
    public void LoadManagedLibraries()
    {
        if (!ManagedLibrariesDirectory.Exists) return;

        foreach (FileInfo file in ManagedLibrariesDirectory.EnumerateFiles("*.dll"))
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(file.FullName);
                if (!_libraries.Contains(assembly))
                    _libraries.Add(assembly);
                Logger.Debug($"Loaded {assembly.GetName()}");
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, $"Could not load library '{file.Name}' - SOME PLUGINS MAY NOT WORK");
            }
        }
    }

    /// <summary>
    ///     Loads native libraries found in <see cref="NativeLibrariesDirectory" />.
    /// </summary>
    public void LoadNativeLibraries()
    {
        if (!NativeLibrariesDirectory.Exists) return;

        foreach (FileInfo file in NativeLibrariesDirectory.EnumerateFiles("*"))
        {
            try
            {
                IntPtr ptr = NativeLibrary.Load(file.FullName);
                _nativeLibraries.Add(ptr);
                Logger.Debug($"Loaded {file.Name}");
            }
            catch (BadImageFormatException exception)
            {
                Logger.Error(exception, $"Could not load native library '{file.Name}' - SOME PLUGINS MAY NOT WORK");
            }
        }
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.Info($"Starting Brackeys Bot version {Version} with API version {ApiVersion}");

        LoadNativeLibraries();
        int nativeLibraryCount = _nativeLibraries.Count;
        Logger.Info($"Loaded {nativeLibraryCount} {(nativeLibraryCount == 1 ? "native library" : "native libraries")}");

        LoadManagedLibraries();
        int managedLibraryCount = _libraries.Count;
        Logger.Info($"Loaded {managedLibraryCount} {(managedLibraryCount == 1 ? "managed library" : "managed libraries")}");

        PluginManager.LoadPlugins();
        int loadedPluginCount = PluginManager.LoadedPlugins.Count;
        Logger.Info($"Loaded {loadedPluginCount} {(loadedPluginCount == 1 ? "plugin" : "plugins")}");

        EnablePlugins();
        int enabledPluginCount = PluginManager.EnabledPlugins.Count;
        Logger.Info($"Enabled {enabledPluginCount} {(enabledPluginCount == 1 ? "plugin" : "plugins")}");

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        DisablePlugins();

        foreach (IPlugin plugin in PluginManager.LoadedPlugins)
            PluginManager.UnloadPlugin(plugin);

        foreach (IntPtr ptr in _nativeLibraries)
            NativeLibrary.Free(ptr);

        return base.StopAsync(cancellationToken);
    }
}
