namespace Interfaces.Accounts.Tests;

internal sealed class AdjustableTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void Advance(TimeSpan duration) => utcNow = utcNow.Add(duration);
}
