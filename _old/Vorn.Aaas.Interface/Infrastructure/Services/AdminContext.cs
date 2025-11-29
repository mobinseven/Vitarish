namespace Vorn.Aaas.Interface.Infrastructure.Services;

using Vorn.Aaas.Client.Api.Abstractions;

internal sealed class AdminContext(IRequestIdentityContext vornContext) : IAdminContext
{
    private string? ownerId;

    public string? UserId => vornContext.UserId;

    public string? OwnerId => ownerId ??= vornContext.OwnerId;
}
