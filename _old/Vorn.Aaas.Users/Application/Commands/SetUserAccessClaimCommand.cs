
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

public record SetUserAccessClaimCommand(string UserId, string ClientId, string AccessValue) : IRequest;

public class SetUserAccessClaimCommandValidator : AbstractValidator<SetUserAccessClaimCommand>
{
    public SetUserAccessClaimCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}

public class SetUserAccessClaimCommandHandler(
    UserManager<User> manager,
    EventPublisher publisher,
    IAdminContext adminContext,
    IUserAccessAuditLogger auditLogger) : IRequestHandler<SetUserAccessClaimCommand>
{
    public async Task Handle(SetUserAccessClaimCommand request, CancellationToken cancellationToken)
    {
        User user = await manager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("Invalid user id");
        Claim newClaim = new(AaasConstants.AccessClaim, $"{request.ClientId}:{request.AccessValue}");
        IList<Claim> claims = await manager.GetClaimsAsync(user);
        Claim? oldClaim = claims.FirstOrDefault(c => c.Type == AaasConstants.AccessClaim && c.Value.StartsWith(request.ClientId + ":"));
        IdentityResult result = oldClaim is null
            ? await manager.AddClaimAsync(user, newClaim)
            : await manager.ReplaceClaimAsync(user, oldClaim, newClaim);
        if (!result.Succeeded)
        {
            throw new Exception("Could not set access claim");
        }

        await publisher.PublishAsync(new UserAccessClaimSetEvent(request.UserId, request.ClientId, request.AccessValue), cancellationToken);

        string action = oldClaim is null ? "grant" : "update";
        await auditLogger.LogAsync(
            new UserAccessAuditEntry(
                DateTimeOffset.UtcNow,
                adminContext.UserId,
                adminContext.OwnerId,
                request.UserId,
                request.ClientId,
                action),
            cancellationToken);
    }
}
