using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace BrackeysBot.ArgumentConverters;

/// <summary>
///     Represents a converter which converts a command argument to <see cref="TimeOnly" />.
/// </summary>
internal sealed class TimeOnlyArgumentConverter : IArgumentConverter<TimeOnly>
{
    /// <inheritdoc />
    public Task<Optional<TimeOnly>> ConvertAsync(string value, CommandContext ctx)
    {
        return Task.FromResult(TimeOnly.TryParse(value, out TimeOnly time)
            ? new Optional<TimeOnly>(time)
            : new Optional<TimeOnly>());
    }
}
