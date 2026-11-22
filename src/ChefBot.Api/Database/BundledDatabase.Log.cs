namespace ChefBot.Api.Database;

/// <summary>
/// The server's log file. Its warnings and failures are copied into ChefBot's own log, so there is one log to read;
/// errors are left out because ChefBot reports the ones that matter to it itself.
/// </summary>
public sealed partial class BundledDatabase
{
    /// <summary>A log bigger than this is moved to postgres.log.1 when the server next starts.</summary>
    private const long LogSizeLimit = 10 * 1024 * 1024;

    private CancellationTokenSource? forwarding;

    private void RotateLog()
    {
        var log = new FileInfo(LogFile);
        if (log is { Exists: true, Length: > LogSizeLimit })
        {
            log.MoveTo($"{LogFile}.1", overwrite: true);
        }
    }

    /// <summary>The last lines of the server's log, for a failure message.</summary>
    private string LogTail()
    {
        if (!File.Exists(LogFile))
        {
            return "(PostgreSQL wrote no log.)";
        }

        using var stream = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(Environment.NewLine, lines.TakeLast(15));
    }

    private void StartForwardingLog(ILogger logger)
    {
        forwarding = new CancellationTokenSource();
        var length = File.Exists(LogFile) ? new FileInfo(LogFile).Length : 0;
        _ = ForwardLogAsync(logger, length, forwarding.Token);
    }

    private void StopForwardingLog()
    {
        forwarding?.Cancel();
        forwarding?.Dispose();
        forwarding = null;
    }

    private async Task ForwardLogAsync(ILogger logger, long position, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!File.Exists(LogFile))
                {
                    continue;
                }

                await using var stream = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (stream.Length < position)
                {
                    position = 0;
                }

                stream.Seek(position, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    Forward(logger, line);
                }

                position = stream.Position;
            }
        }
        catch (OperationCanceledException)
        {
            // ChefBot is stopping.
        }
        catch (IOException exception)
        {
            logger.LogWarning("ChefBot stopped copying the bundled PostgreSQL's log ({LogFile}): {Reason}", LogFile, exception.Message);
        }
    }

    private static void Forward(ILogger logger, string line)
    {
        if (line.Contains(" FATAL: ", StringComparison.Ordinal) || line.Contains(" PANIC: ", StringComparison.Ordinal))
        {
            logger.LogError("PostgreSQL: {Line}", line);
        }
        else if (line.Contains(" WARNING: ", StringComparison.Ordinal))
        {
            logger.LogWarning("PostgreSQL: {Line}", line);
        }
    }
}
