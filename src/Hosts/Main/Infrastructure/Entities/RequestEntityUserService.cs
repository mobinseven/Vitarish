using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Main.Infrastructure.Entities;

internal sealed class RequestEntityUserService(IRequestContext requestContext) : IEntityUserService
{
    private string? userId;

    public string? UserId => userId ?? requestContext.UserId;

    public void Set(string? userId) => this.userId = userId;
}
