namespace Vitarish.Instagram.Scraper;

public sealed record RetryOptions(int MaxAttempts, TimeSpan InitialDelay, TimeSpan MaxDelay)
{
    public static RetryOptions Default { get; } = new(MaxAttempts: 3, InitialDelay: TimeSpan.FromSeconds(1), MaxDelay: TimeSpan.FromSeconds(8));

    public RetryOptions WithMaxAttempts(int attempts) => this with { MaxAttempts = attempts };
    public RetryOptions WithInitialDelay(TimeSpan delay) => this with { InitialDelay = delay };
    public RetryOptions WithMaxDelay(TimeSpan delay) => this with { MaxDelay = delay };
}
