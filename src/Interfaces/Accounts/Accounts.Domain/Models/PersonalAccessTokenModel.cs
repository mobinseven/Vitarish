namespace Vitarish.Accounts.Domain.Models;

public sealed class PersonalAccessTokenModel
{
    [Range(1, 3650)]
    public int LifetimeDays { get; set; } = 30;

    public bool IsReferenceToken { get; set; }
}
