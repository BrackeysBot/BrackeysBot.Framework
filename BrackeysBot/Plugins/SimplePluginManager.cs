using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BrackeysBot.API.Exceptions;
using BrackeysBot.API.Plugins;
using BrackeysBot.Configuration;
using BrackeysBot.Resources;
using DisCatSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using ILogger = NLog.ILogger;

namespace BrackeysBot.Plugins;

/// <summary>
///     Represents a plugin manager which can load .NET assemblies as plugins.
/// </summary>
internal sealed class SimplePluginManager : IPluginManager
{
    private readonly List<Assembly> _loadedAssemblies = new();
    private readonly Dictionary<Plugin, bool> _loadedPlugins = new();
    private readonly Stack<string> _pluginLoadStack = new();

    /// <summary>
    ///     Gets the plugin directory.
    /// </summary>
    /// <value>The plugin directory.</value>
    public DirectoryInfo PluginDirectory { get; } = new("plugins");

    /// <inheritdoc />
    public IReadOnlyList<Plugin> EnabledPlugins => _loadedPlugins.Where(p => p.Value).Select(p => p.Key).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<Plugin> LoadedPlugins => _loadedPlugins.Keys.ToArray();

    /// <inheritdoc />
    public ILogger Logger { get; } = LogManager.GetLogger(nameof(SimplePluginManager));

    /// <inheritdoc />
    public void DisablePlugin(Plugin plugin)
    {
        if (plugin is null) throw new ArgumentNullException(nameof(plugin));
        if (!_loadedPlugins.ContainsKey(plugin)) throw new PluginNotLoadedException(plugin);
        if (!_loadedPlugins[plugin]) return;

        foreach (IHostedService hostedService in plugin.ServiceProvider.GetServices<IHostedService>())
        {
            try
            {
                hostedService.StopAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                Logger.Error(exception,
                    $"An exception was thrown when attempting to stop hosted service {hostedService.GetType()}");
            }
        }

        try
        {
            plugin.OnDisable();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to disable {plugin.PluginInfo.Name}");
        }

        _loadedPlugins[plugin] = false;

        plugin.DiscordClient?.DisconnectAsync();
        Logger.Info($"Disabled plugin {plugin.PluginInfo.Name} {plugin.PluginInfo.Version}");
    }

    /// <inheritdoc />
    public void EnablePlugin(Plugin plugin)
    {
        if (plugin is null) throw new ArgumentNullException(nameof(plugin));
        if (!_loadedPlugins.ContainsKey(plugin)) throw new PluginNotLoadedException(plugin);
        if (_loadedPlugins[plugin]) return;

        foreach (IHostedService hostedService in plugin.ServiceProvider.GetServices<IHostedService>())
        {
            try
            {
                hostedService.StartAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                Logger.Error(exception,
                    $"An exception was thrown when attempting to start hosted service {hostedService.GetType()}");
            }
        }

        try
        {
            plugin.OnEnable();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to enable {plugin.PluginInfo.Name}");
            return;
        }

        plugin.DiscordClient?.ConnectAsync();

        _loadedPlugins[plugin] = true;
        Logger.Info($"Enabled {plugin.PluginInfo.Name} {plugin.PluginInfo.Version}");
    }

    /// <inheritdoc />
    public T? GetPlugin<T>() where T : Plugin
    {
        return _loadedPlugins.Keys.FirstOrDefault(p => p is T) as T;
    }

    /// <inheritdoc />
    public Plugin? GetPlugin(string name)
    {
        return _loadedPlugins.Keys.FirstOrDefault(p => string.Equals(p.PluginInfo.Name, name, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public Plugin LoadPlugin(string name)
    {
        if (_pluginLoadStack.Contains(name))
            throw new CircularPluginDependencyException(name);

        Plugin? loadedPlugin =
            _loadedPlugins.Keys.FirstOrDefault(p => string.Equals(p.PluginInfo.Name, name, StringComparison.Ordinal));
        if (loadedPlugin is not null)
        {
            Logger.Debug($"Plugin {name} was requested to load, but is already loaded. Skipping!");
            return loadedPlugin;
        }

        _pluginLoadStack.Push(name);

        string pluginFileName = Path.Combine(PluginDirectory.FullName, $"{name}.dll");
        if (!File.Exists(pluginFileName))
            throw new PluginNotFoundException(name);

        var dependencies = new List<Plugin>();
        Assembly assembly = Assembly.LoadFile(pluginFileName); // DO NOT LoadFrom here. LoadFile does not load into domain
        if (_loadedAssemblies.Exists(a => a.Location == assembly.Location))
        {
            Logger.Debug($"Assembly {assembly} already loaded. Using cache");
            assembly = _loadedAssemblies.Find(a => a.Location == assembly.Location)!;
        }
        else
        {
            assembly = Assembly.LoadFrom(pluginFileName); // loads into domain
            _loadedAssemblies.Add(assembly);

            Logger.Debug($"Loaded new assembly {assembly}");
        }

        Type[] pluginTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Plugin))).ToArray();

        if (pluginTypes.Length == 0)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.NoPluginClass, typeof(Plugin)));

        if (pluginTypes.Length > 1)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.MultiplePluginsInAssembly, typeof(Plugin)));

        Type type = pluginTypes[0];

        var pluginAttribute = type.GetCustomAttribute<PluginAttribute>();
        if (pluginAttribute is null)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.NoPluginAttribute, typeof(PluginAttribute)));

        var dependenciesAttribute = type.GetCustomAttribute<PluginDependenciesAttribute>();
        if (dependenciesAttribute?.Dependencies is {Length: > 0} dependencyNames)
        {
            Logger.Debug(
                $"{pluginAttribute.Name} requires {dependencyNames.Length} dependencies: {string.Join(", ", dependencyNames)}");

            foreach (string dependencyName in dependencyNames)
            {
                try
                {
                    Plugin dependency = LoadPlugin(dependencyName);
                    dependencies.Add(dependency);
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, $"Could not load dependency '{dependencyName}'");
                    return null!; // return value will be ignored anyway
                }
            }
        }

        string assemblyVersion = assembly.GetName().Version?.ToString(3) ?? pluginAttribute.Version;
        if (!string.Equals(pluginAttribute.Version, assemblyVersion))
        {
            Logger.Warn(
                $"Plugin version {pluginAttribute.Version} and assembly version {assemblyVersion} do not match for {pluginAttribute.Name}!");
        }

        PluginInfo.PluginAuthorInfo? author = null;

        var authorAttribute = type.GetCustomAttribute<PluginAuthorAttribute>();
        if (authorAttribute is not null)
            author = new PluginInfo.PluginAuthorInfo(authorAttribute.Name, authorAttribute.Email, authorAttribute.Url);

        var descriptionAttribute = type.GetCustomAttribute<PluginDescriptionAttribute>();
        string description = descriptionAttribute?.Description ?? string.Empty;

        var pluginInfo = new PluginInfo(pluginAttribute.Name, pluginAttribute.Version, description, author,
            dependencies.Select(p => p.PluginInfo).ToArray());

        if (_loadedPlugins.Keys.Any(p => string.Equals(p.PluginInfo.Name, pluginInfo.Name, StringComparison.Ordinal)))
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.DuplicatePluginName, pluginInfo.Name));

        if (Activator.CreateInstance(type) is not Plugin plugin)
        {
            throw new TypeInitializationException(type.FullName,
                new InvalidPluginException(name, ExceptionMessages.NoDerivationOfPluginClass));
        }

        plugin.PluginManager = this;
        plugin.PluginInfo = pluginInfo;
        plugin.Logger = LogManager.GetLogger(pluginInfo.Name);
        (plugin.DataDirectory = new DirectoryInfo(Path.Combine(PluginDirectory.FullName, pluginInfo.Name))).Create();

        var jsonFileConfiguration = new JsonFileConfiguration();
        string configFilePath = Path.Combine(plugin.DataDirectory.FullName, "config.json");
        jsonFileConfiguration.ConfigurationFile = new FileInfo(configFilePath);
        jsonFileConfiguration.SaveDefault();
        plugin.Configuration = jsonFileConfiguration;

        var token = plugin.Configuration.Get<string>("discord.token");
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.Warn(
                $"No token was specified in the config file for {plugin.PluginInfo.Name}! No client will be created for this plugin.");
        }
        else
        {
            var intents = DiscordIntents.AllUnprivileged;
            var intentsAttribute = type.GetCustomAttribute<PluginIntentsAttribute>();

            if (intentsAttribute is not null)
                intents = intentsAttribute.Intents;

            plugin.DiscordClient ??= new DiscordClient(new DiscordConfiguration
            {
                Intents = intents,
                LoggerFactory = new NLogLoggerFactory(),
                Token = token
            });
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });

        serviceCollection.AddSingleton(this);
        serviceCollection.AddSingleton(plugin);
        serviceCollection.AddSingleton(plugin.Configuration);

        if (plugin.DiscordClient is not null)
            serviceCollection.AddSingleton(plugin.DiscordClient);

        plugin.ConfigureServices(serviceCollection);
        plugin.ServiceProvider = serviceCollection.BuildServiceProvider();

        plugin.OnLoad();

        _loadedPlugins.Add(plugin, false);

        Logger.Info($"Loaded plugin {pluginInfo.Name} {pluginInfo.Version}");
        _pluginLoadStack.Pop();
        return plugin;
    }

    /// <inheritdoc />
    public IReadOnlyList<Plugin> LoadPlugins()
    {
        try
        {
            PluginDirectory.Create();
        }
        catch (IOException exception)
        {
            Logger.Warn(exception, $"The plugin directory '{PluginDirectory.FullName}' could not be created.");
            return ArraySegment<Plugin>.Empty;
        }

        var plugins = new List<Plugin>();

        foreach (FileInfo file in PluginDirectory.EnumerateFiles("*.dll"))
        {
            string pluginName = Path.GetFileNameWithoutExtension(file.Name);

            Plugin? plugin = null;
            try
            {
                plugin = LoadPlugin(pluginName);
                if (plugin is not null)
                    plugins.Add(plugin);
            }
            catch (Exception exception)
            {
                if (plugin is not null)
                {
                    plugin.DiscordClient?.Dispose();
                    plugin.DiscordClient = null;
                    plugin.Dispose();
                }

                Logger.Error(exception, $"Could not load plugin {pluginName}");
            }
        }

        return plugins.AsReadOnly();
    }

    /// <inheritdoc />
    public void UnloadPlugin(Plugin plugin)
    {
        DisablePlugin(plugin);

        try
        {
            plugin.OnUnload();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to unload {plugin.PluginInfo.Name}");
        }

        plugin.DiscordClient?.Dispose();
        plugin.DiscordClient = null;
        plugin.Dispose();

        Logger.Info($"Unloaded plugin {plugin.PluginInfo.Name}");
        _loadedPlugins.Remove(plugin);
    }
}
