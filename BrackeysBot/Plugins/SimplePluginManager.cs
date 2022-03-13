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
    private readonly Dictionary<IPlugin, bool> _loadedPlugins = new();
    private readonly Stack<string> _pluginLoadStack = new();

    /// <summary>
    ///     Gets the plugin directory.
    /// </summary>
    /// <value>The plugin directory.</value>
    public DirectoryInfo PluginDirectory { get; } = new("plugins");

    /// <inheritdoc />
    public IReadOnlyList<IPlugin> EnabledPlugins => _loadedPlugins.Where(p => p.Value).Select(p => p.Key).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.Keys.ToArray();

    /// <inheritdoc />
    public ILogger Logger { get; } = LogManager.GetLogger(nameof(SimplePluginManager));

    /// <inheritdoc />
    public void DisablePlugin(IPlugin plugin)
    {
        if (plugin is null) throw new ArgumentNullException(nameof(plugin));
        if (!_loadedPlugins.ContainsKey(plugin)) throw new PluginNotLoadedException(plugin);
        if (!_loadedPlugins[plugin]) return;
        if (plugin is not MonoPlugin monoPlugin) return;

        foreach (IHostedService hostedService in plugin.ServiceProvider.GetServices<IHostedService>())
        {
            try
            {
                hostedService.StopAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, string.Format(LoggerMessages.ExceptionWhenStoppingService, hostedService.GetType()));
            }
        }

        try
        {
            monoPlugin.OnDisable().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, string.Format(LoggerMessages.ExceptionWhenDisablingPlugin, plugin.PluginInfo.Name));
        }

        _loadedPlugins[plugin] = false;

        monoPlugin.DiscordClient?.DisconnectAsync();
        Logger.Info(string.Format(LoggerMessages.DisabledPlugin, plugin.PluginInfo.Name, plugin.PluginInfo.Version));
    }

    /// <inheritdoc />
    public void EnablePlugin(IPlugin plugin)
    {
        if (plugin is null) throw new ArgumentNullException(nameof(plugin));
        if (!_loadedPlugins.ContainsKey(plugin)) throw new PluginNotLoadedException(plugin);
        if (_loadedPlugins[plugin]) return;
        if (plugin is not MonoPlugin monoPlugin) return;

        foreach (IHostedService hostedService in plugin.ServiceProvider.GetServices<IHostedService>())
        {
            try
            {
                hostedService.StartAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, string.Format(LoggerMessages.ExceptionWhenStartingService, hostedService.GetType()));
            }
        }

        try
        {
            monoPlugin.OnEnable().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, string.Format(LoggerMessages.ExceptionWhenEnablingPlugin, plugin.PluginInfo.Name));
            return;
        }

        monoPlugin.DiscordClient?.ConnectAsync();

        _loadedPlugins[plugin] = true;
        Logger.Info(string.Format(LoggerMessages.EnabledPlugin, plugin.PluginInfo.Name, plugin.PluginInfo.Version));
    }

    /// <inheritdoc />
    public T? GetPlugin<T>() where T : IPlugin
    {
        IPlugin? plugin = _loadedPlugins.Keys.FirstOrDefault(p => p is T);
        if (plugin is T actual) return actual;
        return default;
    }

    /// <inheritdoc />
    public IPlugin? GetPlugin(string name)
    {
        return _loadedPlugins.Keys.FirstOrDefault(p => string.Equals(p.PluginInfo.Name, name, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public IPlugin LoadPlugin(string name)
    {
        if (_pluginLoadStack.Contains(name))
            throw new CircularPluginDependencyException(name);

        IPlugin? loadedPlugin =
            _loadedPlugins.Keys.FirstOrDefault(p => string.Equals(p.PluginInfo.Name, name, StringComparison.Ordinal));
        if (loadedPlugin is not null)
        {
            Logger.Debug(string.Format(LoggerMessages.PluginAlreadyLoaded, name));
            return loadedPlugin;
        }

        _pluginLoadStack.Push(name);

        string pluginFileName = Path.Combine(PluginDirectory.FullName, $"{name}.dll");
        if (!File.Exists(pluginFileName))
            throw new PluginNotFoundException(name);

        var dependencies = new List<IPlugin>();
        Assembly assembly = Assembly.LoadFile(pluginFileName); // DO NOT LoadFrom here. LoadFile does not load into domain
        if (_loadedAssemblies.Exists(a => a.Location == assembly.Location))
        {
            Logger.Debug(string.Format(LoggerMessages.AssemblyAlreadyLoaded, assembly));
            assembly = _loadedAssemblies.Find(a => a.Location == assembly.Location)!;
        }
        else
        {
            assembly = Assembly.LoadFrom(pluginFileName); // loads into domain
            _loadedAssemblies.Add(assembly);

            Logger.Debug(string.Format(LoggerMessages.LoadedNewAssembly, assembly));
        }

        Type[] pluginTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MonoPlugin))).ToArray();

        if (pluginTypes.Length == 0)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.NoPluginClass, typeof(MonoPlugin)));

        if (pluginTypes.Length > 1)
        {
            throw new InvalidPluginException(name,
                string.Format(ExceptionMessages.MultiplePluginsInAssembly, typeof(MonoPlugin)));
        }

        Type type = pluginTypes[0];

        var pluginAttribute = type.GetCustomAttribute<PluginAttribute>();
        if (pluginAttribute is null)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.NoPluginAttribute, typeof(PluginAttribute)));

        var dependenciesAttribute = type.GetCustomAttribute<PluginDependenciesAttribute>();
        if (dependenciesAttribute?.Dependencies is {Length: > 0} dependencyNames)
        {
            Logger.Debug(string.Format(LoggerMessages.PluginRequiresDependencies, pluginAttribute.Name, dependencyNames.Length,
                string.Join(", ", dependencyNames)));

            foreach (string dependencyName in dependencyNames)
            {
                try
                {
                    IPlugin dependency = LoadPlugin(dependencyName);
                    dependencies.Add(dependency);
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, string.Format(LoggerMessages.CouldNotLoadDependency, dependencyName));
                    return null!; // return value will be ignored anyway
                }
            }
        }

        string assemblyVersion = assembly.GetName().Version?.ToString(3) ?? pluginAttribute.Version;
        if (!string.Equals(pluginAttribute.Version, assemblyVersion))
        {
            Logger.Warn(string.Format(LoggerMessages.PluginVersionMismatch, pluginAttribute.Version, assemblyVersion,
                pluginAttribute.Name));
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

        if (Activator.CreateInstance(type) is not MonoPlugin plugin)
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


        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });

        serviceCollection.AddSingleton<IPluginManager>(this);
        serviceCollection.AddSingleton(plugin.GetType(), plugin);
        serviceCollection.AddSingleton(plugin.Configuration);

        var token = plugin.Configuration.Get<string>("discord.token");
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.Warn(string.Format(LoggerMessages.NoPluginToken, plugin.PluginInfo.Name));
        }
        else
        {
            var intents = DiscordIntents.AllUnprivileged;
            var intentsAttribute = type.GetCustomAttribute<PluginIntentsAttribute>();

            if (intentsAttribute is not null)
                intents = intentsAttribute.Intents;

            serviceCollection.AddSingleton(provider =>
            {
                var client = new DiscordClient(new DiscordConfiguration
                {
                    Intents = intents,
                    LoggerFactory = new NLogLoggerFactory(),
                    ServiceProvider = provider,
                    Token = token
                });

                plugin.DiscordClient = client;
                return client;
            });
        }

        plugin.ConfigureServices(serviceCollection);
        plugin.ServiceProvider = serviceCollection.BuildServiceProvider();

        plugin.OnLoad().GetAwaiter().GetResult();

        _loadedPlugins.Add(plugin, false);

        Logger.Info(string.Format(LoggerMessages.LoadedPlugin, plugin.PluginInfo.Name, plugin.PluginInfo.Version));
        _pluginLoadStack.Pop();
        return plugin;
    }

    /// <inheritdoc />
    public IReadOnlyList<IPlugin> LoadPlugins()
    {
        try
        {
            PluginDirectory.Create();
        }
        catch (IOException exception)
        {
            Logger.Warn(exception, string.Format(LoggerMessages.PluginDirectoryCantBeCreated, PluginDirectory.FullName));
            return ArraySegment<IPlugin>.Empty;
        }

        var plugins = new List<IPlugin>();

        foreach (FileInfo file in PluginDirectory.EnumerateFiles("*.dll"))
        {
            string pluginName = Path.GetFileNameWithoutExtension(file.Name);

            IPlugin? plugin = null;
            try
            {
                plugin = LoadPlugin(pluginName);
                if (plugin is not null)
                    plugins.Add(plugin);
            }
            catch (Exception exception)
            {
                if (plugin is MonoPlugin monoPlugin)
                {
                    monoPlugin.DiscordClient?.Dispose();
                    monoPlugin.DiscordClient = null;
                }

                plugin?.Dispose();
                Logger.Error(exception, string.Format(LoggerMessages.ExceptionWhenLoadingPlugin, pluginName));
            }
        }

        return plugins.AsReadOnly();
    }

    /// <inheritdoc />
    public void UnloadPlugin(IPlugin plugin)
    {
        if (plugin is not MonoPlugin monoPlugin) return;

        DisablePlugin(plugin);

        try
        {
            monoPlugin.OnUnload().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, string.Format(LoggerMessages.ExceptionWhenUnloadingPlugin, plugin.PluginInfo.Name));
        }

        monoPlugin.DiscordClient?.Dispose();
        monoPlugin.DiscordClient = null;
        plugin.Dispose();

        Logger.Info(string.Format(LoggerMessages.UnloadedPlugin, plugin.PluginInfo.Name, plugin.PluginInfo.Version));
        _loadedPlugins.Remove(plugin);
    }
}
