namespace BrackeysBot.API.Configuration;

/// <summary>
///     Represents an object which can hold mutable configuration.
/// </summary>
public interface IConfigurationHolder
{
    /// <summary>
    ///     Gets the configuration for this object.
    /// </summary>
    IConfiguration Configuration { get; }
}
