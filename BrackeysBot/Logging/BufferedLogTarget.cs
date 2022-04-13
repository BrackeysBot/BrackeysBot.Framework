using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using BrackeysBot.API.Logging;
using NLog;
using NLog.Targets;

namespace BrackeysBot.Logging;

/// <summary>
///     Represents an NLog target which writes its output to a buffer stream, and raises an event at a specified interval.
/// </summary>
internal sealed class BufferedLogTarget : TargetWithLayout
{
    private readonly MemoryStream _bufferedLogStream = new();
    private readonly Timer _timer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="BufferedLogTarget" /> class.
    /// </summary>
    /// <param name="name">The name of the log target.</param>
    /// <param name="pollInterval">The buffered log event interval.</param>
    public BufferedLogTarget(string name, TimeSpan pollInterval)
    {
        Name = name;

        _timer.Interval = pollInterval.TotalMilliseconds;
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    /// <summary>
    ///     Occurs when the target has flushed the buffered log events.
    /// </summary>
    public event EventHandler<BufferedLogEventArgs>? BufferedLog;

    /// <inheritdoc />
    protected override void Write(LogEventInfo logEvent)
    {
        using var writer = new StreamWriter(_bufferedLogStream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Layout.Render(logEvent));

        if (logEvent.Exception is { } exception)
            writer.Write($": {exception}");

        base.Write(logEvent);
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var buffer = new List<string>();
        using var reader = new StreamReader(_bufferedLogStream, Encoding.UTF8, leaveOpen: true);

        while (reader.ReadLine() is { } line)
            buffer.Add(line);

        _bufferedLogStream.SetLength(0);
        _bufferedLogStream.Position = 0;

        // we call ToArray() to prevent event subscribers from casting the IEnumerable<string> to List<string>,
        // allowing them to mutate it and effecting other subscribers.
        BufferedLog?.Invoke(this, new BufferedLogEventArgs(buffer.ToArray()));
    }
}
