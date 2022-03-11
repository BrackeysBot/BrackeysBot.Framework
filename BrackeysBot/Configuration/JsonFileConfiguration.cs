using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BrackeysBot.API.Configuration;

namespace BrackeysBot.Configuration;

/// <summary>
///     Represents a class which implements JSON configuration as read from a config file.
/// </summary>
internal sealed class JsonFileConfiguration : IConfiguration
{
    private static readonly JsonWriterOptions JsonWriterOptions = new() {Indented = true};

    /// <summary>
    ///     Gets the configuration file for this object.
    /// </summary>
    /// <value>The configuration file.</value>
    public FileInfo ConfigurationFile { get; internal set; }

    /// <inheritdoc />
    public T? Get<T>(string propertyName, T? defaultValue = default)
    {
        using FileStream stream = ConfigurationFile.OpenRead();
        JsonNode configuration = JsonNode.Parse(stream) ?? new JsonObject();
        var buffer = new StringBuilder();
        var tokens = new List<string>();
        var escape = false;

        JsonNode? node = configuration;
        for (var index = 0; index < propertyName.Length; index++)
        {
            char current = propertyName[index];
            switch (current)
            {
                case '.' when !escape:
                    tokens.Add(buffer.ToString());
                    buffer.Clear();
                    break;

                case '\\' when escape:
                    buffer.Append('\\');
                    goto case '\\';

                case '\\':
                    escape = !escape;
                    break;

                default:
                    buffer.Append(current);
                    break;
            }
        }

        tokens.Add(buffer.ToString());
        buffer.Clear();

        for (var index = 0; index < tokens.Count - 1; index++)
        {
            string token = tokens[index];
            if (node[token] is null) return defaultValue;
            node = node[token];
            if (node is null) return defaultValue;
        }

        return node[tokens[^1]] is null ? defaultValue : node[tokens[^1]].Deserialize<T>();
    }

    /// <inheritdoc />
    public void Set<T>(string propertyName, T? value)
    {
        JsonNode configuration;
        using (FileStream stream = ConfigurationFile.OpenRead())
        {
            configuration = JsonNode.Parse(stream) ?? new JsonObject();
            var buffer = new StringBuilder();
            var tokens = new List<string>();
            var escape = false;

            JsonNode node = configuration;
            for (var index = 0; index < propertyName.Length; index++)
            {
                char current = propertyName[index];
                switch (current)
                {
                    case '.' when !escape:
                        tokens.Add(buffer.ToString());
                        buffer.Clear();
                        break;

                    case '\\' when escape:
                        buffer.Append('\\');
                        goto case '\\';

                    case '\\':
                        escape = !escape;
                        break;

                    default:
                        buffer.Append(current);
                        break;
                }
            }

            tokens.Add(buffer.ToString());
            buffer.Clear();

            for (var index = 0; index < tokens.Count - 1; index++)
            {
                string token = tokens[index];
                node = node[token] ??= new JsonObject();
            }

            node[tokens[^1]] = GetValue(value);
        }

        using (FileStream stream = ConfigurationFile.Create())
        {
            using var writer = new Utf8JsonWriter(stream, JsonWriterOptions);
            configuration.WriteTo(writer, new JsonSerializerOptions {WriteIndented = true});
        }
    }

    /// <inheritdoc />
    public void SaveDefault()
    {
        if (!ConfigurationFile.Exists)
        {
            using FileStream stream = ConfigurationFile.Create();
            JsonDocument emptyDocument = JsonDocument.Parse("{}");
            JsonSerializer.Serialize(stream, emptyDocument);
        }

        JsonDocument configuration;

        using (FileStream stream = ConfigurationFile.OpenRead())
        {
            try
            {
                configuration = JsonDocument.Parse(stream);
            }
            catch (JsonException)
            {
                return;
            }
        }

        Assembly assembly = GetType().Assembly;
        using Stream? configStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{ConfigurationFile.Name}");
        if (configStream is null) return;

        JsonDocument defaultConfiguration = JsonDocument.Parse(configStream);
        JsonElement defaultRootElement = defaultConfiguration.RootElement;
        if (defaultRootElement.ValueKind != JsonValueKind.Object) return;

        using (FileStream stream = ConfigurationFile.Create())
        {
            using var writer = new Utf8JsonWriter(stream, JsonWriterOptions);

            writer.WriteStartObject();
            foreach (JsonProperty property in defaultConfiguration.RootElement.EnumerateObject())
                WriteObject(property, defaultRootElement, writer);
            foreach (JsonProperty property in configuration.RootElement.EnumerateObject())
                WriteObject(property, configuration.RootElement, writer);

            writer.WriteEndObject();
            configuration.WriteTo(writer);
        }
    }

    private static JsonValue? GetValue<T>(T? value)
    {
        return value switch
        {
            string s => JsonValue.Create(s),
            char c => JsonValue.Create(c.ToString()),
            byte b => JsonValue.Create(b),
            sbyte b => JsonValue.Create(b),
            short s => JsonValue.Create(s),
            ushort s => JsonValue.Create(s),
            int i => JsonValue.Create(i),
            uint i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            ulong l => JsonValue.Create(l),
            float f => JsonValue.Create(f),
            double d => JsonValue.Create(d),
            decimal d => JsonValue.Create(d),
            bool b => JsonValue.Create(b),
            null => null,
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static void WriteObject(JsonProperty property, JsonElement comparisonRoot, Utf8JsonWriter writer)
    {
        if (comparisonRoot.TryGetProperty(property.Name, out _))
        {
            if (property.Value.ValueKind != JsonValueKind.Object) return;
            foreach (JsonProperty childProperty in property.Value.EnumerateObject())
                WriteObject(childProperty, comparisonRoot, writer);
        }
        else
            property.WriteTo(writer);
    }
}
