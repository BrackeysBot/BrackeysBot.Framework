namespace BrackeysBot.API.Exceptions;

/// <summary>
///     The exception that is thrown when an attempt to load a named plugin failed because the file does not exist.
/// </summary>
public sealed class PluginNotFoundException : FileNotFoundException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginNotFoundException" /> class.
    /// </summary>
    /// <param name="pluginName">The name of the plugin.</param>
    internal PluginNotFoundException(string pluginName)
        : base($"The plugin with the name {pluginName} could not be found", $"{pluginName}.dll")
    {
        PluginName = pluginName;
    }

    /// <summary>
    ///     Gets the name of the plugin which was not found.
    /// </summary>
    /// <value>The plugin name.</value>
    public string PluginName { get; }
}
