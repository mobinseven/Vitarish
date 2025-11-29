namespace Vorn.Aaas.Users.Application.Abstractions;

public interface IAdminContext
{
    string? UserId { get; }

    string? OwnerId { get; }
}
