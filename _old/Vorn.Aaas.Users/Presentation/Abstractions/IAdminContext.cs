namespace Vorn.Aaas.Users.Presentation.Abstractions;

public interface IAdminContext
{
    string? UserId { get; }

    string? OwnerId { get; }
}
