using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;

namespace CookDating.BddTests.Support;

/// <summary>
/// Collects log entries from Aspire resource log streams and allows
/// querying for unexpected errors/warnings after each scenario.
/// </summary>
public sealed class LogCollector : IAsyncDisposable
{
    private readonly ConcurrentBag<LogEntry> _entries = new();
    private readonly List<CancellationTokenSource> _watchers = new();
    private int _scenarioStartIndex;

    /// <summary>
    /// Known warning/error patterns that are expected in the floci
    /// emulator environment and should not cause test failures.
    /// </summary>
    private static readonly string[] ExpectedPatterns =
    [
        "AdminConfirmSignUp not available",
        "Cognito auth unavailable",
        "Cognito token acquisition failed",
        "AWS bootstrapping",
        "Queue not found",
        "not found (attempt",
        "CORS policy execution"
    ];

    /// <summary>
    /// Begins watching log streams for the given resource names.
    /// Call once after the Aspire app has started.
    /// </summary>
    public void StartWatching(ResourceLoggerService loggerService, params string[] resourceNames)
    {
        foreach (var name in resourceNames)
        {
            var cts = new CancellationTokenSource();
            _watchers.Add(cts);
            _ = WatchResourceAsync(loggerService, name, cts.Token);
        }
    }

    private async Task WatchResourceAsync(
        ResourceLoggerService loggerService, string resourceName, CancellationToken ct)
    {
        try
        {
            await foreach (var batch in loggerService.WatchAsync(resourceName).WithCancellation(ct))
            {
                foreach (var line in batch)
                {
                    _entries.Add(new LogEntry(resourceName, line.Content, line.IsErrorMessage));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }

    /// <summary>
    /// Marks the start of a new scenario so only logs from this
    /// point forward are checked for unexpected errors.
    /// </summary>
    public void MarkScenarioStart()
    {
        _scenarioStartIndex = _entries.Count;
    }

    /// <summary>
    /// Returns error log entries from the current scenario that
    /// don't match any expected patterns.
    /// </summary>
    public IReadOnlyList<LogEntry> GetUnexpectedErrors()
    {
        return _entries
            .Skip(_scenarioStartIndex)
            .Where(e => e.IsError)
            .Where(e => !ExpectedPatterns.Any(p =>
                e.Content.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Returns all log entries from the current scenario (for diagnostics output).
    /// </summary>
    public IReadOnlyList<LogEntry> GetScenarioLogs()
    {
        return _entries.Skip(_scenarioStartIndex).ToList();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _watchers)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
        _watchers.Clear();
    }
}

public sealed record LogEntry(string Resource, string Content, bool IsError);
