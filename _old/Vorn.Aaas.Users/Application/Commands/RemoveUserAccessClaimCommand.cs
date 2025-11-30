
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Domain.Events;
using Vorn.Aaas.Users.Domain.Models;
using Vorn.Aaas.Users.Infrastructure.Abstractions;
using Vorn.Aaas.Users.Infrastructure.Service;
using Vorn.Aaas.Users.Presentation.Abstractions;

namespace Vorn.Aaas.Users.Application.Commands;

public record RemoveUserAccessClaimCommand(string UserId, string ClientId) : IRequest;

public class RemoveUserAccessClaimCommandValidator : AbstractValidator<RemoveUserAccessClaimCommand>
{
    public RemoveUserAccessClaimCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}

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
