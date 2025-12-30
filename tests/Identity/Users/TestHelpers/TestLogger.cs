using Microsoft.Extensions.Logging;

namespace Vitarish.Users.Tests.TestHelpers;

internal sealed class TestLogger<T> : ILogger<T>
{
    internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    public List<LogEntry> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose() { }
    }
}
