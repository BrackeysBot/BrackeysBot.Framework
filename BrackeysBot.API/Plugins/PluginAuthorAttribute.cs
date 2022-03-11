using System;

namespace BrackeysBot.API.Plugins;

/// <summary>
///     Specifies the author of a <see cref="Plugin" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginAuthorAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginDependenciesAttribute" /> class.
    /// </summary>
    /// <param name="name">The name of the author.</param>
    /// <param name="email">Optional. The author's email address. Default is <see langword="null" />.</param>
    /// <param name="url">Optional. The author's homepage URL. Default is <see langword="null" />.</param>
    public PluginAuthorAttribute(string name, string? email = null, string? url = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email;
        Url = url;
    }

    /// <summary>
    ///     Gets the name of the author.
    /// </summary>
    /// <value>The author's name.</value>
    public string Name { get; }

    /// <summary>
    ///     Gets the email address of the author.
    /// </summary>
    /// <value>The author's email address, or <see langword="null" /> if not this value is not specified.</value>
    public string? Email { get; }

    /// <summary>
    ///     Gets the homepage URL of the author.
    /// </summary>
    /// <value>The author's homepage URL, or <see langword="null" /> if not this value is not specified.</value>
    public string? Url { get; }
}
