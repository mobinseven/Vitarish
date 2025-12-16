namespace Vitarish.Entities.Server.Services;


public interface IEntityUserService
{
    string? UserId { get; }
    void Set(string? userId);
}

