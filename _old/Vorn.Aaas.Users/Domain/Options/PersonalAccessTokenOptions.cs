namespace Vorn.Aaas.Users.Domain.Options;

public sealed class PersonalAccessTokenOptions
{
    public int DefaultLifetimeDays { get; init; } = 30;
}
