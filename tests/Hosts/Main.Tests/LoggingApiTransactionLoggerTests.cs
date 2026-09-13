using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Main.Infrastructure.Platform;
using Vitarish.Modules.Base.Core.Platform.Transactions;

namespace Vitarish.Main.Tests;

public sealed class LoggingApiTransactionLoggerTests
{
    [Fact]
    public async Task Completion_duration_uses_monotonic_time_when_wall_clock_moves_backward()
    {
        TestTimeProvider timeProvider = new(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
        LoggingApiTransactionLogger logger = new(NullLogger<LoggingApiTransactionLogger>.Instance, timeProvider);
        ApiTransaction transaction = new()
        {
            ProviderKey = "provider",
            OperationName = "operation",
            CorrelationId = "correlation"
        };

        Guid transactionId = await logger.BeginTransactionAsync(transaction);
        timeProvider.Advance(TimeSpan.FromMilliseconds(125));
        timeProvider.SetWallClock(timeProvider.GetUtcNow().AddMinutes(-5));

        await logger.CompleteTransactionAsync(transactionId, 200, null, success: true);

        transaction.DurationMs.Should().Be(125);
        transaction.FinishedAt.Should().Be(timeProvider.GetUtcNow().UtcDateTime);
    }

    private sealed class TestTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
    {
        private DateTimeOffset utcNow = initialUtcNow;
        private long timestamp;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public override long TimestampFrequency => 1000;

        public override long GetTimestamp() => timestamp;

        public void Advance(TimeSpan elapsed)
        {
            utcNow = utcNow.Add(elapsed);
            timestamp += (long)elapsed.TotalMilliseconds;
        }

        public void SetWallClock(DateTimeOffset value) => utcNow = value;
    }
}
