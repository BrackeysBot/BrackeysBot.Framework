using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;

namespace BrackeysBot.API.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordMember" />.
/// </summary>
public static class DiscordMemberExtensions
{
    /// <summary>
    ///     Normalizes a <see cref="DiscordMember" /> so that the internal client is assured to be a specified value.
    /// </summary>
    /// <param name="member">The <see cref="DiscordMember" /> to normalize.</param>
    /// <param name="client">The target client.</param>
    /// <returns>
    ///     A <see cref="DiscordMember" /> whose public values will match <paramref name="member" />, but whose internal client
    ///     is <paramref name="client" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="client" /> is <see langword="null" /></para>
    /// </exception>
    public static async Task<DiscordMember> NormalizeClientAsync(this DiscordMember member, DiscordClient client)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (client is null) throw new ArgumentNullException(nameof(client));

        DiscordGuild guild = await member.Guild.NormalizeClientAsync(client);
        return await guild.GetMemberAsync(member.Id);
    }
}
