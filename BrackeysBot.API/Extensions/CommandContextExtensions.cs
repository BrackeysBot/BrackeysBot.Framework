﻿using System.Threading.Tasks;
using DisCatSharp.CommandsNext;

namespace BrackeysBot.API.Extensions;

/// <summary>
///     Extension methods for <see cref="CommandContext" />.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    ///     Acknowledges the message provided by a <see cref="CommandContext" /> by reacting to it.
    /// </summary>
    /// <param name="context">The command context.</param>
    public static Task AcknowledgeAsync(this CommandContext context)
    {
        return context.Message.AcknowledgeAsync();
    }
}
