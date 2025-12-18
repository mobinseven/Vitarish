namespace Vitarish.Entities.Server.Services;

public class EntityUserService : IEntityUserService
{
    public string? UserId { get; private set; }
    public void Set(string? userId) => UserId = userId;
}
