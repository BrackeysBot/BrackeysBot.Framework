namespace BrackeysBot.API.Configuration;

/// <summary>
///     Represents a configuration object.
/// </summary>
public interface IConfiguration
{
    /// <summary>
    ///     Gets the configuration file for this object.
    /// </summary>
    /// <value>The configuration file.</value>
    FileInfo ConfigurationFile { get; }

    /// <summary>
    ///     Gets a configuration value and attempts to convert it to a specified type.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="propertyName" /> was not found.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The configuration value, or <paramref name="defaultValue" /> if the property name was not found.</returns>
    /// <remarks>Nested types are supported using a period (<c>.</c>) to specify child property names.</remarks>
    T? Get<T>(string propertyName, T? defaultValue = default);

    /// <summary>
    ///     Sets a configuration value, creating the property if it does not exist.
    /// </summary>
    /// <param name="propertyName">The name of the property to create or modify.</param>
    /// <param name="value">The value to save.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <remarks>Nested types are supported using a period (<c>.</c>) to specify child property names.</remarks>
    void Set<T>(string propertyName, T? value);

    /// <summary>
    ///     Saves the default configuration for this object, merging the current configuration (if any) with the default. Existing
    ///     keys are not overwritten.
    /// </summary>
    void SaveDefault();
}
