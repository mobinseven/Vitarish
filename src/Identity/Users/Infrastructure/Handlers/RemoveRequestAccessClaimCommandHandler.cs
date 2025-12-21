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
/// Handler for removing access claim based on full request context.
/// Centralizes access decision logic using the request pattern.
/// </summary>
public class RemoveRequestAccessClaimCommandHandler(
    UserManager<User> manager,
    EventPublisher publisher,
    IAdminContext adminContext,
    IUserAccessAuditLogger auditLogger) : IRequestHandler<RemoveRequestAccessClaimCommand>
{
    public async Task Handle(RemoveRequestAccessClaimCommand command, CancellationToken cancellationToken)
    {
        string userId = command.Request.UserId ?? throw new InvalidOperationException("UserId is required for removing access");
        string clientId = command.Request.ClientId;

        User user = await manager.FindByIdAsync(userId) ?? throw new InvalidDataException("Invalid user id");
        IList<Claim> claims = await manager.GetClaimsAsync(user);
        Claim? claim = claims.FirstOrDefault(c => c.Type == IdentityAccessConstants.AccessClaim && c.Value.StartsWith(clientId + ":"));
        
        if (claim is null)
        {
            return;
        }
        
        IdentityResult result = await manager.RemoveClaimAsync(user, claim);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not remove access claim for user '{userId}' and client '{clientId}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        
        await publisher.PublishAsync(new UserAccessClaimRemovedEvent(userId, clientId), cancellationToken);

        await auditLogger.LogAsync(
            new UserAccessAuditEntry(
                DateTimeOffset.UtcNow,
                adminContext.UserId,
                adminContext.OwnerId,
                userId,
                clientId,
                "remove"),
            cancellationToken);
    }
}
