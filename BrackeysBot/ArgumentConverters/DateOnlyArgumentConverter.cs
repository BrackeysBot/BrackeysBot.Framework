using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace BrackeysBot.ArgumentConverters;

/// <summary>
///     Represents a converter which converts a command argument to <see cref="DateOnly" />.
/// </summary>
internal sealed class DateOnlyArgumentConverter : IArgumentConverter<DateOnly>
{
    /// <inheritdoc />
    public Task<Optional<DateOnly>> ConvertAsync(string value, CommandContext ctx)
    {
        return Task.FromResult(DateTime.TryParse(value, out DateTime date)
            ? new Optional<DateOnly>(DateOnly.FromDateTime(date))
            : new Optional<DateOnly>());
    }
}
