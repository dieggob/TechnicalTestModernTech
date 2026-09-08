using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Maintenance.IntegrationTests;

/// <summary>
/// Collects log entries (category, level, rendered message, scope values) so tests can
/// assert what the API logs and, just as important, what it does not.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public sealed record Entry(string Category, LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Scope);

    private readonly ConcurrentQueue<Entry> _entries = new();
    private readonly ScopeStack _scopes = new();

    public IReadOnlyCollection<Entry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries, _scopes);

    public void Dispose()
    {
    }

    private sealed class ScopeStack
    {
        private readonly AsyncLocal<ImmutableStack> _current = new();

        public IDisposable Push(object? state)
        {
            var previous = _current.Value;
            _current.Value = new ImmutableStack(state, previous);
            return new Pop(this, previous);
        }

        public IReadOnlyDictionary<string, object?> Snapshot()
        {
            var values = new Dictionary<string, object?>();
            for (var node = _current.Value; node is not null; node = node.Previous)
            {
                if (node.State is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        values.TryAdd(pair.Key, pair.Value);
                    }
                }
            }

            return values;
        }

        private sealed record ImmutableStack(object? State, ImmutableStack? Previous);

        private sealed class Pop(ScopeStack owner, ImmutableStack? previous) : IDisposable
        {
            public void Dispose() => owner._current.Value = previous!;
        }
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<Entry> entries, ScopeStack scopes) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new Entry(category, logLevel, formatter(state, exception), scopes.Snapshot()));
    }
}
