namespace Vitarish.Users.Application.Abstractions;

public interface IAdminContext
{
    string? UserId { get; }

    string? OwnerId { get; }
}
