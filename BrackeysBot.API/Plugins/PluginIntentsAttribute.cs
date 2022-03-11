using System;
using DisCatSharp;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Specifies the Discord client intents that this plugin will utilise.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginIntentsAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginIntentsAttribute" /> class.
    /// </summary>
    /// <param name="intents">The intents.</param>
    public PluginIntentsAttribute(DiscordIntents intents)
    {
        Intents = intents;
    }

    /// <summary>
    ///     Gets the intents.
    /// </summary>
    /// <value>The intents.</value>
    public DiscordIntents Intents { get; }
}
