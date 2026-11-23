using Microsoft.Extensions.Logging;

namespace SwiftCollab.Bench.Harness;

/// <summary>
/// A logger that formats every message and writes it to <see cref="TextWriter.Null"/>. In the Part 4 bench
/// the original's console output is redirected to the same null writer, so both versions pay for building
/// and writing their messages and neither pays for a terminal.
/// </summary>
public sealed class NullWriterLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        TextWriter.Null.WriteLine(formatter(state, exception));
}
