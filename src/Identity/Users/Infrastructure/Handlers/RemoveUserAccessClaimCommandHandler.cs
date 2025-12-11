using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Events;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Abstractions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class RemoveUserAccessClaimCommandHandler(
    UserManager<User> manager,
    EventPublisher publisher,
    IAdminContext adminContext,
    IUserAccessAuditLogger auditLogger) : IRequestHandler<RemoveUserAccessClaimCommand>
{
    public async Task Handle(RemoveUserAccessClaimCommand request, CancellationToken cancellationToken)
    {
        User user = await manager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("Invalid user id");
        IList<Claim> claims = await manager.GetClaimsAsync(user);
        Claim? claim = claims.FirstOrDefault(c => c.Type == AaasConstants.AccessClaim && c.Value.StartsWith(request.ClientId + ":"));
        if (claim is null)
        {
            return;
        }
        IdentityResult result = await manager.RemoveClaimAsync(user, claim);
        if (!result.Succeeded)
        {
            throw new Exception("Could not remove access claim");
        }
        await publisher.PublishAsync(new UserAccessClaimRemovedEvent(request.UserId, request.ClientId), cancellationToken);

        await auditLogger.LogAsync(
            new UserAccessAuditEntry(
                DateTimeOffset.UtcNow,
                adminContext.UserId,
                adminContext.OwnerId,
                request.UserId,
                request.ClientId,
                "remove"),
            cancellationToken);
    }
}
