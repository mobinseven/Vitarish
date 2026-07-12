namespace Vitarish.Platform.Contracts.Responses;

public sealed record PlatformCommandResponse(bool Succeeded, string? Error = null)
{
    public static PlatformCommandResponse Success() => new(true);
    public static PlatformCommandResponse Failure(string error) => new(false, error);
}
