using FluentValidation;
using MediatR;
using Vitarish.Requests.Domain.Models;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Command to remove access claim for a request based on the full request context.
/// This replaces the client-centric RemoveUserAccessClaimCommand.
/// </summary>
public record RemoveRequestAccessClaimCommand(RequestIdentityDto Request) : IRequest;

public class RemoveRequestAccessClaimCommandValidator : AbstractValidator<RemoveRequestAccessClaimCommand>
{
    public RemoveRequestAccessClaimCommandValidator()
    {
        _ = RuleFor(c => c.Request).NotNull();
        _ = RuleFor(c => c.Request.UserId).NotEmpty();
        _ = RuleFor(c => c.Request.ClientId).NotEmpty();
    }
}
