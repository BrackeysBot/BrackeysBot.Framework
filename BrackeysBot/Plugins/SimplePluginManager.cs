using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API;
using BrackeysBot.API.Exceptions;
using BrackeysBot.API.Plugins;
using BrackeysBot.ArgumentConverters;
using BrackeysBot.Commands;
using BrackeysBot.Configuration;
using BrackeysBot.Logging;
using BrackeysBot.Resources;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Converters;
using DisCatSharp.CommandsNext.Exceptions;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
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
    private readonly Dictionary<IPlugin, List<string>> _commands = new();
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

        monoPlugin.EnableTime = null;
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

        monoPlugin.EnableTime = DateTimeOffset.UtcNow;

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
        foreach ((IPlugin? plugin, bool _) in _loadedPlugins)
        {
            if (plugin is T actual)
                return actual;
        }

        return default;
    }

    /// <inheritdoc />
    public IPlugin? GetPlugin(string name)
    {
        foreach ((IPlugin? plugin, bool _) in _loadedPlugins)
        {
            if (string.Equals(name, plugin.PluginInfo.Name))
                return plugin;
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsPluginEnabled(IPlugin plugin)
    {
        return _loadedPlugins.TryGetValue(plugin, out bool enabled) && enabled;
    }

    /// <inheritdoc />
    public bool IsPluginLoaded(IPlugin plugin)
    {
        return _loadedPlugins.ContainsKey(plugin);
    }

    /// <inheritdoc />
    public IPlugin LoadPlugin(string name)
    {
        if (_pluginLoadStack.Contains(name))
            throw new CircularPluginDependencyException(name);

        if (TryGetPlugin(name, out IPlugin? loadedPlugin))
        {
            Logger.Debug(string.Format(LoggerMessages.PluginAlreadyLoaded, name));
            return loadedPlugin;
        }

        _pluginLoadStack.Push(name);

        var file = new FileInfo(Path.Combine(PluginDirectory.FullName, $"{name}.dll"));
        if (!file.Exists) throw new PluginNotFoundException(name);

        var context = new AssemblyLoadContext(name, true);
        using FileStream stream = file.Open(FileMode.Open, FileAccess.Read);
        Assembly assembly = context.LoadFromStream(stream);

        Type pluginType = GetPluginType(name, assembly, out PluginAttribute? pluginAttribute);
        name = pluginAttribute.Name;

        if (_loadedPlugins.Any(p => string.Equals(name, p.Key.PluginInfo.Name)))
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.DuplicatePluginName, name));

        string version = pluginAttribute.Version;

        string assemblyVersion = assembly.GetName().Version?.ToString(3) ?? version;
        if (!string.Equals(pluginAttribute.Version, assemblyVersion))
        {
            Logger.Warn(string.Format(LoggerMessages.PluginVersionMismatch, pluginAttribute.Version, assemblyVersion,
                pluginAttribute.Name));
        }

        var descriptionAttribute = pluginType.GetCustomAttribute<PluginDescriptionAttribute>();
        string? description = descriptionAttribute?.Description;

        IPlugin[] dependencies = EnumeratePluginDependencies(pluginType).ToArray();
        PluginInfo.PluginAuthorInfo? authorInfo = GetPluginAuthorInfo(pluginType);
        var pluginInfo = new PluginInfo(name, version, description, authorInfo, dependencies.Select(d => d.PluginInfo).ToArray());
        if (Activator.CreateInstance(pluginType) is not MonoPlugin instance)
            throw new InvalidPluginException(name, ExceptionMessages.NoDerivationOfPluginClass);

        instance.Dependencies = dependencies.ToArray(); // defensive copy
        instance.LoadContext = context;
        instance.PluginInfo = pluginInfo;
        instance.PluginManager = this;
        instance.Logger = LogManager.GetLogger(pluginInfo.Name);

        UpdatePluginDependants(instance);
        SetupPluginDataDirectory(pluginInfo, instance);
        SetupPluginConfiguration(instance);
        SetupPluginServices(instance, pluginInfo, pluginType);
        CommandsNextExtension? commandsNext = SetupPluginCommands(instance);

        instance.OnLoad().GetAwaiter().GetResult();

        if (commandsNext is not null)
        {
            commandsNext.UnregisterConverter<TimeSpanConverter>();
            commandsNext.RegisterConverter(new TimeSpanArgumentConverter());

            string[] commandNames = commandsNext.RegisteredCommands.Keys.OrderBy(c => c).ToArray();
            Logger.Info(string.Format(LoggerMessages.PluginRegisteredCommands, pluginInfo.Name, commandNames.Length,
                string.Join(", ", commandNames)));

            CheckDuplicateCommands(instance, commandNames);
            RegisterCommandEvents(instance, commandsNext);

            _commands.Add(instance, commandNames.ToList());
        }

        Logger.Info(string.Format(LoggerMessages.LoadedPlugin, pluginInfo.Name, pluginInfo.Version));

        _pluginLoadStack.Pop();
        _loadedPlugins.Add(instance, false);
        return instance;
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

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
    public bool TryGetPlugin(string name, [NotNullWhen(true)] out IPlugin? plugin)
    {
        plugin = _loadedPlugins.Keys.FirstOrDefault(p => string.Equals(name, p.PluginInfo.Name));
        return plugin is not null;
    }

    /// <inheritdoc />
    public void UnloadPlugin(IPlugin plugin)
    {
        if (!IsPluginLoaded(plugin)) return;
        if (plugin is not MonoPlugin monoPlugin) return;

        DisablePlugin(plugin);

        foreach (IPlugin dependant in plugin.Dependants)
            UnloadPlugin(dependant);

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

        monoPlugin.LoadContext.Unload();
        _commands.Remove(plugin);
        _loadedPlugins.Remove(plugin);

        Logger.Info(string.Format(LoggerMessages.UnloadedPlugin, plugin.PluginInfo.Name, plugin.PluginInfo.Version));
    }

    private static Task ClientOnMessageCreated(MonoPlugin plugin, DiscordClient sender, MessageCreateEventArgs e)
    {
        CommandsNextExtension? commandsNext = sender.GetCommandsNext();
        if (commandsNext is null) return Task.CompletedTask;

        DiscordMessage? message = e.Message;
        if (message.Content is not {Length: > 0} content) return Task.CompletedTask;

        string prefix = plugin.Configuration.Get<string>("discord.prefix") ?? "[]";
        int commandStart = message.GetStringPrefixLength(MentionUtility.MentionUser(sender.CurrentUser.Id, false) + ' ');
        if (commandStart == -1)
        {
            commandStart = message.GetStringPrefixLength(MentionUtility.MentionUser(sender.CurrentUser.Id) + ' ');
            if (commandStart == -1)
            {
                commandStart = message.GetStringPrefixLength(prefix);
                if (commandStart == -1) return Task.CompletedTask;
            }
        }

        prefix = content[..commandStart];
        string commandString = content[commandStart..];

        Command? command = commandsNext.FindCommand(commandString, out string? args);
        if (command is null) return Task.CompletedTask;

        CommandContext context = commandsNext.CreateContext(message, prefix, command, args);
        Task.Run(async () => await commandsNext.ExecuteCommandAsync(context));
        return Task.CompletedTask;
    }

    private static Type GetPluginType(string name, Assembly assembly, out PluginAttribute pluginAttribute)
    {
        Type[] pluginTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MonoPlugin))).ToArray();
        if (pluginTypes.Length == 0)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.NoPluginClass, typeof(MonoPlugin)));

        if (pluginTypes.Length > 1)
        {
            throw new InvalidPluginException(name,
                string.Format(ExceptionMessages.MultiplePluginsInAssembly, typeof(MonoPlugin)));
        }

        Type pluginType = pluginTypes[0];
        pluginAttribute = pluginType.GetCustomAttribute<PluginAttribute>()!;
        if (pluginAttribute is null)
            throw new InvalidPluginException(name, string.Format(ExceptionMessages.NoPluginAttribute, typeof(PluginAttribute)));

        return pluginType;
    }

    private IEnumerable<IPlugin> EnumeratePluginDependencies(Type pluginType)
    {
        var pluginDependenciesAttribute = pluginType.GetCustomAttribute<PluginDependenciesAttribute>();
        if (pluginDependenciesAttribute?.Dependencies is not {Length: > 0} requestedDependencies)
            yield break;

        foreach (string requestedDependency in requestedDependencies)
        {
            IPlugin? dependency;

            try
            {
                dependency = LoadPlugin(requestedDependency);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, string.Format(LoggerMessages.CouldNotLoadDependency, requestedDependency));
                dependency = null;
            }

            if (dependency is not null)
                yield return dependency;
        }
    }

    private static PluginInfo.PluginAuthorInfo? GetPluginAuthorInfo(Type pluginType)
    {
        var authorAttribute = pluginType.GetCustomAttribute<PluginAuthorAttribute>();
        if (authorAttribute is null)
            return null;

        return new PluginInfo.PluginAuthorInfo(authorAttribute.Name, authorAttribute.Email, authorAttribute.Url);
    }

    private static CommandsNextExtension? SetupPluginCommands(MonoPlugin plugin)
    {
        if (plugin.ServiceProvider.GetService<DiscordClient>() is not { } client) return null;

        client.MessageCreated += (sender, e) => ClientOnMessageCreated(plugin, sender, e);

        CommandsNextExtension? commandsNext = client.UseCommandsNext(new CommandsNextConfiguration
        {
            ServiceProvider = plugin.ServiceProvider,
            UseDefaultCommandHandler = false
        });

        commandsNext.RegisterCommands<InfoCommand>();
        return commandsNext;
    }

    private void SetupPluginDataDirectory(PluginInfo pluginInfo, MonoPlugin instance)
    {
        var dataDirectory = new DirectoryInfo(Path.Combine(PluginDirectory.FullName, pluginInfo.Name));
        dataDirectory.Create();
        instance.DataDirectory = dataDirectory;
    }

    private static void SetupPluginConfiguration(MonoPlugin instance)
    {
        var jsonFileConfiguration = new JsonFileConfiguration();
        string configFilePath = Path.Combine(instance.DataDirectory.FullName, "config.json");
        jsonFileConfiguration.ConfigurationFile = new FileInfo(configFilePath);
        jsonFileConfiguration.SaveDefault();
        instance.Configuration = jsonFileConfiguration;
    }

    private void CheckDuplicateCommands(IPlugin plugin, IReadOnlyCollection<string> commands)
    {
        string pluginName = plugin.PluginInfo.Name;

        foreach ((IPlugin current, List<string> currentCommands) in _commands)
        {
            string currentPluginName = current.PluginInfo.Name;

            foreach (string command in commands)
            {
                if (currentCommands.Contains(command))
                    Logger.Warn(string.Format(LoggerMessages.PluginCommandConflict, pluginName, command, currentPluginName));
            }
        }
    }

    private static void RegisterCommandEvents(IPlugin plugin, CommandsNextExtension commandsNext)
    {
        commandsNext.CommandErrored += (_, args) =>
        {
            CommandContext context = args.Context;
            if (context?.Command is null) return Task.CompletedTask;
            if (args.Exception is ChecksFailedException) return Task.CompletedTask; // no need to log ChecksFailedException

            var commandName = $"{context.Prefix}{context.Command.Name}";
            plugin.Logger.Error(args.Exception, $"An exception was thrown when executing {commandName}");
            return Task.CompletedTask;
        };
    }

    private void SetupPluginServices(MonoPlugin instance, PluginInfo pluginInfo, Type pluginType)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });

        serviceCollection.AddSingleton<IPluginManager>(this);
        serviceCollection.AddSingleton<IPlugin>(instance);
        serviceCollection.AddSingleton(instance.GetType(), instance);
        serviceCollection.AddSingleton(instance.Configuration);

        var token = instance.Configuration.Get<string>("discord.token");
        if (string.IsNullOrWhiteSpace(token))
            Logger.Warn(string.Format(LoggerMessages.NoPluginToken, pluginInfo.Name));
        else
        {
            var intents = DiscordIntents.AllUnprivileged;
            var intentsAttribute = pluginType.GetCustomAttribute<PluginIntentsAttribute>();

            if (intentsAttribute is not null)
                intents = intentsAttribute.Intents;

            serviceCollection.AddSingleton(provider =>
            {
                var client = new DiscordClient(new DiscordConfiguration
                {
                    Intents = intents,
                    LoggerFactory = new PluginLoggerFactory(instance),
                    ServiceProvider = provider,
                    Token = token
                });

                instance.DiscordClient = client;
                return client;
            });
        }

        instance.ConfigureServices(serviceCollection);
        instance.ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private static void UpdatePluginDependants(IPlugin instance)
    {
        foreach (MonoPlugin dependency in instance.Dependencies.OfType<MonoPlugin>())
        {
            if (dependency.Dependants.Contains(instance)) continue;

            var dependants = new List<IPlugin>(dependency.Dependants) {instance};
            dependency.Dependants = dependants.ToArray();
        }
    }
}
