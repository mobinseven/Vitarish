namespace Vorn.Aaas.Users.Application.Commands;

using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vorn.Aaas.Users;
using Vorn.Aaas.Users.Application.Abstractions;
using Vorn.Aaas.Users.Application.Auditing;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Domain.Events;
using Vorn.Aaas.Users.Infrastructure.Publishing;

public record RemoveUserAccessClaimCommand(string UserId, string ClientId) : IRequest;

public class RemoveUserAccessClaimCommandValidator : AbstractValidator<RemoveUserAccessClaimCommand>
{
    public RemoveUserAccessClaimCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
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
        if(claim is null)
        {
            return;
        }
        IdentityResult result = await manager.RemoveClaimAsync(user, claim);
        if(!result.Succeeded)
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
