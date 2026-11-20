namespace ChefBot.Api.Logging;

/// <summary>Writes one category's log entries to the <see cref="LogBuffer"/>.</summary>
internal sealed class LogBufferLogger(LogBuffer buffer, TimeProvider clock, string category) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        buffer.Add(clock.GetUtcNow(), logLevel.ToString(), category, formatter(state, exception), exception?.ToString());
    }
}
