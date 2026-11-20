namespace ChefBot.Api.Logging;

/// <summary>Sends every log entry that passes the Logging:LogLevel filters to the <see cref="LogBuffer"/> too.</summary>
[ProviderAlias("Buffer")]
public sealed class LogBufferProvider(LogBuffer buffer, TimeProvider clock) : ILoggerProvider
{
    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new LogBufferLogger(buffer, clock, categoryName);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
