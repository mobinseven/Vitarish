namespace Vitarish.Entities.Server.Abstractions;


public interface IEntityUserService
{
    string? UserId { get; }
    void Set(string? userId);
}

