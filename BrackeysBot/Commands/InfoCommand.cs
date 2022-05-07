using System;
using System.Text;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.API.Plugins;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace BrackeysBot.Commands;

/// <summary>
///     Represents a class which implements the <c>info</c> command. The <c>info</c> command requires the mention prefix to
///     discern which bot details are being requested.
/// </summary>
internal sealed class InfoCommand : ApplicationCommandModule
{
    private readonly IPlugin _plugin;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfoCommand" /> class.
    /// </summary>
    /// <param name="plugin">The owning plugin.</param>
    public InfoCommand(IPlugin plugin)
    {
        _plugin = plugin;
    }

    [SlashCommand("info", "Displays information about the bot.")]
    [SlashRequireGuild]
    public async Task InfoCommandAsync(InteractionContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        DiscordColor color = 0;
        DiscordGuild guild = context.Guild;
        DiscordUser currentUser = context.Client.CurrentUser;

        if (guild.Members.TryGetValue(currentUser.Id, out DiscordMember? member))
            color = member.Color;

        if (color.Value == 0)
            color = 0x3F51B5;

        var embed = new DiscordEmbedBuilder();
        embed.WithFooter(guild.Name, guild.IconUrl);
        embed.WithThumbnail(currentUser.GetAvatarUrl(ImageFormat.Png));
        embed.WithColor(color);

        string prefix = _plugin.Configuration.Get<string>("discord.prefix") ?? "[]";

        embed.AddField(Formatter.Underline("Ping"), context.Client.Ping, true);
        embed.AddField(Formatter.Underline("Prefix"), prefix, true);
        embed.AddFieldIf(_plugin.EnableTime.HasValue, Formatter.Underline("Enabled"),
            () => Formatter.Timestamp(_plugin.EnableTime!.Value), true);

        var builder = new StringBuilder();
        builder.AppendLine($"BrackeysBot: {BrackeysBotApp.Version}");
        builder.AppendLine($"API: {BrackeysBotApp.ApiVersion}");
        builder.AppendLine($"{_plugin.PluginInfo.Name}: {_plugin.PluginInfo.Version}");
        builder.AppendLine($"D#+: {context.Client.VersionString}");
        builder.AppendLine($"CLR: {Environment.Version}");
        builder.AppendLine($"Host: {Environment.OSVersion}");

        embed.AddField(Formatter.Underline("Version"), Formatter.BlockCode(builder.ToString().Trim()));
        ;
        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
