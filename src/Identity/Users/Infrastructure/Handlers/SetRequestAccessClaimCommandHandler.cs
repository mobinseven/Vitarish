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

/// <summary>
/// Handler for setting access claim based on full request context.
/// Centralizes access decision logic using the request pattern.
/// </summary>
public class SetRequestAccessClaimCommandHandler(
    UserManager<User> manager,
    EventPublisher publisher,
    IAdminContext adminContext,
    IUserAccessAuditLogger auditLogger) : IRequestHandler<SetRequestAccessClaimCommand>
{
    public async Task Handle(SetRequestAccessClaimCommand command, CancellationToken cancellationToken)
    {
        string userId = command.Request.UserId ?? throw new InvalidOperationException("UserId is required for setting access");
        string clientId = command.Request.ClientId;

        User user = await manager.FindByIdAsync(userId) ?? throw new InvalidDataException("Invalid user id");
        Claim newClaim = new(IdentityAccessConstants.AccessClaim, $"{clientId}:{command.AccessValue}");
        IList<Claim> claims = await manager.GetClaimsAsync(user);
        Claim? oldClaim = claims.FirstOrDefault(c => c.Type == IdentityAccessConstants.AccessClaim && c.Value.StartsWith(clientId + ":"));
        
        IdentityResult result = oldClaim is null
            ? await manager.AddClaimAsync(user, newClaim)
            : await manager.ReplaceClaimAsync(user, oldClaim, newClaim);
        
        if (!result.Succeeded)
        {
            throw new Exception("Could not set access claim");
        }

        await publisher.PublishAsync(new UserAccessClaimSetEvent(userId, clientId, command.AccessValue), cancellationToken);

        string action = oldClaim is null ? "grant" : "update";
        await auditLogger.LogAsync(
            new UserAccessAuditEntry(
                DateTimeOffset.UtcNow,
                adminContext.UserId,
                adminContext.OwnerId,
                userId,
                clientId,
                action),
            cancellationToken);
    }
}
