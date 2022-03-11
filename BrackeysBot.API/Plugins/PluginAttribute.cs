using System;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Specifies <see cref="Plugin" /> information such as the plugin's name and version.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginAttribute" /> class.
    /// </summary>
    /// <param name="name">The name of this plugin.</param>
    /// <param name="version">The version of this plugin.</param>
    public PluginAttribute(string name, string version)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <summary>
    ///     Gets the name of the plugin.
    /// </summary>
    /// <value>The name of the plugin.</value>
    public string Name { get; }

    /// <summary>
    ///     Gets the version of this plugin.
    /// </summary>
    /// <value>The version of this plugin.</value>
    public string Version { get; }
}
