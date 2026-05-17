namespace Vitarish.Social.Domain.Entities;

public class SocialConnectionToken : Entity
{
    public Guid ConnectionId
    {
        get;
        set => SetField(ref field, value, nameof(ConnectionId));
    }

    public string AccessToken
    {
        get;
        set => SetField(ref field, value, nameof(AccessToken));
    } = string.Empty;

    public string? RefreshToken
    {
        get;
        set => SetField(ref field, value, nameof(RefreshToken));
    }

    public DateTimeOffset? ExpiresAt
    {
        get;
        set => SetField(ref field, value, nameof(ExpiresAt));
    }

    public string Scopes
    {
        get;
        set => SetField(ref field, value, nameof(Scopes));
    } = string.Empty;

    public DateTimeOffset? LastRefreshedAt
    {
        get;
        set => SetField(ref field, value, nameof(LastRefreshedAt));
    }
}
