namespace Service.Common.Monitoring;

public static class Monitor
{
    private static readonly TimeSpan s_sentryFlushTime = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     Sentry Dsn if discovered via configuration.
    /// </summary>
    private static string? SentryDsn { get; set; }

    /// <summary>
    ///     Runs and monitors the <paramref name="runTask" />.
    /// </summary>
    /// <param name="args">Args to be passed to the task</param>
    /// <param name="runTask">Task to monitor</param>
    /// <param name="onExit">Invoked when the task complete (whether or not it was successful)</param>
    public static async Task Run(
        string[] args, Func<string[], Task> runTask,
        Func<Exception?, ValueTask>? onExit = null)
    {
        onExit ??= _ => ValueTask.CompletedTask;

        try
        {
            await runTask(args);
        }
        catch (Exception e)
        {
            await onExit(e);

            using var _ = !SentrySdk.IsEnabled ? SentrySdk.Init(SentryDsn ?? FindSentryDsn()) : null;
            SentrySdk.CaptureException(e);
            await SentrySdk.FlushAsync(s_sentryFlushTime);

            throw;
        }

        // Not using finally, because apparently that doesn't always run if exception is unhandled.
        // See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/try-finally
        await onExit(null);
        await SentrySdk.FlushAsync(s_sentryFlushTime);
    }

    private static string? FindSentryDsn()
    {
        var knownNames = new[] { "SentryDsn", "SENTRYDSN", "Sentry__Dsn", "SENTRY__DSN", "Sentry_Dsn", "SENTRY_DSN" };

        return knownNames
            .Select(Environment.GetEnvironmentVariable)
            .FirstOrDefault(envValue => envValue is not null);
    }
}
