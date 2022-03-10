namespace BrackeysBot.API.Plugins;

/// <summary>
///     Represents a class which contains deserialized data from a <c>plugin.json</c> file.
/// </summary>
public class PluginInfo
{
    internal PluginInfo(string name, string version, string description, PluginAuthorInfo? author, PluginInfo[] dependencies)
    {
        Name = name;
        Version = version;
        Description = description;
        Author = author;
        Dependencies = dependencies;
    }

    /// <summary>
    ///     Gets the author of this plugin.
    /// </summary>
    /// <value>The plugin author, or <see langword="null" /> if no author is specified.</value>
    public PluginAuthorInfo? Author { get; }

    /// <summary>
    ///     Gets the dependencies of this plugin.
    /// </summary>
    /// <value>The plugin dependencies.</value>
    public PluginInfo[] Dependencies { get; }

    /// <summary>
    ///     Gets the description of this plugin.
    /// </summary>
    /// <value>The plugin description.</value>
    public string Description { get; }

    /// <summary>
    ///     Gets the name of this plugin.
    /// </summary>
    /// <value>The plugin name.</value>
    public string Name { get; }

    /// <summary>
    ///     Gets the version of this plugin.
    /// </summary>
    /// <value>The plugin version.</value>
    public string Version { get; }

    /// <summary>
    ///     Represents a class which contains deserialized data from the <c>author</c> property of a <c>plugin.json</c> file.
    /// </summary>
    public class PluginAuthorInfo
    {
        internal PluginAuthorInfo(string name, string? email, string? url)
        {
            Email = email;
            Name = name;
            Url = url;
        }

        /// <summary>
        ///     Gets the email address of the author.
        /// </summary>
        /// <value>The author's email address.</value>
        public string? Email { get; }

        /// <summary>
        ///     Gets the name of the author.
        /// </summary>
        /// <value>The author's name.</value>
        public string Name { get; }

        /// <summary>
        ///     Gets the URL of the author.
        /// </summary>
        /// <value>The author's URL.</value>
        public string? Url { get; }
    }
}
