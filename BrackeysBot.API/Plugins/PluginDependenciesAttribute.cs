using System;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Specifies the dependencies that should be loaded prior to this <see cref="MonoPlugin" />, so that this plugin functions
///     correctly.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginDependenciesAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginDependenciesAttribute" /> class.
    /// </summary>
    /// <param name="firstDependency">The first dependency.</param>
    /// <param name="otherDependencies">The dependencies of this plugin.</param>
    public PluginDependenciesAttribute(string firstDependency, params string[] otherDependencies)
    {
        otherDependencies ??= Array.Empty<string>();

        var dependencies = new string[otherDependencies.Length + 1];
        dependencies[0] = firstDependency;
        Array.Copy(otherDependencies, 0, dependencies, 1, otherDependencies.Length);

        Dependencies = dependencies;
    }

    /// <summary>
    ///     Gets the dependencies of this plugin.
    /// </summary>
    /// <value>The dependencies.</value>
    public string[] Dependencies { get; }
}
