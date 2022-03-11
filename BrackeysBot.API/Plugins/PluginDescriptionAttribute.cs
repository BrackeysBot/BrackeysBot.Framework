using System;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Specifies the description of a <see cref="Plugin" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginDescriptionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginDependenciesAttribute" /> class.
    /// </summary>
    /// <param name="description">The description of this plugin.</param>
    public PluginDescriptionAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    ///     Gets the description of this plugin.
    /// </summary>
    /// <value>The description.</value>
    public string Description { get; }
}
