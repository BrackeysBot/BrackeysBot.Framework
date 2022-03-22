using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;

namespace BrackeysBot.API.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordChannel" />.
/// </summary>
public static class DiscordChannelExtensions
{
    /// <summary>
    ///     Normalizes a <see cref="DiscordChannel" /> so that the internal client is assured to be a specified value.
    /// </summary>
    /// <param name="channel">The <see cref="DiscordChannel" /> to normalize.</param>
    /// <param name="client">The target client.</param>
    /// <returns>
    ///     A <see cref="DiscordChannel" /> whose public values will match <paramref name="channel" />, but whose internal client
    ///     is <paramref name="client" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="channel" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="client" /> is <see langword="null" /></para>
    /// </exception>
    public static async Task<DiscordChannel> NormalizeClientAsync(this DiscordChannel channel, DiscordClient client)
    {
        if (channel is null) throw new ArgumentNullException(nameof(channel));
        if (client is null) throw new ArgumentNullException(nameof(client));

        return await client.GetChannelAsync(channel.Id);
    }
}
