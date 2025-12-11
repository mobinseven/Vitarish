namespace Vitarish.Clients.Identity.Options;

public sealed record SignalRConnectionOptions
{
    public bool? UseMessagePack { get; init; }
}
